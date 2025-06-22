using System;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading.CompanionTypes;
using BrawlhallaAnimLib.Reading.CostumeTypes;
using BrawlhallaAnimLib.Reading.ItemTypes;
using BrawlhallaAnimLib.Reading.SpawnBotTypes;
using BrawlhallaAnimLib.Reading.WeaponSkinTypes;

namespace WallyAnmRenderer;

public sealed class GfxInfo : IGfxInfo
{
    public string? SourceFilePath { get; set; }

    public int Team { get; set; } = 0;
    public string? AnimClass { get; set; }
    public string? AnimFile { get; set; }
    public string? Animation { get; set; }
    public double AnimScale { get; set; } = 2;
    public bool Flip { get; set; } = false;

    public string? CostumeType { get; set; }
    public string? WeaponSkinType { get; set; }
    public string? ItemType { get; set; }
    public string? SpawnBotType { get; set; }
    public string? CompanionType { get; set; }
    public ColorScheme? ColorScheme { get; set; }

    public GfxMouthOverride? MouthOverride { get; set; }
    public GfxEyesOverride? EyesOverride { get; set; }

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

        CostumeTypesGfx? costumeGfx = null;
        if (CostumeType is not null)
        {
            CostumeTypes costumeTypes = gameFiles.CostumeTypes;
            if (costumeTypes.TryGetGfx(CostumeType, out costumeGfx))
            {
                gfx = costumeGfx.ToGfxType(gfx, ColorScheme, colorExceptionTypes);
            }
            else
            {
                throw new ArgumentException($"Invalid costume type {CostumeType}");
            }
        }

        if (WeaponSkinType is not null)
        {
            WeaponSkinTypes weaponSkinTypes = gameFiles.WeaponSkinTypes;
            if (weaponSkinTypes.TryGetGfx(WeaponSkinType, out WeaponSkinTypesGfx? weaponSkinGfx))
            {
                gfx = weaponSkinGfx.ToGfxType(gfx, ColorScheme, colorExceptionTypes, costumeGfx);
            }
            else
            {
                throw new ArgumentException($"Invalid weapon skin type {WeaponSkinType}");
            }
        }

        if (ItemType is not null)
        {
            ItemTypes itemTypes = gameFiles.ItemTypes;
            if (itemTypes.TryGetGfx(ItemType, out ItemTypesGfx? itemGfx))
            {
                gfx = itemGfx.ToHeldGfx(gfx, Team);
            }
            else
            {
                throw new ArgumentException($"Invalid item type {ItemType}");
            }
        }

        if (SpawnBotType is not null)
        {
            SpawnBotTypes spawnBotTypes = gameFiles.SpawnBotTypes;
            if (spawnBotTypes.TryGetGfx(SpawnBotType, out SpawnBotTypesGfx? spawnBot))
            {
                gfx = spawnBot.ToGfxType(gfx);
            }
            else
            {
                throw new ArgumentException($"Invalid spawn bot type {SpawnBotType}");
            }
        }

        if (CompanionType is not null)
        {
            CompanionTypes spawnBotTypes = gameFiles.CompanionTypes;
            if (spawnBotTypes.TryGetGfx(CompanionType, out CompanionTypesGfx? companion))
            {
                IGfxType companionGfx = companion.ToGfxType();
                // we do a bit of cheating. companion is meant to be a standalone gfx, so we merge it manually
                // this should be safe because the art type is unique.
                GfxType newGfx = new(gfx);
                newGfx.CustomArts.AddRange(companionGfx.CustomArts);
                newGfx.ColorSwaps.AddRange(companionGfx.ColorSwaps);
                gfx = newGfx;
            }
            else
            {
                throw new ArgumentException($"Invalid companion type {CompanionType}");
            }
        }

        if (MouthOverride is not null)
        {
            gfx = gfx.WithMouthOverride(MouthOverride.Value);
        }

        if (EyesOverride is not null)
        {
            gfx = gfx.WithEyesOverride(EyesOverride.Value);
        }

        return (gfx, Animation, Flip);
    }
}