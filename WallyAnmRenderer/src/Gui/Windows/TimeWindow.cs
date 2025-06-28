using System;
using System.Numerics;
using BrawlhallaAnimLib;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class TimeWindow
{
    private const int BASE_WIDTH = 34;
    private const int TEXT_WIDTH_MULT = 7;
    private static readonly Vector4 Orange = new(1, 0.5f, 0, 1);
    private static readonly Vector4 Green = new(0, 1, 0, 1);
    private static readonly Vector4 Cyan = new(0, 1, 1, 1);
    private static readonly Vector4 Yellow = new(1, 1, 0, 1);

    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public event EventHandler<long>? FrameSeeked;
    public event EventHandler<long>? FrameMove;
    public event EventHandler<bool>? Paused;

    public void Show(AnimationData data, TimeSpan time, bool paused)
    {
        ImGui.Begin("Timeline", ref _open);

        long frames = data.FrameCount;
        uint loopStart = data.LoopStart;
        uint recoveryStart = data.RecoveryStart;
        if (frames > 0)
        {
            int textPad = (int)Math.Floor(Math.Log10(frames)) + 1;
            int neededWidth = BASE_WIDTH + textPad * TEXT_WIDTH_MULT;

            long currentFrame = (long)Math.Floor(24 * time.TotalSeconds);
            currentFrame = MathUtils.SafeMod(currentFrame, frames);
            for (long i = 0; i < frames; ++i)
            {
                float width = ImGui.GetContentRegionAvail().X;
                int columns = (int)Math.Floor(width / neededWidth);
                if (columns != 0 && i % columns != 0) ImGui.SameLine();

                string text = i.ToString().PadLeft(textPad);
                ImGui.Text(text);
                ImGui.SameLine();
                Vector4 color =
                    i == currentFrame ? Yellow :
                    i == loopStart ? Cyan :
                    i == recoveryStart ? Orange :
                    Green;
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

        ImGui.SeparatorText("Raw info");
        ImGui.Text($"Frame count: {frames}");
        ImGui.Text($"Base start: {data.BaseStart}");
        ImGui.Text($"Preview frame: {data.PreviewFrame}");
        ImGui.Text($"Loop start: {loopStart}");
        ImGui.Text($"Recovery start: {recoveryStart}");
        ImGui.Text($"Free start: {data.FreeStart}");

        ImGui.End();
    }
}