using System;
using System.Collections.Generic;
using System.Text;
using BrawlhallaAnimLib.Gfx;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class GfxInfoWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    public void Show(IGfxType? gfxType)
    {
        Dictionary<ICustomArt, uint> customArtNode = new(new CustomArtHasher());
        Dictionary<IColorSwap, uint> colorSwapNode = new(new ColorSwapHasher());

        ImGui.Begin("GfxInfo", ref _open);

        if (gfxType is not null)
        {
            if (ImGui.TreeNodeEx("Sprite swaps", ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool hasAny = false;
                foreach (ICustomArt customArt in gfxType.CustomArts)
                {
                    hasAny = true;
                    uint nodeId = customArtNode.AddOrUpdate(customArt, 0u, x => x + 1);

                    // FileName Name (ArtType R)##ID
                    StringBuilder sb = new();
                    sb.Append(customArt.FileName);
                    sb.Append(' ');
                    sb.Append(customArt.Name);
                    sb.Append([' ', '(']);
                    sb.Append(ArtTypeToString(customArt.Type));
                    if (customArt.Right) sb.Append([' ', 'R']);
                    sb.Append([')', '#', '#']);
                    sb.Append(nodeId);
                    if (ImGui.TreeNode(sb.ToString()))
                    {
                        ImGui.Text($"Filename: {customArt.FileName}");
                        ImGui.Text($"Suffix: {customArt.Name}");
                        ImGui.Text($"Target: {ArtTypeToString(customArt.Type)}");
                        ImGui.Text($"For right sprite: {customArt.Right}");

                        ImGui.TreePop();
                    }
                }
                if (!hasAny)
                    ImGui.Text("None");
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Color swaps", ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool hasAny = false;
                foreach (IColorSwap colorSwap in gfxType.ColorSwaps)
                {
                    hasAny = true;
                    uint nodeId = colorSwapNode.AddOrUpdate(colorSwap, 0u, x => x + 1);
                    string typeString = ArtTypeToString(colorSwap.ArtType);

                    if (ImGui.TreeNode($"0x{colorSwap.OldColor:X06} -> 0x{colorSwap.NewColor:X06} {typeString}##{nodeId}"))
                    {
                        ImGui.Text($"Old Color: 0x{colorSwap.OldColor:X06}");
                        ImGui.SameLine();
                        ImGui.ColorButton("##old", ImGuiEx.RGBHexToVec4(colorSwap.OldColor), ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop);

                        ImGui.Text($"New Color: 0x{colorSwap.NewColor:X06}");
                        ImGui.SameLine();
                        ImGui.ColorButton("##new", ImGuiEx.RGBHexToVec4(colorSwap.NewColor), ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoDragDrop);

                        ImGui.Text($"Target: {typeString}");

                        ImGui.TreePop();
                    }
                }
                if (!hasAny)
                    ImGui.Text("None");
                ImGui.TreePop();
            }
        }
        else
        {
            ImGui.Text("Swz files were not loaded");
        }

        ImGui.End();
    }

    private static string ArtTypeToString(ArtTypeEnum artType) => artType switch
    {
        ArtTypeEnum.None => "Any",
        ArtTypeEnum.Weapon => "Weapon",
        ArtTypeEnum.Costume => "Legend",
        ArtTypeEnum.Pickup => "Weapon Spawn",
        ArtTypeEnum.Flag => "Taunt Avatar",
        ArtTypeEnum.Bot => "Sidekick",
        ArtTypeEnum.Companion => "Companion",
        _ => throw new IndexOutOfRangeException(),
    };

    private class CustomArtHasher : IEqualityComparer<ICustomArt>
    {
        public bool Equals(ICustomArt? x, ICustomArt? y)
        {
            if (x is null || y is null) return x == y;
            return x.FileName == y.FileName && x.Name == y.Name && x.Type == y.Type && x.Right == y.Right;
        }

        public int GetHashCode(ICustomArt obj)
        {
            return HashCode.Combine(obj.FileName, obj.Name, obj.Type, obj.Right);
        }
    }

    private class ColorSwapHasher : IEqualityComparer<IColorSwap>
    {
        public bool Equals(IColorSwap? x, IColorSwap? y)
        {
            if (x is null || y is null) return x == y;
            return x.OldColor == y.OldColor && x.NewColor == y.NewColor && x.ArtType == y.ArtType;
        }

        public int GetHashCode(IColorSwap obj)
        {
            return HashCode.Combine(obj.OldColor, obj.NewColor, obj.ArtType);
        }
    }
}