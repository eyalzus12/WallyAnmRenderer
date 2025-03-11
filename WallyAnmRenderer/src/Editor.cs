using System;
using System.Numerics;
using System.Threading.Tasks;

using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Bones;

namespace WallyAnmRenderer;

public sealed class Editor
{
    public const float ZOOM_INCREMENT = 0.15f;
    public const float MIN_ZOOM = 0;
    public const float MAX_ZOOM = 20.0f;
    public const float LINE_WIDTH = 5; // width at Camera zoom = 1
    public const int INITIAL_SCREEN_WIDTH = 1280;
    public const int INITIAL_SCREEN_HEIGHT = 720;

    private Camera2D _cam;

    public TimeSpan Time { get; set; } = TimeSpan.FromSeconds(0);
    private bool _paused = false;

    private PathPreferences PathPrefs { get; }

    public Animator? Animator { get; private set; }
    public GfxInfo GfxInfo { get; private set; } = new();
    private RlColor _bgColor = new(0, 0, 51, 255); // #000033

    public ViewportWindow ViewportWindow { get; } = new();
    public PathsWindow PathsWindow { get; } = new();
    public AnmWindow AnmWindow { get; } = new();
    public AnimationInfoWindow AnimationInfoWindow { get; } = new();
    public TimeWindow TimeWindow { get; } = new();
    public PickerWindow PickerWindow { get; } = new();
    public ExportModal ExportModal { get; } = new();

    private bool _showMainMenuBar = true;

    private int _preFullScreenW;
    private int _preFullScreenH;

    public Editor(PathPreferences pathPrefs)
    {
        PathPrefs = pathPrefs;

        PathPrefs.BrawlhallaPathChanged += (_, path) =>
        {
            if (Animator is not null) Animator.BrawlPath = path;
        };

        PathPrefs.DecryptionKeyChanged += (_, key) =>
        {
            if (Animator is not null) Animator.Key = key;
        };

        AnmWindow.AnmUnloadRequested += (_, file) =>
        {
            if (GfxInfo.SourceFilePath == file)
            {
                GfxInfo.SourceFilePath = null;
                GfxInfo.AnimClass = null;
                GfxInfo.AnimFile = null;
                GfxInfo.Animation = null;
            }
        };

        PickerWindow.ColorSchemeSelected += (_, color) =>
        {
            if (Animator is not null)
            {
                GfxInfo.ColorScheme = color;
                Animator.Loader.AssetLoader.ClearSwfShapeCache();
            }
        };

        TimeWindow.Paused += (_, paused) =>
        {
            _paused = paused;
        };

        TimeWindow.FrameSeeked += (_, frame) =>
        {
            Time = TimeSpan.FromSeconds(frame / 24.0);
            Time += TimeSpan.FromTicks(1); // required due to imprecision
        };

        TimeWindow.FrameMove += (_, frame) =>
        {
            Time += TimeSpan.FromSeconds(frame / 24.0);
            Time += TimeSpan.FromTicks(frame); // required due to imprecision
        };
    }

    public void Run()
    {
        Setup();

        while (!Rl.WindowShouldClose())
        {
            if (!_paused)
            {
                float delta = Rl.GetFrameTime();
                Time += TimeSpan.FromSeconds(delta);
            }
            Update();
            Draw();
        }

        PathPrefs.Save();

        Rl.CloseWindow();
    }

    private void Setup()
    {
        LogCallback.Init();

        PathsWindow.Open = true;

        Rl.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.MaximizedWindow);
        Rl.InitWindow(INITIAL_SCREEN_WIDTH, INITIAL_SCREEN_HEIGHT, "WallyAnmRenderer");
        Rl.MaximizeWindow(); // why is the config flag not working smh

        Rl.SetExitKey(KeyboardKey.Null);
        rlImGui.Setup(true, true);
        Style.Apply();

