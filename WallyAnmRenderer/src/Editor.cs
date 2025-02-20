using System;
using System.Numerics;

using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Math;

namespace WallyAnmRenderer;

public sealed class Editor
{
    public const float ZOOM_INCREMENT = 0.15f;
    public const float MIN_ZOOM = 0.1f;
    public const float MAX_ZOOM = 20.0f;
    public const float LINE_WIDTH = 5; // width at Camera zoom = 1
    public const int INITIAL_SCREEN_WIDTH = 1280;
    public const int INITIAL_SCREEN_HEIGHT = 720;

    private Camera2D _cam;
    public TimeSpan Time { get; set; } = TimeSpan.FromSeconds(0);

    private PathPreferences PathPrefs { get; }
    public Animator? Animator { get; private set; }
    public GfxInfo GfxInfo { get; private set; } = new();

    public ViewportWindow ViewportWindow { get; } = new();
    public PathsWindow PathsWindow { get; } = new();
    public AnmWindow AnmWindow { get; } = new();
    public PickerWindow PickerWindow { get; } = new();

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
    }

    public void Run()
    {
        Setup();

        while (!Rl.WindowShouldClose())
        {
            float delta = Rl.GetFrameTime();
            Time += TimeSpan.FromSeconds(delta);
            Draw();
            Update();
        }

        PathPrefs.Save();

        Rl.CloseWindow();
    }

    private void Setup()
    {
        LogCallback.Init();

        PathsWindow.Open = PathPrefs.DecryptionKey is null || PathPrefs.BrawlhallaPath is null;

        Rl.SetConfigFlags(ConfigFlags.VSyncHint);
        Rl.SetConfigFlags(ConfigFlags.ResizableWindow);
        Rl.InitWindow(INITIAL_SCREEN_WIDTH, INITIAL_SCREEN_HEIGHT, "WallyAnmRenderer");
        // window position ends up too high for me. tf?
        Vector2 windowPos = Rl.GetWindowPosition();
        Rl.SetWindowPosition((int)windowPos.X, (int)windowPos.Y + 20);

        Rl.SetExitKey(KeyboardKey.Null);
        rlImGui.Setup(true, true);
        Style.Apply();

        ResetCam(INITIAL_SCREEN_WIDTH, INITIAL_SCREEN_HEIGHT);
    }

    private void Draw()
    {
        Rl.BeginDrawing();
        Rl.ClearBackground(RlColor.Black);
        Rlgl.SetLineWidth(Math.Max(LINE_WIDTH * _cam.Zoom, 1));
        rlImGui.Begin();

        Gui();

        Rl.BeginTextureMode(ViewportWindow.Framebuffer);
        Rl.BeginMode2D(_cam);

        Rl.ClearBackground(RlColor.Black);

        if (Animator is not null)
        {
            (IGfxType, string)? info = GfxInfo.ToGfxType(Animator.Loader.SwzFiles.Game);
            if (info is not null)
            {
                (IGfxType gfxType, string animation) = info.Value;
                float width = ViewportWindow.Bounds.Width;
                float height = ViewportWindow.Bounds.Height;
                Transform2D center = Transform2D.CreateTranslate(width / 2.0, height / 2.0);
                bool finishedLoading = Animator.Animate(gfxType, animation, (long)Math.Floor(24 * Time.TotalSeconds), center);

                if (!finishedLoading)
                {
                    string text = "Loading...";
                    int textSize = Rl.MeasureText(text, 100);
                    Rl.DrawText(text, (int)((width - textSize) / 2.0), (int)(height / 2.0) - 160, 100, RlColor.RayWhite);
                }
            }
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
        if (AnmWindow.Open && Animator is not null)
            AnmWindow.Show(PathPrefs.BrawlhallaPath, Animator.Loader.AssetLoader, GfxInfo);
        if (PickerWindow.Open && Animator is not null)
            PickerWindow.Show(Animator.Loader, GfxInfo);
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
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Cache", Animator is not null) && Animator is not null)
        {
            if (ImGui.MenuItem("Clear swf shape cache")) Animator.Loader.AssetLoader.ClearSwfShapeCache();
            if (ImGui.MenuItem("Clear swf file cache")) Animator.Loader.AssetLoader.ClearSwfFileCache();
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