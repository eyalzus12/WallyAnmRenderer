using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class CustomColorEditPopup(string? id = null)
{
    public const string NAME = "Edit color";
    private string PopupName => $"{NAME}{(id is null ? "" : $"##{id}")}";

    private bool _shouldOpen;
    private bool _open = false;
    public void Open() => _shouldOpen = true;

    public event EventHandler? SaveRequested;

    private bool _saving = false;
    private readonly List<string> _errors = [];

    public void Update(ColorScheme color, ColorScheme originalColor, IEnumerable<ColorScheme> otherColors)
    {
        if (_shouldOpen)
        {
            ImGui.OpenPopup(PopupName);
            _shouldOpen = false;
            _open = true;
            _saving = false;
            _errors.Clear();
        }

        if (!ImGui.BeginPopupModal(PopupName, ref _open)) return;

        string name = color.Name;
        ImGui.BeginDisabled(_saving);
        if (ImGui.InputText("Name", ref name, 256))
            color.Name = name;
        ImGui.EndDisabled();
        bool valid = !otherColors.Any((color2) => originalColor != color2 && color.Name == color2.Name);

        if (!valid)
        {
            ImGui.PushTextWrapPos();
            ImGui.TextColored(new(1, 1, 0, 1), "Two custom colors cannot have the same name!");
            ImGui.PopTextWrapPos();
        }

        ImGui.SeparatorText("Editing");
        ImGui.PushTextWrapPos();
        ImGui.Text("Pure black (");
        ImGui.SameLine(0, 0);
        ImGui.TextColored(new(0, 0, 0, 1), "#000000");
        ImGui.SameLine(0, 0);
        ImGui.Text(") means \"don't swap\"");
        ImGui.PopTextWrapPos();
        CustomColorComponent.MainTable(PopupName, color);
        ImGui.TextWrapped("Note that no color scheme in the game uses these.");
        CustomColorComponent.HandsTable(PopupName, color);

        ImGui.SeparatorText("Save");
        if (ImGuiEx.DisabledButton("Save", _saving || !valid))
            SaveRequested?.Invoke(this, EventArgs.Empty);
        if (_saving)
        {
            ImGui.SameLine();
            ImGui.Text("Saving...");
        }

        if (_errors.Count > 0)
        {
            ImGui.SeparatorText("Errors");
            if (ImGui.Button("Clear"))
                _errors.Clear();
            foreach (string error in _errors)
            {
                ImGui.TextWrapped($"[Error]: {error}");
            }
        }

        ImGui.EndPopup();
    }

    public async Task SaveAsync(ColorScheme color, string originalName)
    {
        _saving = true;
        string folderPath = CustomColorList.FolderPath;
        string fileName = Path.Combine(folderPath, $"{color.Name}{CustomColorList.FILE_EXTENSION}");
        Directory.CreateDirectory(folderPath);

        if (color.Name != originalName)
        {
            string deletedPath = Path.Combine(folderPath, originalName);
            if (File.Exists(deletedPath))
                File.Delete(deletedPath);
        }

        try
        {
            using FileStream file = new(fileName, FileMode.Open, FileAccess.Write);
            await CustomColorList.WriteColorSchemeAsync(color, file);
        }
        catch (Exception e)
        {
            _errors.Add($"Exception while saving {color.Name}: {e.Message}");
        }

        _saving = false;
    }
}