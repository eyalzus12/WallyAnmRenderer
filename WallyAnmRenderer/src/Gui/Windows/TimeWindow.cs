using System;
using System.Numerics;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class TimeWindow
{
    private static readonly Vector4 Green = new(0, 1, 0, 1);
    private static readonly Vector4 Yellow = new(1, 1, 0, 1);

    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public event EventHandler<long>? FrameSeeked;
    public event EventHandler<long>? FrameMove;
    public event EventHandler<bool>? Paused;

    public void Show(long frames, TimeSpan time, bool paused)
    {
        ImGui.Begin("Timeline", ref _open);

        if (frames > 0)
        {
            int textPad = (int)Math.Floor(Math.Log10(frames)) + 1;

            long currentFrame = (long)Math.Floor(24 * time.TotalSeconds);
            currentFrame = MathUtils.SafeMod(currentFrame, frames);
            for (long i = 0; i < frames; ++i)
            {
                if (i % 15 != 0) ImGui.SameLine();

                string text = i.ToString().PadLeft(textPad);
                ImGui.Text(text);
                ImGui.SameLine();
                Vector4 color = i == currentFrame ? Yellow : Green;
                if (ImGui.ColorButton(text, color, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop))
                {
                    FrameSeeked?.Invoke(this, i);
                }
                ImGui.SameLine();
                ImGui.Spacing();
            }
        }

        if (ImGui.Button(paused ? "Unpause" : "Pause"))
            Paused?.Invoke(this, !paused);

        if (ImGui.Button("Prev frame"))
            FrameMove?.Invoke(this, -1);
        ImGui.SameLine();
        if (ImGui.Button("Next frame"))
            FrameMove?.Invoke(this, 1);

        ImGui.End();
    }
}