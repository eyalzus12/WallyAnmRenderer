using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using BrawlhallaAnimLib.Gfx;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class CustomColorList
{
    public const string CUSTOM_COLORS_FOLDER = "CustomColors";
    public const string FILE_EXTENSION = ".wcolor";

    public static string FolderPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        PathPreferences.APPDATA_DIR_NAME,
        CUSTOM_COLORS_FOLDER
    );

    private List<ColorScheme> _colors = [];
    private string _colorFilter = "";

    private Task<List<ColorScheme>>? _loadingTask = null;
    private readonly List<string> _errors = [];

    private ColorScheme? _originalColor = null;
    private ColorScheme? _editedColor = null;
    private readonly CustomColorEditPopup _editModal = new();

    public CustomColorList()
    {
        _loadingTask = RefreshColorList();

        _editModal.SaveRequested += async (_, _) =>
        {
            if (_editedColor is null || _originalColor is null)
                return;

            await _editModal.Save(_editedColor, _originalColor.Name);
            // replace color with new one
            int index = _colors.FindIndex((c) => c == _originalColor);
            if (index != -1)
                _colors[index] = _editedColor;
            // promote edited object
            _originalColor = _editedColor;
            _editedColor = new(_originalColor);
        };
    }

    public void Show()
    {
        bool loadingColors = _loadingTask is not null;
        if (_loadingTask is not null)
        {
            if (_loadingTask.IsCompletedSuccessfully)
            {
                _colors = _loadingTask.Result;
                _loadingTask = null;
            }
            else if (_loadingTask.IsCanceled || _loadingTask.IsFaulted)
            {
                _loadingTask = null;
            }
        }

        if (ImGuiEx.DisabledButton("Refresh list", loadingColors))
            _loadingTask = RefreshColorList();
        if (loadingColors)
        {
            ImGui.SameLine();
            ImGui.Text("Loading...");
        }

        if (ImGuiEx.DisabledButton("Create new", loadingColors))
        {
            string? name = null;
            for (int i = 1; i <= 100; ++i)
            {
                string fileName = $"My Custom Color {i}";
                if (!_colors.Any((color) => color.Name == fileName))
                {
                    name = fileName;
                    break;
                }
            }
            if (name is null)
                throw new Exception("Organize your fucking colors");

            string filePath = Path.Combine(FolderPath, $"{name}{FILE_EXTENSION}");
            _colors.Add(new(name));
        }

        ImGui.InputText("Filter colors", ref _colorFilter, 256);

        ImGuiEx.BeginStyledChild("Custom colors");
        HashSet<ColorScheme>? deleted = null;
        for (int i = 0; i < _colors.Count; ++i)
        {
            ColorScheme color = _colors[i];
            if (!color.Name.Contains(_colorFilter, StringComparison.CurrentCultureIgnoreCase))
                continue;

            ImGui.Text($"{color.Name}");
            ImGui.SameLine();
            if (ImGuiEx.DisabledButton($"Edit##{color.GetHashCode()}", loadingColors))
            {
                _originalColor = color;
                _editedColor = new(_originalColor);
                _editModal.Open();
            }
            ImGui.SameLine();
            if (ImGuiEx.DisabledButton($"X##{color.GetHashCode()}", loadingColors))
            {
                deleted ??= [];
                deleted.Add(color);

                string deletedPath = Path.Combine(FolderPath, $"{color.Name}{FILE_EXTENSION}");
                if (File.Exists(deletedPath))
                    File.Delete(deletedPath);
            }
        }

        if (deleted is not null)
            _colors = [.. _colors.Where((c) => !deleted.Contains(c))];
        ImGuiEx.EndStyledChild();

        if (_editedColor is not null && _originalColor is not null)
        {
            _editModal.Update(_editedColor, _originalColor, _colors);
        }

        if (_errors.Count > 0)
        {
            ImGui.SeparatorText("Errors");
            if (ImGui.Button("Clear"))
                _errors.Clear();
            foreach (string error in _errors)
            {
                ImGui.Text($"[Error]: {error}");
            }
        }
    }

    public async Task<List<ColorScheme>> RefreshColorList()
    {
        if (!Directory.Exists(FolderPath))
        {
            return [];
        }

        string[] files = Directory.GetFiles(FolderPath);

        List<ColorScheme> result = [];
        foreach (string file in files)
        {
            string extension = Path.GetExtension(file);
            if (extension != FILE_EXTENSION) continue;
            string name = Path.GetFileNameWithoutExtension(file);

            try
            {
                using FileStream stream = File.OpenRead(file);
                ColorScheme colorScheme = await ParseColorSchemeAsync(name, stream);
                result.Add(colorScheme);
            }
            catch (Exception e)
            {
                _errors.Add($"exception while loading color {file}: {e.Message}");
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Error, e.Message);
                Rl.TraceLog(Raylib_cs.TraceLogLevel.Trace, e.StackTrace);
            }
        }
        return result;
    }

    public static async Task<ColorScheme> ParseColorSchemeAsync(string name, Stream stream)
    {
        ColorScheme result = new(name);

        using StreamReader reader = new(stream);
        while ((await reader.ReadLineAsync()) is string line)
        {
            string[] parts = line.Split('=');
            if (parts.Length < 2)
                throw new FormatException($"Invalid line format: no '='");

            string typeString = parts[0];
            if (!Enum.TryParse(typeString, out ColorSchemeSwapEnum type))
                throw new FormatException($"Invalid swap type {typeString}");

            string colorString = parts[1];
            if (!uint.TryParse(colorString, NumberStyles.HexNumber, null, out uint color))
                throw new FormatException($"Invalid color ${colorString}");

            result[type] = color;
        }

        return result;
    }

    public static async Task WriteColorSchemeAsync(ColorScheme scheme, Stream stream)
    {
        using StreamWriter writer = new(stream);
        foreach (ColorSchemeSwapEnum swap in Enum.GetValues<ColorSchemeSwapEnum>())
        {
            uint color = scheme[swap];
            if (color == 0) continue;
            await writer.WriteLineAsync($"{swap}={color:X6}");
        }
    }
}