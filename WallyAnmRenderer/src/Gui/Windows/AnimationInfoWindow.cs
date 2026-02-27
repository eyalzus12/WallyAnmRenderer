using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Math;

namespace WallyAnmRenderer;

public sealed class AnimationInfoWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public void Show(BoneSprite[]? sprites, ref BoneSprite? highlighted, ref RlColor highlightTint)
    {
        Dictionary<string, uint> spriteNameNode = [];
        Dictionary<uint, uint> spriteIdNode = [];
        Dictionary<string, uint> boneNameNode = [];

        ImGui.Begin("Bones", ref _open);

        if (sprites is not null)
        {
            ImGui.TextWrapped("Hover on an item from this list to highlight it in the viewport");
            Vector3 highlightTint2 = RaylibUtils.RlColorToVector3(highlightTint);
            if (ImGui.ColorEdit3("Highlight tint", ref highlightTint2, ImGuiColorEditFlags.NoInputs))
                highlightTint = RaylibUtils.Vector3ToRlColor(highlightTint2);

            foreach (BoneSprite sprite in sprites)
            {
                string spriteText;
                uint nodeId;
                if (sprite is SwfBoneSpriteWithId swfBoneSpriteWithId)
                {
                    spriteText = swfBoneSpriteWithId.SpriteId.ToString();
                    nodeId = spriteIdNode.AddOrUpdate(swfBoneSpriteWithId.SpriteId, 0u, x => x + 1);
                }
                else if (sprite is SwfBoneSpriteWithName swfBoneSpriteWithName)
                {
                    spriteText = swfBoneSpriteWithName.SpriteName;
                    nodeId = spriteNameNode.AddOrUpdate(swfBoneSpriteWithName.SpriteName, 0u, x => x + 1);
                }
                else if (sprite is BitmapBoneSprite bitmapSprite)
                {
                    spriteText = $"{bitmapSprite.SpriteData.BoneName} {bitmapSprite.SpriteData.SetName}";
                    nodeId = boneNameNode.AddOrUpdate(spriteText, 0u, x => x + 1);
                }
                else
                {
                    spriteText = "ERROR-UNKNOWN";
                    nodeId = (uint)sprite.GetHashCode();
                }

                ImGui.PushID((int)nodeId);
                if (ImGui.TreeNode(spriteText))
                {
                    if (ImGui.IsItemHovered())
                    {
                        highlighted = sprite;
                    }

                    if (sprite is SwfBoneSprite boneSprite)
                    {
                        ImGui.Text($"File: {boneSprite.SwfFilePath}");
                        ImGui.Text($"Frame: {boneSprite.Frame}");
                    }
                    else if (sprite is BitmapBoneSprite bitmapSprite)
                    {
                        ImGui.Text($"BoneName: {bitmapSprite.SpriteData.BoneName}");
                        ImGui.Text($"SetName: {bitmapSprite.SpriteData.SetName}");
                        ImGui.Text($"File: {bitmapSprite.SpriteData.File}");
                    }
                    Transform2D transform = sprite.Transform;

                    ImGui.SeparatorText("Transform");
                    ImGui.Text($"X: {transform.TranslateX}");
                    ImGui.Text($"Y: {transform.TranslateY}");
                    ImGui.Text($"ScaleX: {transform.ScaleX}");
                    ImGui.Text($"ScaleY: {transform.ScaleY}");
                    ImGui.Text($"RotateSkew0: {transform.SkewY}");
                    ImGui.Text($"RotateSkew1: {transform.SkewX}");

                    ImGui.SeparatorText("Effects");
                    ImGui.Text($"Opacity: {sprite.Opacity}");

                    ImGui.TreePop();
                }
                else if (ImGui.IsItemHovered())
                {
                    highlighted = sprite;
                }
                ImGui.PopID();
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