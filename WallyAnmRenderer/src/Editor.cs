using System;
using System.Numerics;
using System.Threading.Tasks;

using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib;

namespace WallyAnmRenderer;

public sealed class Editor
{
    public const float ZOOM_INCREMENT = 0.15f;
    public const float MIN_ZOOM = 0;
    public const float MAX_ZOOM = 20.0f;
    public const float LINE_WIDTH = 5; // width at Camera zoom = 1
    public const int INITIAL_SCREEN_WIDTH = 1280;
    public const int INITIAL_SCREEN_HEIGHT = 720;

    public const string LOADING_TEXT = "Loading...";
    public const string ERROR_TEXT = "Error!";
    public static readonly RlColor LOADING_TEXT_COLOR = RlColor.RayWhite with { A = 64 };

    private Camera2D _cam;

    public TimeSpan Time { get; set; } = TimeSpan.FromSeconds(0);
    private double _animationFps = 24.0;
    private bool _paused = false;

    private PathPreferences PathPrefs { get; }

    public Animator? Animator { get; private set; }
    public GfxInfo GfxInfo { get; } = new();
    private RlColor _bgColor = new(0, 0, 51, 255); // #000033

    public ViewportWindow ViewportWindow { get; } = new();
    public PathsWindow PathsWindow { get; } = new();
    public OverridesWindow OverridesWindow { get; } = new();
    public AnmWindow AnmWindow { get; } = new();
    public AnimationInfoWindow AnimationInfoWindow { get; } = new();
    public GfxInfoWindow GfxInfoWindow { get; } = new();
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

        PathsWindow.LoadingRequested += async (_, key, path) =>
        {
            pathPrefs.BrawlhallaPath = path;
            pathPrefs.DecryptionKey = key;
            await LoadFiles();
        };

