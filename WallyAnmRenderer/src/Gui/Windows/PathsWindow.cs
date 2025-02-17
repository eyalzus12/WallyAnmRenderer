using System;
using System.IO;
using System.Threading.Tasks;
using ImGuiNET;
using NativeFileDialogSharp;

namespace WallyAnmRenderer;

public sealed class PathsWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    private string? _brawlPath = null;
    private uint? _key = null;

    private string? _loadingError;
    private string? _loadingStatus;

    public void Show(PathPreferences pathPrefs)
    {
        string? brawlPath = _brawlPath ?? pathPrefs.BrawlhallaPath;
        uint? key = _key ?? pathPrefs.DecryptionKey;

        ImGui.SetNextWindowSizeConstraints(new(500, 400), new(int.MaxValue));
        ImGui.Begin("Paths", ref _open, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse);

        if (ImGui.Button("Select Brawlhalla Path"))
        {
            Task.Run(() =>
            {
                DialogResult result = Dialog.FolderPicker(brawlPath);
                if (result.IsOk)
                    _brawlPath = result.Path;
            });
        }
        ImGui.Text($"Path: {brawlPath}");
        ImGui.Separator();

        string keyInput = key?.ToString() ?? "";
        if (ImGui.InputText("Decryption key", ref keyInput, 9, ImGuiInputTextFlags.CharsDecimal))
        {
            if (uint.TryParse(keyInput, out uint newKey))
            {
                _key = newKey;
            }
        }

        if (brawlPath is not null && ImGui.Button("Find key"))
        {
            Task.Run(() =>
            {
                try
                {
                    _loadingError = null;
                    _loadingStatus = "searching key...";
                    _key = AbcUtils.FindDecryptionKeyFromPath(Path.Combine(brawlPath, "BrawlhallaAir.swf"));
                }
                catch (Exception e)
                {
                    _loadingError = e.Message;
                }
                finally
                {
                    _loadingStatus = null;
                }
            });
        }

        if ((key is not null || brawlPath is not null) && ImGui.Button("Load files"))
        {
            if (key is not null)
                pathPrefs.DecryptionKey = key;
            if (brawlPath is not null)
                pathPrefs.BrawlhallaPath = brawlPath;
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
}