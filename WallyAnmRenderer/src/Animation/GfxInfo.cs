using System;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using BrawlhallaAnimLib.Reading.WeaponSkinTypes;

namespace WallyAnmRenderer;

public sealed class GfxInfo : IGfxInfo
{
    public string? SourceFilePath { get; set; }

    public string? AnimClass { get; set; }
    public string? AnimFile { get; set; }
    public string? Animation { get; set; }
    public double AnimScale { get; set; } = 2;
    public bool Flip { get; set; } = false;

    public string? CostumeType { get; set; }
    public string? WeaponSkinType { get; set; }
    public string? SpawnBotType { get; set; }
    public ColorScheme? ColorScheme { get; set; }

    [MemberNotNullWhen(true, nameof(AnimClass))]
    [MemberNotNullWhen(true, nameof(AnimFile))]
    [MemberNotNullWhen(true, nameof(Animation))]
    public bool AnimationPicked => AnimClass is not null && AnimFile is not null && Animation is not null;

    public (IGfxType gfx, string animation, bool flip)? ToGfxType(SwzGameFile gameFiles)
    {
        if (!AnimationPicked)
            return null;

        ColorExceptionTypes colorExceptionTypes = gameFiles.ColorExceptionTypes;

        IGfxType gfx = new GfxType()
        {
            AnimFile = AnimFile,
            AnimClass = AnimClass,
            AnimScale = AnimScale,
        };

        CostumeTypes costumeTypes = gameFiles.CostumeTypes;
        CostumeTypesGfx? costumeGfx = null;
        if (CostumeType is not null)
        {
            if (costumeTypes.TryGetGfx(CostumeType, out costumeGfx))
            {
                gfx = costumeGfx.ToGfxType(gfx, ColorScheme, colorExceptionTypes);
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
                gfx = weaponSkinGfx.ToGfxType(gfx, ColorScheme, colorExceptionTypes, costumeGfx);
            }
            else
            {
                throw new ArgumentException($"Invalid weapon skin type {WeaponSkinType}");
            }
        }

        SpawnBotTypes spawnBotTypes = gameFiles.SpawnBotTypes;
        if (SpawnBotType is not null)
        {
            if (spawnBotTypes.TryGetGfx(SpawnBotType, out SpawnBotTypesGfx? spawnBot))
            {
                gfx = spawnBot.ToGfxType(gfx);
            }
            else
            {
                throw new ArgumentException($"Invalid spawn bot type {WeaponSkinType}");
            }
        }

        return (gfx, Animation, Flip);
    }
}