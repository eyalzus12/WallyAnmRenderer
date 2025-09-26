using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace WallyAnmRenderer;

public readonly struct PickerListBox<T>()
{
    private static readonly Vector4 SELECTED_COLOR = ImGuiEx.RGBHexToVec4(0xFF7F00);

    public string ListLabel { get; init; } = string.Empty;
    public required IEnumerable<T> Options { get; init; }
    public required Func<T, string> OptionToString { get; init; }
    public required Action<T> OnSelect { get; init; }
    public Func<T, string, bool>? ShouldShow { get; init; }

    public void Show(T? value)
    {
        if (ImGui.BeginListBox(ListLabel))
        {
            foreach (T option in Options)
            {
                string optionText = OptionToString(option);

                if (ShouldShow is not null && !ShouldShow(option, optionText))
                    continue;

                bool selected = Equals(option, value);
                if (selected) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
                if (ImGui.Selectable(optionText, selected))
                    OnSelect(option);
                if (selected) ImGui.PopStyleColor();
            }

            ImGui.EndListBox();
        }
    }
}