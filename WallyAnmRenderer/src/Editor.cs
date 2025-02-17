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
    public const float MIN_ZOOM = 0.01f;
    public const float MAX_ZOOM = 5.0f;
    public const float LINE_WIDTH = 5; // width at Camera zoom = 1
    public const int INITIAL_SCREEN_WIDTH = 1280;
    public const int INITIAL_SCREEN_HEIGHT = 720;

    private Camera2D _cam = new();
    public TimeSpan Time { get; set; } = TimeSpan.FromSeconds(0);

    private PathPreferences PathPrefs { get; }
    public Animator? Animator { get; private set; }
    public GfxInfo GfxInfo { get; private set; } = new();

    public ViewportWindow ViewportWindow { get; } = new();
    public PathsWindow PathsWindow { get; } = new();
    public PickerWindow PickerWindow { get; } = new();

    private bool _showMainMenuBar = true;

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

        PathsWindow.Open = true;

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
        //Rlgl.SetLineWidth(Math.Max(LINE_WIDTH * _cam.Zoom, 1));
        rlImGui.Begin();
        ImGui.PushFont(Style.Font);

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
                Animator.Animate(gfxType, animation, (long)Math.Floor(24 * Time.TotalSeconds), Transform2D.IDENTITY);
            }
        }

        Rl.EndMode2D();
        Rl.EndTextureMode();

        ImGui.PopFont();
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
            if (ImGui.MenuItem("Pick animation", null, PickerWindow.Open)) PickerWindow.Open = !PickerWindow.Open;
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

            if (!wantCaptureKeyboard && Rl.IsKeyPressed(KeyboardKey.R) && !Rl.IsKeyDown(KeyboardKey.LeftControl))
                ResetCam((int)ViewportWindow.Bounds.Width, (int)ViewportWindow.Bounds.Height);
        }

        //if (ViewportWindow.Hovered && Rl.IsMouseButtonReleased(MouseButton.Left))
        //  Selection.Object = PickingFramebuffer.GetObjectAtCoords(ViewportWindow, Canvas, Level, _cam, _renderConfig, _state);

        if (!wantCaptureKeyboard)
        {
            if (Rl.IsKeyPressed(KeyboardKey.F11)) Rl.ToggleFullscreen();
            if (Rl.IsKeyPressed(KeyboardKey.F1)) _showMainMenuBar = !_showMainMenuBar;
        }
    }

    public Vector2 ScreenToWorld(Vector2 screenPos) =>
        Rl.GetScreenToWorld2D(screenPos - ViewportWindow.Bounds.P1, _cam);

    public void ResetCam() => ResetCam((int)ViewportWindow.Bounds.Width, (int)ViewportWindow.Bounds.Height);

    public void ResetCam(int surfaceW, int surfaceH)
    {
        _cam.Zoom = 1.0f;
        var bounds = new { X = 0, Y = 0, W = 1024, H = 576 };
        double scale = Math.Min(surfaceW / bounds.W, surfaceH / bounds.H);
        _cam.Offset = new(0);
        _cam.Target = new(bounds.X, bounds.Y);
        _cam.Zoom = (float)scale;
    }

    ~Editor()
    {
        rlImGui.Shutdown();
        Rl.CloseWindow();
    }
}