using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BrawlhallaAnimLib.Gfx;
using BrawlhallaAnimLib.Reading;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class PickerWindow
{
    private static readonly Vector4 NOTE_COLOR = ImGuiEx.RGBHexToVec4(0x00AAFF);
    private static readonly Vector4 SELECTED_COLOR = ImGuiEx.RGBHexToVec4(0xFF7F00);
    private static readonly string[] ITEM_TEAM_OPTIONS = ["None", "Red", "Blue"];
    private static readonly string[] PODIUM_TEAM_OPTIONS = ["None", "Red", "Blue"];
    private static readonly string[] BUBBLE_TEAM_OPTIONS = ["None", "Red", "Blue"];
    private static readonly string[] HORDE_TYPE_OPTIONS = ["Standard", "Nightmare"];
    private static readonly string[] VOLLEY_BALL_COLOR_OPTIONS = ["None", "White", "Red", "Blue"];
    private const string DEBUG_COLOR_TEXT = "DEBUG (not a real color scheme)";

    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    private string _costumeTypeFilter = "";
    private string _weaponSkinTypeFilter = "";
    private string _heldItemTypeFilter = "";
    private string _equipItemTypeFilter = "";
    private string _worldItemTypeFilter = "";
    private string _spawnBotTypeFilter = "";
    private string _companionTypeFilter = "";
    private string _podiumTypeFilter = "";
    private string _seasonBorderTypeFilter = "";
    private string _playerThemeTypeFilter = "";
    private string _avatarTypesFilter = "";
    private string _emojiTypesFilter = "";
    private string _endMatchVoicelineTypeFilter = "";
    private string _clientThemeTypeFilter = "";
    private string _colorSchemeFilter = "";

    private readonly CustomColorList _customColors = new();

    public event EventHandler<ColorScheme>? ColorSchemeSelected;

    public PickerWindow()
    {
        _customColors.ColorSchemeSelected += (@this, color) =>
        {
            ColorSchemeSelected?.Invoke(@this, color);
        };
    }

    private static Func<T, string, bool> CreateShouldShowFromFilter<T>(string filter)
    {
        return (thing, name) => thing is null || name.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
    }

    public void Show(Loader? loader, GfxInfo gfxInfo, ref RlColor bgColor)
    {
        ImGui.Begin("Options", ref _open);
        if (loader is null)
        {
            ImGui.End();
            return;
        }

        ImGui.SeparatorText("Config");

        bool flip = gfxInfo.Flip;
        if (ImGui.Checkbox("Flip", ref flip))
            gfxInfo.Flip = flip;

        Vector3 bgColor2 = RaylibUtils.RlColorToVector3(bgColor);
        if (ImGui.ColorEdit3("Background color", ref bgColor2, ImGuiColorEditFlags.NoInputs))
            bgColor = RaylibUtils.Vector3ToRlColor(bgColor2);

        ImGui.Spacing();
        ImGui.PushTextWrapPos();
        ImGui.TextColored(new(1, 1, 0, 1), "WARNING! Increasing this too much will make your computer cry.");
        ImGui.PopTextWrapPos();

        double animScale = gfxInfo.AnimScale;
        if (ImGui.InputDouble("Render quality", ref animScale))
        {
            gfxInfo.AnimScale = animScale;
            loader.AssetLoader.ClearSwfShapeCache();
        }

        ImGui.SeparatorText("Gameplay");
        if (ImGui.TreeNode("Legend skins"))
        {
            CostumeTypeSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Weapon skins"))
        {
            WeaponSkinTypeSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Items"))
        {
            ItemsSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Sidekicks"))
        {
            SpawnBotTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Companions"))
        {
            CompanionTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Mouth/Eye Overrides"))
        {
            OverridesSection(gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Gamemodes"))
        {
            GamemodesSection(gfxInfo);
            ImGui.TreePop();
        }

        ImGui.SeparatorText("Colors");

        if (ImGui.TreeNode("Color schemes"))
        {
            ColorSchemeSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Custom colors"))
        {
            _customColors.Show(gfxInfo.ColorScheme);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Weapon spawn colors"))
        {
            CrateColorsSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        ImGui.SeparatorText("UI");

        if (ImGui.TreeNode("Podiums"))
        {
            PodiumTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Loading frames"))
        {
            SeasonBorderTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("UI themes"))
        {
            PlayerThemeTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Avatars"))
        {
            AvatarTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Emojis"))
        {
            EmojiTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Event Logos"))
        {
            ClientThemeTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("End of Match Voicelines"))
        {
            EndMatchVoicelineTypesSection(loader, gfxInfo);
            ImGui.TreePop();
        }

        ImGui.End();
    }

    private void CostumeTypeSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        CostumeTypes costumeTypes = loader.SwzFiles.Game.CostumeTypes;
        HeroTypes heroTypes = loader.SwzFiles.Game.HeroTypes;

        string costumeToName(string? costumeType)
        {
            if (costumeType is null) return "None##none";

            string costumeName = costumeType;
            if (costumeTypes.TryGetInfo(costumeType, out CostumeTypeInfo info))
            {
                bool hasHero = heroTypes.TryGetHero(info.OwnerHero, out HeroTypeInfo hero);
                // First check if we have a hero and it's the default skin (highest priority case)
                if (hasHero && info.CostumeIndex == 0)
                {
                    // Default skin always uses hero name
                    // e.g. `Bödvar (Viking)`
                    costumeName = $"{hero.BioName} ({costumeType})";
                }
                // Otherwise try to get the display name
                else if (loader.TryGetStringName(info.DisplayNameKey, out string? realCostumeName))
                {
                    // We have the display name, now see if we also have hero info
                    if (hasHero && !string.IsNullOrEmpty(hero.BioName))
                    {
                        // Normal skin with both display name and hero info
                        // e.g. `Bear'dvar (Bödvar: Bear)`
                        costumeName = $"{realCostumeName} ({hero.BioName}: {costumeType})";
                    }
                    else
                    {
                        // Just display name
                        // e.g. `DEFAULT_CHARACTER (Default)`
                        costumeName = $"{realCostumeName} ({costumeType})";
                    }
                }
                // If we reach here, we keep the default costumeName = costumeType
                // e.g. `Mech`
            }
            return costumeName;
        }

        void selectCostume(string? costumeType)
        {
            gfxInfo.CostumeType = costumeType;
            OnSelect(loader);
        }

        PickerListBox<string?> picker = new()
        {
            Options = costumeTypes.Costumes.Prepend(null),
            OptionToString = costumeToName,
            OnSelect = selectCostume,
            ShouldShow = CreateShouldShowFromFilter<string?>(_costumeTypeFilter)
        };
        ImGui.InputText("Filter costumes", ref _costumeTypeFilter, 256);
        picker.Show(gfxInfo.CostumeType);
    }

    private void WeaponSkinTypeSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        WeaponSkinTypes weaponSkinTypes = loader.SwzFiles.Game.WeaponSkinTypes;

        string weaponSkinToName(string? weaponSkinType)
        {
            if (weaponSkinType is null) return "None##none";

            string weaponSkinName = weaponSkinType;
            if (weaponSkinTypes.TryGetInfo(weaponSkinType, out WeaponSkinTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realWeaponSkinName))
                    weaponSkinName = $"{realWeaponSkinName} ({weaponSkinType})";
            }
            return weaponSkinName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = weaponSkinTypes.WeaponSkins.Prepend(null),
            OptionToString = weaponSkinToName,
            OnSelect = (weaponSkinType) => gfxInfo.WeaponSkinType = weaponSkinType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_weaponSkinTypeFilter),
        };
        ImGui.InputText("Filter weapon skins", ref _weaponSkinTypeFilter, 256);
        picker.Show(gfxInfo.WeaponSkinType);
    }

    private readonly record struct ShouldShowItem(string ItemType, bool Held = false, bool Equip = false, bool World = false);

    private void ItemsSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ItemTypes itemTypes = loader.SwzFiles.Game.ItemTypes;

        string itemToName(string? itemType)
        {
            if (itemType is null) return "None##none";

            string itemName = itemType;
            if (itemTypes.TryGetInfo(itemType, out ItemTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realItemName))
                    itemName = $"{realItemName} ({itemType})";
            }
            return itemName;
        }

        IEnumerable<ShouldShowItem> shouldShowItemTypes = itemTypes.Items.Select((itemType) =>
        {
            if (!itemTypes.TryGetGfx(itemType, out ItemTypesGfx? itemGfx))
                return new ShouldShowItem(itemType);

            return new(itemType)
            {
                Held = itemGfx.HasHeldCustomArt,
                Equip = itemGfx.HasEquipCustomArt,
                World = itemGfx.HasWorldCustomArt,
            };
        });

        ImGui.PushID("held");
        ImGui.SeparatorText("Held item");
        ImGui.TextColored(NOTE_COLOR, "Use Animation_Player.swf/a__HeldItemAnimation");

        // team
        int team = gfxInfo.ItemTypeTeam;
        ImGui.Combo("Team", ref team, ITEM_TEAM_OPTIONS, ITEM_TEAM_OPTIONS.Length);
        gfxInfo.ItemTypeTeam = team;
        // item
        IEnumerable<string?> heldItemTypes = shouldShowItemTypes
            .Where((shouldShow) => shouldShow.Held)
            .Select((shouldShow) => shouldShow.ItemType)
            .Prepend(null);
        PickerListBox<string?> heldPicker = new()
        {
            Options = heldItemTypes,
            OptionToString = itemToName,
            OnSelect = (itemType) => gfxInfo.HeldItemType = itemType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_heldItemTypeFilter),
        };
        ImGui.InputText("Filter items", ref _heldItemTypeFilter, 256);
        heldPicker.Show(gfxInfo.HeldItemType);

        ImGui.PopID(); ImGui.PushID("equip");
        ImGui.SeparatorText("Equipped item");

        if (gfxInfo.EquipItemType is not null && itemTypes.TryGetGfx(gfxInfo.EquipItemType, out ItemTypesGfx? equipItem))
        {
            ImGui.TextColored(NOTE_COLOR, $"Use {equipItem.EquipAnimFile}/{equipItem.EquipAnimClass}");

            ImGui.PushTextWrapPos();
            ImGui.TextColored(NOTE_COLOR, "You may not notice changes if you selected a weapon");
            ImGui.PopTextWrapPos();
        }
        else
        {
            ImGui.TextColored(NOTE_COLOR, "Intended animation depends on item");
        }

        IEnumerable<string?> equipItemTypes = shouldShowItemTypes
            .Where((shouldShow) => shouldShow.Equip)
            .Select((shouldShow) => shouldShow.ItemType)
            .Prepend(null);
        PickerListBox<string?> equipPicker = new()
        {
            Options = equipItemTypes,
            OptionToString = itemToName,
            OnSelect = (itemType) => gfxInfo.EquipItemType = itemType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_equipItemTypeFilter),
        };
        ImGui.InputText("Filter items", ref _equipItemTypeFilter, 256);
        equipPicker.Show(gfxInfo.EquipItemType);

        ImGui.PopID(); ImGui.PushID("world");
        ImGui.SeparatorText("World item");

        if (gfxInfo.WorldItemType is not null && itemTypes.TryGetGfx(gfxInfo.WorldItemType, out ItemTypesGfx? worldItem))
        {
            ImGui.TextColored(NOTE_COLOR, $"Use {worldItem.WorldAnimFile}/{worldItem.WorldAnimClass}");

            ImGui.PushTextWrapPos();
            ImGui.TextColored(NOTE_COLOR, "You may not notice changes if you selected a weapon");
            ImGui.PopTextWrapPos();
        }
        else
        {
            ImGui.TextColored(NOTE_COLOR, "Intended animation depends on item");
        }

        IEnumerable<string?> worldItemTypes = shouldShowItemTypes
            .Where((shouldShow) => shouldShow.World)
            .Select((shouldShow) => shouldShow.ItemType)
            .Prepend(null);
        PickerListBox<string?> worldPicker = new()
        {
            Options = worldItemTypes,
            OptionToString = itemToName,
            OnSelect = (itemType) => gfxInfo.WorldItemType = itemType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_worldItemTypeFilter),
        };
        ImGui.InputText("Filter items", ref _worldItemTypeFilter, 256);
        worldPicker.Show(gfxInfo.WorldItemType);

        ImGui.PopID();
    }

    private void SpawnBotTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        SpawnBotTypes spawnBotTypes = loader.SwzFiles.Game.SpawnBotTypes;

        string spawnBotToName(string? spawnBotType)
        {
            if (spawnBotType is null) return "None##none";

            string spawnBotName = spawnBotType;
            if (spawnBotTypes.TryGetInfo(spawnBotType, out SpawnBotTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realSpawnBotName))
                    spawnBotName = $"{realSpawnBotName} ({spawnBotType})";
            }
            return spawnBotName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = spawnBotTypes.SpawnBots.Prepend(null),
            OptionToString = spawnBotToName,
            OnSelect = (spawnBotType) => gfxInfo.SpawnBotType = spawnBotType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_spawnBotTypeFilter),
        };
        ImGui.InputText("Filter sidekicks", ref _spawnBotTypeFilter, 256);
        picker.Show(gfxInfo.SpawnBotType);
    }

    private void CompanionTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ImGui.PushTextWrapPos();
        ImGui.TextColored(NOTE_COLOR, "NOTE: Each companion is intended to be used with its unique anm file");
        ImGui.PopTextWrapPos();

        CompanionTypes? companionTypes = loader.SwzFiles.Game.CompanionTypes;

        if (companionTypes is null)
        {
            ImGui.TextWrapped("No CompanionTypes.xml found in swz file. You may be using an older version of the game.");
            return;
        }

        string companionToName(string? companionType)
        {
            if (companionType is null) return "None##none";

            string companionName = companionType;
            if (companionTypes.TryGetInfo(companionType, out CompnaionTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realCompanionName))
                    companionName = $"{realCompanionName} ({companionType})";
            }
            return companionName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = companionTypes.Companions.Prepend(null),
            OptionToString = companionToName,
            OnSelect = (companionType) => gfxInfo.CompanionType = companionType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_companionTypeFilter),
        };
        ImGui.InputText("Filter companions", ref _companionTypeFilter, 256);
        picker.Show(gfxInfo.CompanionType);
    }

    private void PodiumTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        gfxInfo.PodiumTypeTeam = ImGuiEx.EnumCombo("Team", gfxInfo.PodiumTypeTeam, PODIUM_TEAM_OPTIONS);

        PodiumTypes podiumTypes = loader.SwzFiles.Game.PodiumTypes;

        string podiumToName(string? podiumType)
        {
            if (podiumType is null) return "None##none";

            string podiumName = podiumType;
            if (podiumTypes.TryGetInfo(podiumType, out PodiumTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realPodiumName))
                    podiumName = $"{realPodiumName} ({podiumType})";
            }
            return podiumName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = podiumTypes.Podiums.Prepend(null),
            OptionToString = podiumToName,
            OnSelect = (podiumType) => gfxInfo.PodiumType = podiumType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_podiumTypeFilter),
        };
        ImGui.InputText("Filter podiums", ref _podiumTypeFilter, 256);
        picker.Show(gfxInfo.PodiumType);
    }

    private void SeasonBorderTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ImGui.PushTextWrapPos();
        ImGui.TextColored(NOTE_COLOR, "NOTE: These are intended to be used with Animation_LoadingFrames");
        ImGui.PopTextWrapPos();

        SeasonBorderTypes seasonBorderTypes = loader.SwzFiles.Game.SeasonBorderTypes;

        string seasonBorderToName(string? seasonBorderType)
        {
            if (seasonBorderType is null) return "None##none";

            string seasonBorderName = seasonBorderType;
            if (seasonBorderTypes.TryGetInfo(seasonBorderType, out SeasonBorderTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realSeasonBorderName))
                    seasonBorderName = $"{realSeasonBorderName} ({seasonBorderType})";
            }
            return seasonBorderName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = seasonBorderTypes.LoadingFrames.Prepend(null),
            OptionToString = seasonBorderToName,
            OnSelect = (seasonBorderType) => gfxInfo.SeasonBorderType = seasonBorderType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_seasonBorderTypeFilter),
        };
        ImGui.InputText("Filter loading frames", ref _seasonBorderTypeFilter, 256);
        picker.Show(gfxInfo.SeasonBorderType);
    }

    private void PlayerThemeTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ImGui.PushTextWrapPos();
        ImGui.TextColored(NOTE_COLOR, "NOTE: These are intended to be used with Animation_PlayerThemes");
        ImGui.PopTextWrapPos();

        ImGui.PushTextWrapPos();
        ImGui.TextColored(new(1, 1, 0, 1), "Some older UI themes may not work due to BMG-ness.");
        ImGui.PopTextWrapPos();

        PlayerThemeTypes playerThemeTypes = loader.SwzFiles.Game.PlayerThemeTypes;

        string playerThemeToName(string? playerThemeType)
        {
            if (playerThemeType is null) return "None##none";

            string playerThemeName = playerThemeType;
            if (playerThemeTypes.TryGetInfo(playerThemeType, out PlayerThemeTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realPlayerThemeName))
                    playerThemeName = $"{realPlayerThemeName} ({playerThemeType})";
            }
            return playerThemeName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = playerThemeTypes.UIThemes.Prepend(null),
            OptionToString = playerThemeToName,
            OnSelect = (playerThemeType) => gfxInfo.PlayerThemeType = playerThemeType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_playerThemeTypeFilter),
        };
        ImGui.InputText("Filter UI themes", ref _playerThemeTypeFilter, 256);
        picker.Show(gfxInfo.PlayerThemeType);
    }

    private void AvatarTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        AvatarTypes avatarTypes = loader.SwzFiles.Game.AvatarTypes;

        string avatarToName(string? avatarType)
        {
            if (avatarType is null) return "None##none";

            string avatarName = avatarType;
            if (avatarTypes.TryGetInfo(avatarType, out AvatarTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realAvatarName))
                    avatarName = $"{realAvatarName} ({avatarType})";
            }
            return avatarName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = avatarTypes.Avatars.Prepend(null),
            OptionToString = avatarToName,
            OnSelect = (avatarType) => gfxInfo.AvatarType = avatarType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_avatarTypesFilter),
        };
        ImGui.InputText("Filter avatars", ref _avatarTypesFilter, 256);
        picker.Show(gfxInfo.AvatarType);
    }

    private void EmojiTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ImGui.PushTextWrapPos();
        ImGui.TextColored(NOTE_COLOR, "NOTE: These are intended to be used with Animation_Emojis");
        ImGui.PopTextWrapPos();

        EmojiTypes emojiTypes = loader.SwzFiles.Game.EmojiTypes;

        string emojiToName(string? emojiType)
        {
            if (emojiType is null) return "None##none";

            string emojiName = emojiType;
            if (emojiTypes.TryGetInfo(emojiType, out EmojiTypeInfo info))
            {
                string displayNameKey = info.DisplayNameKey;
                if (loader.TryGetStringName(displayNameKey, out string? realEmojiName))
                    emojiName = $"{realEmojiName} ({emojiType})";
            }
            return emojiName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = emojiTypes.Emojis.Prepend(null),
            OptionToString = emojiToName,
            OnSelect = (emojiType) => gfxInfo.EmojiType = emojiType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_emojiTypesFilter),
        };
        ImGui.InputText("Filter emojis", ref _emojiTypesFilter, 256);
        picker.Show(gfxInfo.EmojiType);
    }

    private void EndMatchVoicelineTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ImGui.PushTextWrapPos();
        ImGui.TextColored(NOTE_COLOR, "NOTE: These are intended to be used with Animation_GameUI");
        ImGui.PopTextWrapPos();

        EndMatchVoicelineTypes endMatchVoicelineTypes = loader.SwzFiles.Game.EndMatchVoicelineTypes;

        string voicelineToName(string? endMatchVoicelineType)
        {
            if (endMatchVoicelineType is null) return "None##none";

            string endMatchVoicelineName = endMatchVoicelineType;
            if (endMatchVoicelineTypes.TryGetInfo(endMatchVoicelineType, out EndMatchVoicelineTypesInfo info))
            {
                // the wwise sound event contains the spoken word
                endMatchVoicelineName = $"{info.WWiseSoundName} ({endMatchVoicelineType})";
            }
            return endMatchVoicelineName;
        }

        PickerListBox<string?> picker = new()
        {
            Options = endMatchVoicelineTypes.Voicelines.Prepend(null),
            OptionToString = voicelineToName,
            OnSelect = (voicelineType) => gfxInfo.EndMatchVoicelineType = voicelineType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_endMatchVoicelineTypeFilter),
        };
        ImGui.InputText("Filter voicelines", ref _endMatchVoicelineTypeFilter, 256);
        picker.Show(gfxInfo.EndMatchVoicelineType);
    }

    private void ClientThemeTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ImGui.PushTextWrapPos();
        ImGui.TextColored(NOTE_COLOR, "NOTE: These are intended to be used with Animation_ClientThemeLogos");
        ImGui.PopTextWrapPos();

        ClientThemeTypes clientThemeTypes = loader.SwzFiles.Game.ClientThemeTypes;

        static string clientThemeToName(string? clientThemeType)
        {
            return clientThemeType ?? "None#none";
        }

        PickerListBox<string?> picker = new()
        {
            Options = clientThemeTypes.Themes.Prepend(null),
            OptionToString = clientThemeToName,
            OnSelect = (voicelineType) => gfxInfo.ClientThemeType = voicelineType,
            ShouldShow = CreateShouldShowFromFilter<string?>(_clientThemeTypeFilter),
        };
        ImGui.InputText("Filter event logos", ref _clientThemeTypeFilter, 256);
        picker.Show(gfxInfo.ClientThemeType);
    }

    private void ColorSchemeSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ColorSchemeTypes colorSchemeTypes = loader.SwzFiles.Game.ColorSchemeTypes;

        string colorSchemeToName(ColorScheme colorScheme)
        {
            if (colorScheme == ColorScheme.DEBUG)
                return DEBUG_COLOR_TEXT;

            string colorSchemeName = colorScheme.Name;
            string? displayNameKey = colorScheme.DisplayNameKey;
            if (displayNameKey is not null && loader.TryGetStringName(displayNameKey, out string? realSchemeName))
                colorSchemeName = $"{realSchemeName} ({colorSchemeName})";
            return colorSchemeName;
        }

        void selectColorScheme(ColorScheme colorScheme)
        {
            ColorSchemeSelected?.Invoke(this, colorScheme);
        }

        PickerListBox<ColorScheme> picker = new()
        {
            Options = colorSchemeTypes.ColorSchemes.Append(ColorScheme.DEBUG),
            OptionToString = colorSchemeToName,
            OnSelect = selectColorScheme,
            ShouldShow = CreateShouldShowFromFilter<ColorScheme>(_colorSchemeFilter),
        };
        ImGui.InputText("Filter color schemes", ref _colorSchemeFilter, 256);
        picker.Show(gfxInfo.ColorScheme);
    }

    private static void CrateColorsSection(Loader loader, GfxInfo gfxInfo)
    {
        ImGui.PushTextWrapPos();
        ImGui.TextColored(NOTE_COLOR, "NOTE: Pure black is treated by the game and program as no swap");
        ImGui.PopTextWrapPos();

        uint ogCrateColorA = gfxInfo.CrateColorA;
        gfxInfo.CrateColorA = ImGuiEx.ColorPicker3Hex("##outer", gfxInfo.CrateColorA);
        ImGui.SameLine();
        ImGui.Text("Outer color");

        uint ogCrateColorB = gfxInfo.CrateColorB;
        gfxInfo.CrateColorB = ImGuiEx.ColorPicker3Hex("##inner", gfxInfo.CrateColorB);
        ImGui.SameLine();
        ImGui.Text("Inner color");

        // gotta reload the cache because it's not keyed by the color swap
        if (ogCrateColorA != gfxInfo.CrateColorA || ogCrateColorB != gfxInfo.CrateColorB)
        {
            loader.AssetLoader.ClearSwfShapeCache();
        }
    }

    private static void OverridesSection(GfxInfo gfxInfo)
    {
        ImGui.Text("Mouth override");
        ImGui.PushID("mouth");
        PickerListBox<GfxMouthOverride> mouthPicker = new()
        {
            Options = Enum.GetValues<GfxMouthOverride>(),
            OptionToString = EnumStringDicts.GetMouthOverridesString,
            OnSelect = (mouthOverride) => gfxInfo.MouthOverride = mouthOverride,
        };
        mouthPicker.Show(gfxInfo.MouthOverride);
        ImGui.PopID();

        ImGui.Text("Eyes override");
        ImGui.PushID("eyes");
        PickerListBox<GfxEyesOverride> eyesPicker = new()
        {
            Options = Enum.GetValues<GfxEyesOverride>(),
            OptionToString = EnumStringDicts.GetEyesOverridesString,
            OnSelect = (eyesOverride) => gfxInfo.EyesOverride = eyesOverride,
        };
        eyesPicker.Show(gfxInfo.EyesOverride);
        ImGui.PopID();
    }

    public static void GamemodesSection(GfxInfo gfxInfo)
    {
        ImGui.PushTextWrapPos();
        ImGui.TextColored(NOTE_COLOR, "NOTE: These are intended to be used with Animation_GameModes, each their own animation class");
        ImGui.PopTextWrapPos();

        ImGui.SeparatorText("Bubble tag (a__AnimationTagBubble)");
        ImGui.PushID("bubbletag");

        ImGui.Text("Bubble team"); ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        gfxInfo.BubbleTagTeam = ImGuiEx.EnumCombo(string.Empty, gfxInfo.BubbleTagTeam, BUBBLE_TEAM_OPTIONS);

        ImGui.PopID();
        ImGui.SeparatorText("Horde (a__AnimationHordeDemon)");
        ImGui.PushID("horde");

        ImGui.Text("Demon style"); ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        gfxInfo.HordeType = ImGuiEx.EnumCombo(string.Empty, gfxInfo.HordeType, HORDE_TYPE_OPTIONS);

        ImGui.PopID();
        ImGui.SeparatorText("Volleybrawl (a__AnimationSoccerBall)");
        ImGui.PushID("volleybrawl");

        ImGui.Text("Ball color"); ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        gfxInfo.VolleyBattleTeam = ImGuiEx.EnumCombo("##team", gfxInfo.VolleyBattleTeam, VOLLEY_BALL_COLOR_OPTIONS);

        ImGui.Text("Ball damage amount"); ImGui.SameLine();
        int ballNumber = gfxInfo.VolleyBattleBallNumber;
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderInt("##damage", ref ballNumber, 1, 4, "%d Hits", ImGuiSliderFlags.AlwaysClamp);
        gfxInfo.VolleyBattleBallNumber = ballNumber;

        ImGui.PopID();
    }

    private static void OnSelect(Loader loader)
    {
        loader.AssetLoader.ClearSwfShapeCache();
    }
}