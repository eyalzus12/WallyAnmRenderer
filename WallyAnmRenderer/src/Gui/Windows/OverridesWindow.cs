using System;
using System.IO;
using System.Threading.Tasks;
using ImGuiNET;
using NativeFileDialogSharp;

namespace WallyAnmRenderer;

public sealed class OverridesWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    private string? _originalPath = null;
    private string? _overridePath = null;

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
                    _originalPath = Path.GetRelativePath(brawlPath, result.Path).Replace('\\', '/');
            });
        }
        if (_originalPath is not null)
        {
            ImGui.Text($"Original swf: {_originalPath}");
        }

        if (ImGui.Button("Select new swf"))
        {
            Task.Run(() =>
            {
                DialogResult result = Dialog.FileOpen("swf");
                if (result.IsOk)
                    _overridePath = result.Path;
            });
        }
        if (_overridePath is not null)
        {
            ImGui.Text($"New swf: {_overridePath}");
        }

        if (ImGuiEx.DisabledButton("Add override", string.IsNullOrWhiteSpace(_overridePath) || string.IsNullOrWhiteSpace(_originalPath)))
        {
            string originalPath = _originalPath!;
            string overridePath = _overridePath!;
            async Task addOverride()
            {
                OnLoadingStarted();
                try
                {
                    SwfOverride swfOverride = await SwfOverride.Create(originalPath, overridePath);
                    assetLoader.AddSwfOverride(originalPath, swfOverride);
                    OnLoadingFinished();
                }
                catch (Exception e)
                {
                    OnLoadingError(e);
                }
            }
            _ = addOverride();

            _originalPath = null;
            _overridePath = null;
        }

        ImGui.SeparatorText("Overrides");
        foreach (SwfOverride swfOverride in assetLoader.Overrides)
        {
            if (ImGui.Button("X"))
            {
                assetLoader.RemoveSwfOverride(swfOverride.OverridePath);
            }
            ImGui.SameLine();
            ImGui.TextWrapped($"Override {swfOverride.OriginalPath} with {swfOverride.OverridePath}");
            if (ImGui.Button("Reload"))
            {
                async Task reload()
                {
                    OnLoadingStarted();
                    try
                    {
                        await assetLoader.ReloadOverride(swfOverride.OriginalPath);
                        OnLoadingFinished();
                    }
                    catch (Exception e)
                    {
                        OnLoadingError(e);
                    }
                }
                _ = reload();
            }

            ImGui.Separator();
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

    private void OnLoadingStarted()
    {
        _loadingError = null;
        _loadingStatus = "Loading...";
    }

    private void OnLoadingError(Exception e)
    {
        Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
        Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
        _loadingError = e.Message;
        _loadingStatus = null;
    }

    private void OnLoadingFinished()
    {
        _loadingError = null;
        _loadingStatus = null;
    }
}