        ResetCam(INITIAL_SCREEN_WIDTH, INITIAL_SCREEN_HEIGHT);
    }

    private Transform2D GetCenteringTransform()
    {
        float width = ViewportWindow.Bounds.Width;
        float height = ViewportWindow.Bounds.Height;
        return Transform2D.CreateTranslate(width / 2.0, height / 2.0);
    }

    private void Draw()
    {
        Rl.BeginDrawing();
        Rl.ClearBackground(RlColor.Black);
        Rlgl.SetLineWidth(Math.Max(LINE_WIDTH * _cam.Zoom, 1));
        rlImGui.Begin();

        Gui();
        bool finishedLoading = true;

        bool flip = false;
        Task<BoneSpriteWithName[]>? spritesTask = null;
        BoneSpriteWithName? highlightedSprite = null;
        if (Animator?.Loader.SwzFiles.Game is not null && GfxInfo.AnimationPicked)
        {
            var info = GfxInfo.ToGfxType(Animator.Loader.SwzFiles.Game);
            if (info is null)
            {
                finishedLoading = false;
            }
            else
            {
                long frame = (long)Math.Floor(24 * Time.TotalSeconds);
                (IGfxType gfxType, string animation, flip) = info.Value;

                ExportModal.Update(Animator, gfxType, animation, frame, flip);

                spritesTask = Animator.GetAnimationInfo(gfxType, animation, frame, flip ? Transform2D.FLIP_X : Transform2D.IDENTITY);
            }
        }

        BoneSpriteWithName[]? sprites = null;
        if (spritesTask is not null && spritesTask.IsCompletedSuccessfully)
            sprites = spritesTask.Result;

        // done separate from other UI to have access to the animation information
        if (AnimationInfoWindow.Open && Animator is not null && GfxInfo.AnimationPicked)
        {
            AnimationInfoWindow.Show(sprites, ref highlightedSprite);
        }

        Rl.BeginTextureMode(ViewportWindow.Framebuffer);
        Rl.BeginMode2D(_cam);

        Rl.ClearBackground(_bgColor);

        if (Animator is not null && sprites is not null)
        {
            Animator.Loader.AssetLoader.Upload();

            Transform2D center = GetCenteringTransform();
            Transform2D direction = flip ? Transform2D.FLIP_X : Transform2D.IDENTITY;
            Transform2D mainTransform = center * direction;

            foreach (BoneSpriteWithName sprite in sprites)
            {
                bool highlighted = sprite == highlightedSprite;

                Task<BoneShape[]> shapes = Animator.SpriteToShapes(sprite);
                if (!shapes.IsCompletedSuccessfully)
                {
                    finishedLoading = false;
                    continue;
                }

                foreach (BoneShape shape in shapes.Result)
                {
                    Texture2DWrapper? texture = Animator.ShapeToTexture(sprite, shape);
                    if (texture is null)
                    {
                        finishedLoading = false;
                        continue;
                    }

                    RaylibUtils.DrawTextureWithTransform(
                        texture.Texture,
                        0, 0, texture.Width, texture.Height,
                        mainTransform * shape.Transform * texture.Transform,
                        tintB: highlighted ? 0 : 1,
                        tintA: (float)sprite.Opacity
                    );
                }
            }
        }

        if (!finishedLoading)
        {
            string text = "Loading...";
            int textSize = Rl.MeasureText(text, 100);
            float width = ViewportWindow.Bounds.Width;
            float height = ViewportWindow.Bounds.Height;
            Rl.DrawText(text, (int)((width - textSize) / 2.0), (int)(height / 2.0) - 160, 100, RlColor.RayWhite);
        }

        Rl.EndMode2D();
        Rl.EndTextureMode();

        rlImGui.End();
        Rl.EndDrawing();
    }

    private void Gui()
    {
        ImGui.DockSpaceOverViewport();
        if (_showMainMenuBar)
            ShowMainMenuBar();

        if (ViewportWindow.Open)
            ViewportWindow.Show();
        if (PathsWindow.Open)
            PathsWindow.Show(PathPrefs);
        if (AnmWindow.Open)
            AnmWindow.Show(PathPrefs.BrawlhallaPath, Animator?.Loader.AssetLoader, GfxInfo);
        if (TimeWindow.Open && Animator is not null && GfxInfo.AnimationPicked)
        {
            Task<long?> frameCountTask = Animator.GetFrameCount(GfxInfo);
            if (frameCountTask.IsCompletedSuccessfully && frameCountTask.Result is not null)
            {
                long frameCount = frameCountTask.Result.Value;
                TimeWindow.Show(frameCount, Time, _paused);
            }
        }
        if (PickerWindow.Open)
            PickerWindow.Show(Animator?.Loader, GfxInfo, ref _bgColor);
    }

    private void ShowMainMenuBar()
    {
        ImGui.BeginMainMenuBar();

        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.MenuItem("Viewport", null, ViewportWindow.Open)) ViewportWindow.Open = !ViewportWindow.Open;
            if (ImGui.MenuItem("Pick paths", null, PathsWindow.Open)) PathsWindow.Open = !PathsWindow.Open;
            if (ImGui.MenuItem("Pick animation", null, AnmWindow.Open)) AnmWindow.Open = !AnmWindow.Open;
            if (ImGui.MenuItem("Pick cosmetics", null, PickerWindow.Open)) PickerWindow.Open = !PickerWindow.Open;
            if (ImGui.MenuItem("Animation timeline", null, TimeWindow.Open)) TimeWindow.Open = !TimeWindow.Open;
            if (ImGui.MenuItem("Animation info", null, AnimationInfoWindow.Open)) AnimationInfoWindow.Open = !AnimationInfoWindow.Open;
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Cache", Animator is not null) && Animator is not null)
        {
            if (ImGui.MenuItem("Clear swf shape cache")) Animator.Loader.AssetLoader.ClearSwfShapeCache();
            if (ImGui.MenuItem("Clear swf file cache")) Animator.Loader.AssetLoader.ClearSwfFileCache();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Export"))
        {
            if (ImGui.MenuItem("Test")) ExportModal.Open();
            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }

    private void Update()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        bool wantCaptureKeyboard = io.WantCaptureKeyboard;

        if (PathPrefs.BrawlhallaPath is not null && PathPrefs.DecryptionKey is not null)
        {
            Animator ??= new(PathPrefs.BrawlhallaPath, PathPrefs.DecryptionKey.Value);
        }

        if (ViewportWindow.Hovered)
        {
            float wheel = Rl.GetMouseWheelMove();
            if (wheel != 0)
            {
                _cam.Target = ViewportWindow.ScreenToWorld(Rl.GetMousePosition(), _cam);
                _cam.Offset = Rl.GetMousePosition() - ViewportWindow.Bounds.P1;
                _cam.Zoom = Math.Clamp(_cam.Zoom + wheel * ZOOM_INCREMENT * _cam.Zoom, MIN_ZOOM, MAX_ZOOM);
            }

            if (Rl.IsMouseButtonDown(MouseButton.Right))
            {
                Vector2 delta = Rl.GetMouseDelta();
                delta = Raymath.Vector2Scale(delta, -1.0f / _cam.Zoom);
                _cam.Target += delta;
            }

            if (!wantCaptureKeyboard && Rl.IsKeyPressed(KeyboardKey.R))
                ResetCam();
        }

        //if (ViewportWindow.Hovered && Rl.IsMouseButtonReleased(MouseButton.Left))
        //  Selection.Object = PickingFramebuffer.GetObjectAtCoords(ViewportWindow, Canvas, Level, _cam, _renderConfig, _state);

        if (!wantCaptureKeyboard)
        {
            if (Rl.IsKeyPressed(KeyboardKey.F11))
            {
                // bullshit to handle multiple monitors
                if (Rl.IsWindowFullscreen())
                {
                    Rl.ToggleFullscreen();
                    Rl.SetWindowSize(_preFullScreenW, _preFullScreenH);
                }
                else
                {
                    int monitor = Rl.GetCurrentMonitor();
                    _preFullScreenW = Rl.GetScreenWidth();
                    _preFullScreenH = Rl.GetScreenHeight();
                    Rl.SetWindowSize(Rl.GetMonitorWidth(monitor), Rl.GetMonitorHeight(monitor));
                    Rl.ToggleFullscreen();
                }
            }
            if (Rl.IsKeyPressed(KeyboardKey.F1)) _showMainMenuBar = !_showMainMenuBar;
        }
    }

    public Vector2 ScreenToWorld(Vector2 screenPos) =>
        Rl.GetScreenToWorld2D(screenPos - ViewportWindow.Bounds.P1, _cam);

    public void ResetCam() => ResetCam(ViewportWindow.Bounds.Width, ViewportWindow.Bounds.Height);

    public void ResetCam(float surfaceW, float surfaceH)
    {
        // TODO: pick non-arbitrary size
        float scale = MathF.Min(surfaceW / 1024, surfaceH / 768);
        _cam.Offset = new(0, 160); // ~legend height
        _cam.Target = new(0, 0);
        _cam.Zoom = scale;
    }

    ~Editor()
    {
        rlImGui.Shutdown();
        Rl.CloseWindow();
    }
}