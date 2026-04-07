using System;
using System.Collections.Generic;
using ImGuiNET;

namespace WallyAnmRenderer;

public readonly struct PickerListBox<T>()
{
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
                if (selected) ImGui.PushStyleColor(ImGuiCol.Text, Colors.SELECTED_OPTION);
                if (ImGui.Selectable(optionText, selected))
                    OnSelect(option);
                if (selected) ImGui.PopStyleColor();
            }

            ImGui.EndListBox();
        }
    }
}