        AnmWindow.FileUnloaded += (_, file) =>
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
            GfxInfo.ColorScheme = color;
            Animator?.Loader.AssetLoader.ClearSwfShapeCache();
        };

        TimeWindow.FrameSeeked += (_, frame) =>
        {
            Time = TimeSpan.FromSeconds(frame / _animationFps);
            Time += TimeSpan.FromTicks(1); // required due to imprecision
        };

        TimeWindow.FrameMove += (_, frame) =>
        {
            Time += TimeSpan.FromSeconds(frame / _animationFps);
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
        bool hadError = false;

        IGfxType? gfxType = null;
        Task<BoneSprite[]>? spritesTask = null;
        BoneSprite? highlightedSprite = null;
        if (Animator?.Loader.SwzFiles?.Game is not null)
        {
            (IGfxType gfx, bool flip)? info = null;
            try
            {
                info = GfxInfo.ToGfxType(Animator.Loader.SwzFiles.Game);
            }
            catch (Exception e)
            {
                Rl.TraceLog(TraceLogLevel.Error, e.Message);
                Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
                hadError = true;
            }

            if (info is null)
            {
                finishedLoading = false;
            }
            else
            {
                (gfxType, bool flip) = info.Value;
                if (GfxInfo.AnimationPicked)
                {
                    string animation = GfxInfo.Animation;
                    long frame = (long)Math.Floor(_animationFps * Time.TotalSeconds);
                    ExportModal.Update(PathPrefs, Animator, gfxType, animation, frame, flip);

                    spritesTask = Animator.GetAnimationInfo(gfxType, animation, frame, flip ? Transform2D.FLIP_X : Transform2D.IDENTITY);
                    if (!spritesTask.IsCompletedSuccessfully)
                    {
                        if (spritesTask.IsFaulted && spritesTask.Exception is Exception e)
                        {
                            Rl.TraceLog(TraceLogLevel.Error, e.Message);
                            Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
                            hadError = true;
                        }
                        finishedLoading = false;
                    }
                }
            }
        }

        BoneSprite[]? sprites = null;
        if (spritesTask is not null && spritesTask.IsCompletedSuccessfully)
            sprites = spritesTask.Result;

        // done separate from other UI to have access to the animation information
        if (Animator is not null && GfxInfo.AnimationPicked && AnimationInfoWindow.Open)
            AnimationInfoWindow.Show(sprites, ref highlightedSprite);
        if (GfxInfoWindow.Open)
            GfxInfoWindow.Show(gfxType);

        Rl.BeginTextureMode(ViewportWindow.Framebuffer);
        Rl.BeginMode2D(_cam);

        Rl.ClearBackground(_bgColor);

        if (Animator is not null && sprites is not null)
        {
            Animator.Loader.AssetLoader.Upload();

            Transform2D center = GetCenteringTransform();

            foreach (BoneSprite sprite in sprites)
            {
                bool highlighted = sprite == highlightedSprite;

                if (sprite is SwfBoneSprite swfSprite)
                {
                    ValueTask<BoneShape[]> shapesTask = Animator.SpriteToShapes(swfSprite);
                    if (!shapesTask.IsCompletedSuccessfully)
                    {
                        if (shapesTask.IsFaulted && shapesTask.AsTask().Exception is Exception e)
                        {
                            Rl.TraceLog(TraceLogLevel.Error, e.Message);
                            Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
                            hadError = true;
                        }
                        finishedLoading = false;
                        continue;
                    }

                    BoneShape[] shapes = shapesTask.Result;
                    foreach (BoneShape shape in shapes)
                    {
                        Texture2DWrapper? texture = Animator.ShapeToTexture(swfSprite, shape);
                        if (texture is null)
                        {
                            finishedLoading = false;
                            continue;
                        }

                        RaylibUtils.DrawTextureWithTransform(
                            texture.Texture,
                            0, 0, texture.Width, texture.Height,
                            center * shape.Transform * texture.Transform,
                            tintB: highlighted ? 0 : 1,
                            tintA: (float)sprite.Opacity
                        );
                    }
                }
                else if (sprite is BitmapBoneSprite bitmapSprite)
                {
                    Texture2DWrapper? texture = Animator.Loader.AssetLoader.LoadTexture(bitmapSprite.SpriteData);
                    if (texture is null)
                    {
                        finishedLoading = false;
                        continue;
                    }

                    RaylibUtils.DrawTextureWithTransform(
                        texture.Texture,
                        0, 0, texture.Width, texture.Height,
                        center * bitmapSprite.Transform * texture.Transform,
                        tintB: highlighted ? 0 : 1,
                        tintA: (float)sprite.Opacity
                    );
                }
            }
        }

        if (GfxInfo.AnimationPicked && (!finishedLoading || hadError))
        {
            int textSize = Rl.MeasureText(hadError ? ERROR_TEXT : LOADING_TEXT, 100);
            float width = ViewportWindow.Bounds.Width;
            float height = ViewportWindow.Bounds.Height;
            Rl.DrawText(hadError ? ERROR_TEXT : LOADING_TEXT, (int)((width - textSize) / 2.0), (int)(height / 2.0) - 160, 100, LOADING_TEXT_COLOR);
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
        if (OverridesWindow.Open)
            OverridesWindow.Show(PathPrefs.BrawlhallaPath, Animator?.Loader.AssetLoader);
        if (AnmWindow.Open)
            AnmWindow.Show(PathPrefs.BrawlhallaPath, Animator?.Loader.AssetLoader, GfxInfo);
        if (TimeWindow.Open && Animator is not null && GfxInfo.AnimationPicked)
        {
            ValueTask<AnimationData> animDataTask = Animator.GetAnimData(GfxInfo);
            if (animDataTask.IsCompletedSuccessfully)
            {
                AnimationData animData = animDataTask.Result;
                TimeWindow.Show(animData, Time, ref _paused, ref _animationFps);
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
            if (ImGui.MenuItem("Override files", null, OverridesWindow.Open)) OverridesWindow.Open = !OverridesWindow.Open;
            if (ImGui.MenuItem("Pick animation", null, AnmWindow.Open)) AnmWindow.Open = !AnmWindow.Open;
            if (ImGui.MenuItem("Pick cosmetics", null, PickerWindow.Open)) PickerWindow.Open = !PickerWindow.Open;
            if (ImGui.MenuItem("Animation timeline", null, TimeWindow.Open)) TimeWindow.Open = !TimeWindow.Open;
            if (ImGui.MenuItem("Animation info", null, AnimationInfoWindow.Open)) AnimationInfoWindow.Open = !AnimationInfoWindow.Open;
            if (ImGui.MenuItem("Gfx info", null, GfxInfoWindow.Open)) GfxInfoWindow.Open = !GfxInfoWindow.Open;
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Cache", Animator is not null) && Animator is not null)
        {
            if (ImGui.MenuItem("Clear swf shape cache")) Animator.Loader.AssetLoader.ClearSwfShapeCache();
            if (ImGui.MenuItem("Clear swf file cache")) Animator.Loader.AssetLoader.ClearSwfFileCache();
            if (ImGui.MenuItem("Clear bitmap cache")) Animator.Loader.AssetLoader.ClearTextureCache();
            ImGui.EndMenu();
        }

        if (ImGui.MenuItem("Export")) ExportModal.Open();

        ImGui.EndMainMenuBar();
    }

    private void Update()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        bool wantCaptureKeyboard = io.WantCaptureKeyboard;

        if (PathPrefs.BrawlhallaPath is not null && PathPrefs.DecryptionKey is not null)
        {
            if (Animator is null)
            {
                Animator = new(PathPrefs.BrawlhallaPath, PathPrefs.DecryptionKey.Value);
                _ = LoadFiles();
            }
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

            if (Rl.IsKeyDown(KeyboardKey.LeftControl))
            {
                if (Animator is not null && Rl.IsKeyPressed(KeyboardKey.R))
                {
                    Animator.Loader.AssetLoader.ClearSwfFileCache();
                    Animator.Loader.AssetLoader.ClearSwfShapeCache();
                    Animator.Loader.AssetLoader.ClearTextureCache();
                }
            }
            else
            {
                if (ViewportWindow.Hovered && Rl.IsKeyPressed(KeyboardKey.R))
                    ResetCam();
            }
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

    private async Task LoadFiles()
    {
        if (Animator is null) return;

        PathsWindow.OnLoadingStarted();
        try
        {
            await Animator.Loader.LoadFilesAsync();
            PathsWindow.OnLoadingFinished();
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException) return;
            Rl.TraceLog(TraceLogLevel.Error, e.Message);
            Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
            PathsWindow.OnLoadingError(e);
        }
    }

    ~Editor()
    {
        rlImGui.Shutdown();
        Rl.CloseWindow();
    }
}