using System.Collections.Generic;
using ImGuiNET;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Math;

namespace WallyAnmRenderer;

public sealed class AnimationInfoWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public void Show(Transform2D parentTransform, BoneSpriteWithName[]? sprites, ref BoneSpriteWithName? highlight)
    {
        Transform2D.Invert(parentTransform, out Transform2D inverseParentTransform);

        Dictionary<string, uint> spriteIdDict = [];

        ImGui.Begin("Bones", ref _open);

        if (sprites is not null)
        {
            foreach (BoneSpriteWithName sprite in sprites)
            {
                uint spriteId = spriteIdDict.AddOrUpdate(sprite.SpriteName, 0u, x => x + 1);

                if (ImGui.TreeNode($"{sprite.SpriteName}##{spriteId}"))
                {
                    if (ImGui.IsItemHovered())
                    {
                        highlight = sprite;
                    }
                    ImGui.Text($"File: {sprite.SwfFilePath}");
                    ImGui.Text($"Frame: {sprite.Frame}");
                    Transform2D transform = inverseParentTransform * sprite.Transform;
                    ImGui.SeparatorText("Transform");
                    ImGui.Text($"X: {transform.TranslateX}");
                    ImGui.Text($"Y: {transform.TranslateY}");
                    ImGui.Text($"ScaleX: {transform.ScaleX}");
                    ImGui.Text($"ScaleY: {transform.ScaleY}");
                    ImGui.Text($"RotateSkew0: {transform.SkewY}");
                    ImGui.Text($"RotateSkew1: {transform.SkewX}");

                    ImGui.TreePop();
                }
                else if (ImGui.IsItemHovered())
                {
                    highlight = sprite;
                }
            }
        }
        else
        {
            ImGui.Text("Animation loading...");
        }

        ImGui.TextWrapped("Note that some of these may not be real sprites. The game (and program) simply ignore sprites that don't exist");

        ImGui.End();
    }
}