using System;
using BrawlhallaAnimLib.Gfx;

namespace WallyAnmRenderer;

internal static class EnumStringDicts
{
    private static readonly string[] MouthOverrides;
    private static readonly string[] EyesOverrides;

    static EnumStringDicts()
    {
        int mouthOverrideCount = 1 + (int)GfxMouthOverride.Neutral;
        MouthOverrides = new string[mouthOverrideCount];
        MouthOverrides[(int)GfxMouthOverride.NoChange] = "None";
        MouthOverrides[(int)GfxMouthOverride.Warcry] = "Warcry";
        MouthOverrides[(int)GfxMouthOverride.Smile] = "Smile";
        MouthOverrides[(int)GfxMouthOverride.KO] = "KO";
        MouthOverrides[(int)GfxMouthOverride.Hit] = "Hit";
        MouthOverrides[(int)GfxMouthOverride.Growl] = "Growl";
        MouthOverrides[(int)GfxMouthOverride.Whistle] = "Whistle";
        MouthOverrides[(int)GfxMouthOverride.Neutral] = "Neutral";

        int eyeOverrideCount = 1 + (int)GfxEyesOverride.Neutral;
        EyesOverrides = new string[eyeOverrideCount];
        EyesOverrides[(int)GfxEyesOverride.NoChange] = "None";
        EyesOverrides[(int)GfxEyesOverride.LookSide] = "Look side";
        EyesOverrides[(int)GfxEyesOverride.KO] = "KO";
        EyesOverrides[(int)GfxEyesOverride.Hit] = "Hit";
        EyesOverrides[(int)GfxEyesOverride.LookDown] = "Look down";
        EyesOverrides[(int)GfxEyesOverride.Angry] = "Angry";
        EyesOverrides[(int)GfxEyesOverride.Neutral] = "Neutral";
    }

    public static string GetMouthOverridesString(GfxMouthOverride mouthOverride)
    {
        int overrideIndex = (int)mouthOverride;
        if (mouthOverride < 0 || overrideIndex >= MouthOverrides.Length)
            throw new ArgumentException($"Invalid mouth override enum value {mouthOverride}");
        return MouthOverrides[overrideIndex];
    }

    public static string GetEyesOverridesString(GfxEyesOverride eyesOverride)
    {
        int overrideIndex = (int)eyesOverride;
        if (eyesOverride < 0 || overrideIndex >= EyesOverrides.Length)
            throw new ArgumentException($"Invalid eyes override enum value {eyesOverride}");
        return EyesOverrides[overrideIndex];
    }
}