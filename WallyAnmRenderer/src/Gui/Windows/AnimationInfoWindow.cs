using System.Collections.Generic;
using ImGuiNET;
using BrawlhallaAnimLib.Bones;
using BrawlhallaAnimLib.Math;

namespace WallyAnmRenderer;

public sealed class AnimationInfoWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public void Show(BoneSprite[]? sprites, ref BoneSprite? highlight)
    {
        Dictionary<string, uint> spriteNameNode = [];
        Dictionary<uint, uint> spriteIdNode = [];
        Dictionary<string, uint> boneNameNode = [];

        ImGui.Begin("Bones", ref _open);

        if (sprites is not null)
        {
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

                if (ImGui.TreeNode($"{spriteText}##{nodeId}"))
                {
                    if (ImGui.IsItemHovered())
                    {
                        highlight = sprite;
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