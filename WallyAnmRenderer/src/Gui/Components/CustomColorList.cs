using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using BrawlhallaAnimLib.Gfx;
using ImGuiNET;
using NativeFileDialogSharp;

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

            await _editModal.SaveAsync(_editedColor, _originalColor.Name);
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
        bool loadingColors = _loadingTask is not null;

        if (ImGuiEx.DisabledButton("Load from file", loadingColors))
        {
            Task.Run(() => Dialog.FileOpen(FILE_EXTENSION[1..])).ContinueWith(async (task) =>
            {
                DialogResult result = task.Result;

                if (result.IsOk)
                {
                    string path = result.Path;
                    string name = Path.GetFileNameWithoutExtension(path);
                    ColorScheme newColor;
                    using (FileStream file = File.OpenRead(path))
                        newColor = await ParseColorSchemeAsync(name, file);
                    _colors.Add(newColor);
                }
                else if (result.IsError)
                {
                    _errors.Add(result.ErrorMessage);
                }
            });
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

        async Task<ColorScheme?> read(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);

            try
            {
                using FileStream stream = File.OpenRead(path);
                ColorScheme colorScheme = await ParseColorSchemeAsync(name, stream);
                return colorScheme;
            }
            catch (Exception e)
            {
                _errors.Add($"Exception while loading {name}: {e.Message}");
                return null;
            }
        }

        string[] files = Directory.GetFiles(FolderPath);
        List<Task<ColorScheme?>> tasks = new(files.Length);
        foreach (string file in files)
        {
            string extension = Path.GetExtension(file);
            if (extension != FILE_EXTENSION) continue;
            tasks.Add(read(file));
        }
        ColorScheme?[] result = await Task.WhenAll(tasks);

        return [.. result.Where((c) => c is not null)];
    }

    public static async Task<ColorScheme> ParseColorSchemeAsync(string name, Stream stream)
    {
        ColorScheme result = new(name);

        int lineNum = 0;
        using StreamReader reader = new(stream);
        while ((await reader.ReadLineAsync()) is string line)
        {
            string[] parts = line.Split('=');
            if (parts.Length < 2)
                throw new FormatException($"Missing '=' in line {lineNum}");

            string typeString = parts[0].Trim();
            if (!Enum.TryParse(typeString, out ColorSchemeSwapEnum type))
                throw new FormatException($"Invalid swap type {typeString} in line {lineNum}");

            string colorString = parts[1].Trim();
            if (colorString[0] != '#')
                throw new FormatException($"Missing '#' before color in line {lineNum}");
            if (!uint.TryParse(colorString[1..].Trim(), NumberStyles.HexNumber, null, out uint color))
                throw new FormatException($"Invalid color ${colorString} in line {lineNum}");

            result[type] = color;

            lineNum++;
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
            await writer.WriteLineAsync($"{swap}=#{color:X6}");
        }
    }
}