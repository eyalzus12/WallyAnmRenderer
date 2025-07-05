using System;
using System.IO;
using System.Threading.Tasks;
using ImGuiNET;
using NativeFileDialogSharp;
using SwfLib;

namespace WallyAnmRenderer;

public sealed class OverridesWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    private string? _overridePath = null;
    private string? _filePath = null;

    private string? _loadingError;
    private string? _loadingStatus;

    public void Show(string? brawlPath, AssetLoader? assetLoader)
    {
        ImGui.Begin("Overrides", ref _open);

        if (brawlPath is null)
        {
            ImGui.TextWrapped("You must select your brawlhalla path to add overrides");
            ImGui.End();
            return;
        }

        if (assetLoader is null)
        {
            ImGui.End();
            return;
        }

        ImGui.SeparatorText("Add new");
        if (ImGui.Button("Select original swf"))
        {
            Task.Run(() =>
            {
                DialogResult result = Dialog.FileOpen("swf", brawlPath);
                if (result.IsOk)
                    _overridePath = Path.GetRelativePath(brawlPath, result.Path).Replace('\\', '/');
            });
        }
        if (_overridePath is not null)
        {
            ImGui.Text($"Original swf: {_overridePath}");
        }

        if (ImGui.Button("Select new swf"))
        {
            Task.Run(() =>
            {
                DialogResult result = Dialog.FileOpen("swf");
                if (result.IsOk)
                    _filePath = result.Path;
            });
        }
        if (_filePath is not null)
        {
            ImGui.Text($"New swf: {_filePath}");
        }

        if (ImGuiEx.DisabledButton("Add override", string.IsNullOrWhiteSpace(_overridePath) || string.IsNullOrWhiteSpace(_filePath)))
        {
            try
            {
                string overridePath = _overridePath!;
                string filePath = _filePath!;
                Task.Run(() =>
                {
                    SwfFile swfFile;
                    using (FileStream file = File.OpenRead(filePath))
                        swfFile = SwfFile.ReadFrom(file);
                    SwfFileData data = SwfFileData.CreateFrom(swfFile);
                    assetLoader.AddSwfOverride(overridePath, new(overridePath, filePath, data));
                });
            }
            catch (Exception e)
            {
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
                _loadingError = e.Message;
            }
            _overridePath = null;
            _filePath = null;
        }

        ImGui.SeparatorText("Overrides");
        foreach (SwfOverride swfOverride in assetLoader.Overrides)
        {
            ImGui.BulletText($"Override {swfOverride.OverridePath} with {swfOverride.OriginalPath}");
        }

        if (_loadingStatus is not null)
        {
            ImGui.PushTextWrapPos();
            ImGui.Text("[Status]: " + _loadingStatus);
            ImGui.PopTextWrapPos();
        }

        if (_loadingError is not null)
        {
            ImGui.PushTextWrapPos();
            ImGui.Text("[Error]: " + _loadingError);
            ImGui.PopTextWrapPos();
        }

        ImGui.End();
    }

    public void OnLoadingStarted()
    {
        _loadingError = null;
        _loadingStatus = "Loading...";
    }

    public void OnLoadingError(Exception e)
    {
        _loadingError = e.Message;
        _loadingStatus = null;
    }

    public void OnLoadingFinished()
    {
        _loadingError = null;
        _loadingStatus = null;
    }
}