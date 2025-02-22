using System;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using BrawlhallaAnimLib.Reading.WeaponSkinTypes;

namespace WallyAnmRenderer;

public sealed class GfxInfo
{
    public string? SourceFilePath { get; set; }

    public string? AnimClass { get; set; }
    public string? AnimFile { get; set; }
    public string? Animation { get; set; }
    public double AnimScale { get; set; } = 2;

    public string? CostumeType { get; set; }
    public string? WeaponSkinType { get; set; }
    public string? ColorScheme { get; set; }

    [MemberNotNullWhen(true, nameof(AnimClass))]
    [MemberNotNullWhen(true, nameof(AnimFile))]
    [MemberNotNullWhen(true, nameof(Animation))]
    public bool AnimationPicked => AnimClass is not null && AnimFile is not null && Animation is not null;

    public (IGfxType gfx, string animation)? ToGfxType(SwzGameFile gameFiles)
    {
        if (AnimClass is null || AnimFile is null || Animation is null)
            return null;

        ColorExceptionTypes colorExceptionTypes = gameFiles.ColorExceptionTypes;

        IGfxType gfx = new GfxType()
        {
            AnimFile = AnimFile,
            AnimClass = AnimClass,
            AnimScale = AnimScale,
        };

        ColorSchemeTypes colorSchemeTypes = gameFiles.ColorSchemeTypes;
        ColorScheme? scheme = null;
        if (ColorScheme is not null)
        {
            if (!colorSchemeTypes.TryGetColorScheme(ColorScheme, out scheme))
            {
                throw new ArgumentException($"Invalid color scheme {ColorScheme}");
            }
        }

        CostumeTypes costumeTypes = gameFiles.CostumeTypes;
        CostumeTypesGfx? costumeGfx = null;
        if (CostumeType is not null)
        {
            if (costumeTypes.TryGetGfx(CostumeType, out costumeGfx))
            {
                gfx = costumeGfx.ToGfxType(gfx, scheme, colorExceptionTypes);
            }
            else
            {
                throw new ArgumentException($"Invalid costume type {CostumeType}");
            }
        }

        WeaponSkinTypes weaponSkinTypes = gameFiles.WeaponSkinTypes;
        if (WeaponSkinType is not null)
        {
            if (weaponSkinTypes.TryGetGfx(WeaponSkinType, out WeaponSkinTypesGfx? weaponSkinGfx))
            {
                gfx = weaponSkinGfx.ToGfxType(gfx, scheme, colorExceptionTypes, costumeGfx);
            }
            else
            {
                throw new ArgumentException($"Invalid weapon skin type {WeaponSkinType}");
            }
        }

        return (gfx, Animation);
    }
}