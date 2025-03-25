using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Gfx;

namespace WallyAnmRenderer;

public sealed class GfxType : IGfxType
{
    public required string AnimFile { get; set; }
    public required string AnimClass { get; set; }
    public double AnimScale { get; set; } = 1;
    public uint Tint { get; set; } = 0;
    public uint AsymmetrySwapFlags { get; set; } = 0;

    public List<ICustomArt> CustomArts { get; set; } = [];
    IEnumerable<ICustomArt> IGfxType.CustomArts => CustomArts;
    public List<IColorSwap> ColorSwaps { get; set; } = [];
    IEnumerable<IColorSwap> IGfxType.ColorSwaps => ColorSwaps;

    public bool UseRightTorso { get; set; }
    public bool UseRightJaw { get; set; }
    public bool UseRightEyes { get; set; }
    public bool UseRightMouth { get; set; }
    public bool UseRightHair { get; set; }
    public bool UseRightForearm { get; set; }
    public bool UseRightShoulder1 { get; set; }
    public bool UseRightLeg1 { get; set; }
    public bool UseRightShin { get; set; }
    public bool UseTrueLeftRightHands { get; set; }
    public bool HidePaperDollRightPistol { get; set; }
    public bool UseRightGauntlet { get; set; }
    public bool UseRightKatar { get; set; }
    public bool HideRightPistol2D { get; set; }
    public bool UseTrueLeftRightTorso { get; set; }

    public Dictionary<string, string> BoneOverride { get; set; } = [];
    IReadOnlyDictionary<string, string> IGfxType.BoneOverride => BoneOverride;

    public GfxType() { }

    [SetsRequiredMembers]
    public GfxType(IGfxType gfx)
    {
        AnimFile = gfx.AnimFile;
        AnimClass = gfx.AnimClass;
        AnimScale = gfx.AnimScale;
        Tint = gfx.Tint;
        AsymmetrySwapFlags = gfx.AsymmetrySwapFlags;
        CustomArts = [.. gfx.CustomArts];
        ColorSwaps = [.. gfx.ColorSwaps];
        UseRightTorso = gfx.UseRightTorso;
        UseRightJaw = gfx.UseRightJaw;
        UseRightEyes = gfx.UseRightEyes;
        UseRightMouth = gfx.UseRightMouth;
        UseRightHair = gfx.UseRightHair;
        UseRightForearm = gfx.UseRightForearm;
        UseRightShoulder1 = gfx.UseRightShoulder1;
        UseRightLeg1 = gfx.UseRightLeg1;
        UseRightShin = gfx.UseRightShin;
        UseTrueLeftRightHands = gfx.UseTrueLeftRightHands;
        HidePaperDollRightPistol = gfx.HidePaperDollRightPistol;
        UseRightGauntlet = gfx.UseRightGauntlet;
        UseRightKatar = gfx.UseRightKatar;
        HideRightPistol2D = gfx.HideRightPistol2D;
        UseTrueLeftRightTorso = gfx.UseTrueLeftRightTorso;
        BoneOverride = new(gfx.BoneOverride);
    }
}