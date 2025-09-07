using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BrawlhallaAnimLib;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading;

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
    public int ItemTypeTeam { get; set; } = 0;
    public string? ItemType { get; set; }
    public string? SpawnBotType { get; set; }
    public string? CompanionType { get; set; }
    public PodiumTeamEnum PodiumTypeTeam = PodiumTeamEnum.None;
    public string? PodiumType { get; set; }
    public string? SeasonBorderType { get; set; }
    public string? PlayerThemeType { get; set; }
    public string? AvatarType { get; set; }
    public string? EmojiType { get; set; }
    public string? EndMatchVoicelineType { get; set; }
    public string? ClientThemeType { get; set; }
    public ColorScheme? ColorScheme { get; set; }

    public uint CrateColorA { get; set; } = 0;
    public uint CrateColorB { get; set; } = 0;

    public GfxMouthOverride? MouthOverride { get; set; }
    public GfxEyesOverride? EyesOverride { get; set; }

    [MemberNotNullWhen(true, nameof(AnimClass))]
    [MemberNotNullWhen(true, nameof(AnimFile))]
    [MemberNotNullWhen(true, nameof(Animation))]
    public bool AnimationPicked => AnimClass is not null && AnimFile is not null && Animation is not null;

    public (IGfxType gfx, bool flip)? ToGfxType(SwzGameFile gameFiles)
    {
        ColorExceptionTypes colorExceptionTypes = gameFiles.ColorExceptionTypes;

        IGfxType gfx = new GfxType()
        {
            AnimFile = AnimFile ?? "",
            AnimClass = AnimClass ?? "a__Animation",
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
                gfx = itemGfx.ToHeldGfx(gfx, ItemTypeTeam);
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
            CompanionTypes companionTypes = gameFiles.CompanionTypes;
            if (companionTypes.TryGetGfx(CompanionType, out CompanionTypesGfx? companion))
            {
                IGfxType companionGfx = companion.ToGfxType(ColorScheme);
                // we do a bit of cheating. companion is meant to be a standalone gfx, so we merge it manually.
                // this should be safe because the art type is unique.
                gfx = AddCustomArts(gfx, companionGfx.CustomArts);
                gfx = AddColorSwaps(gfx, companionGfx.ColorSwaps);
            }
            else
            {
                throw new ArgumentException($"Invalid companion type {CompanionType}");
            }
        }

        if (PodiumType is not null)
        {
            PodiumTypes podiumTypes = gameFiles.PodiumTypes;
            if (podiumTypes.TryGetGfx(PodiumType, out PodiumTypesGfx? podium))
            {
                IGfxType podiumGfx = podium.ToGfxType(PodiumTypeTeam);
                // we do a bit of cheating. podium is meant to be a standalone gfx, so we merge it manually.
                // this may cause conflicts with other custom arts, but hopefully not.
                gfx = AddCustomArts(gfx, podiumGfx.CustomArts);
            }
            else
            {
                throw new ArgumentException($"Invalid podium type {PodiumType}");
            }
        }

        if (SeasonBorderType is not null)
        {
            SeasonBorderTypes seasonBorderTypes = gameFiles.SeasonBorderTypes;
            if (seasonBorderTypes.TryGetGfx(SeasonBorderType, out SeasonBorderTypesGfx? loadingFrame))
            {
                IGfxType loadingFrameGfx = loadingFrame.ToGfxType();
                // we do a bit of cheating. season border is meant to be a standalone gfx, so we merge it manually.
                // this may cause conflicts with other custom arts, but hopefully not.
                gfx = AddCustomArts(gfx, loadingFrameGfx.CustomArts);
            }
            else
            {
                throw new ArgumentException($"Invalid loading frame type {SeasonBorderType}");
            }
        }

        if (PlayerThemeType is not null)
        {
            PlayerThemeTypes seasonBorderTypes = gameFiles.PlayerThemeTypes;
            if (seasonBorderTypes.TryGetGfx(PlayerThemeType, out PlayerThemeTypesGfx? uiTheme))
            {
                IGfxType uiThemeGfx = uiTheme.ToGfxType();
                // we do a bit of cheating. player theme is meant to be a standalone gfx, so we merge it manually.
                // this may cause conflicts with other custom arts, but hopefully not.
                gfx = AddCustomArts(gfx, uiThemeGfx.CustomArts);
            }
            else
            {
                throw new ArgumentException($"Invalid ui theme type {PlayerThemeType}");
            }
        }

        if (AvatarType is not null)
        {
            AvatarTypes avatarTypes = gameFiles.AvatarTypes;
            if (avatarTypes.TryGetGfx(AvatarType, out AvatarTypesGfx? avatar))
            {
                ICustomArt flagCustomArt = avatar.ToFlagCustomArt();
                gfx = AddCustomArts(gfx, [flagCustomArt]);
            }
            else
            {
                throw new ArgumentException($"Invalid avatar type {AvatarType}");
            }
        }

        if (EmojiType is not null)
        {
            EmojiTypes emojiTypes = gameFiles.EmojiTypes;
            if (emojiTypes.TryGetGfx(EmojiType, out EmojiTypesGfx? emoji))
            {
                IGfxType emojiGfx = emoji.ToGfxType();
                // we do a bit of cheating. emoji is meant to be a standalone gfx, so we merge it manually.
                // this may cause conflicts with other custom arts, but hopefully not.
                gfx = AddCustomArts(gfx, emojiGfx.CustomArts);
            }
        }

        if (EndMatchVoicelineType is not null)
        {
            EndMatchVoicelineTypes endMatchVoicelineTypes = gameFiles.EndMatchVoicelineTypes;
            if (endMatchVoicelineTypes.TryGetGfx(EndMatchVoicelineType, out EndMatchVoicelineTypesGfx? voiceline))
            {
                IGfxType voicelineGfx = voiceline.ToGfxType();
                // we do a bit of cheating. voiceline is meant to be a standalone gfx, so we merge it manually.
                // this may cause conflicts with other custom arts, but hopefully not.
                gfx = AddCustomArts(gfx, voicelineGfx.CustomArts);
            }
        }

        if (ClientThemeType is not null)
        {
            ClientThemeTypes endMatchVoicelineTypes = gameFiles.ClientThemeTypes;
            if (endMatchVoicelineTypes.TryGetGfx(ClientThemeType, out ClientThemeTypesGfx? theme))
            {
                IGfxType themeGfx = theme.ToGfxType();
                // we do a bit of cheating. client theme is meant to be a standalone gfx, so we merge it manually.
                // this may cause conflicts with other custom arts, but hopefully not.
                gfx = AddCustomArts(gfx, themeGfx.CustomArts);
            }
        }

        if (CrateColorA != 0)
        {
            gfx = AddColorSwaps(gfx, [CrateColorUtils.GetCrateAColorSwap(CrateColorA)]);
        }

        if (CrateColorB != 0)
        {
            gfx = AddColorSwaps(gfx, [CrateColorUtils.GetCrateBColorSwap(CrateColorB)]);
        }

        if (MouthOverride is not null)
        {
            gfx = gfx.WithMouthOverride(MouthOverride.Value);
        }

        if (EyesOverride is not null)
        {
            gfx = gfx.WithEyesOverride(EyesOverride.Value);
        }

        return (gfx, Flip);
    }

    private static GfxType AddCustomArts(IGfxType gfx, IEnumerable<ICustomArt> customArts)
    {
        GfxType gfxType = gfx is GfxType a ? a : new(gfx);
        gfxType.CustomArts.AddRange(customArts);
        return gfxType;
    }

    private static GfxType AddColorSwaps(IGfxType gfx, IEnumerable<IColorSwap> colorSwaps)
    {
        GfxType gfxType = gfx is GfxType a ? a : new(gfx);
        gfxType.ColorSwaps.AddRange(colorSwaps);
        return gfxType;
    }
}