using System;
using System.Numerics;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class PickerWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    private string _costumeTypeFilter = "";
    private string _weaponSkinTypeFilter = "";
    private string _colorSchemeFilter = "";

    private readonly CustomColorList _customColors = new();

    public void Show(Loader? loader, GfxInfo info, ref RlColor bgColor)
    {
        ImGui.Begin("Options", ref _open);
        if (loader is null)
        {
            ImGui.End();
            return;
        }

        ImGui.SeparatorText("Config");

        bool flip = info.Flip;
        if (ImGui.Checkbox("Flip", ref flip))
            info.Flip = flip;

        Vector3 bgColor2 = RaylibUtils.RlColorToVector3(bgColor);
        if (ImGui.ColorEdit3("Background color", ref bgColor2, ImGuiColorEditFlags.NoInputs))
            bgColor = RaylibUtils.Vector3ToRlColor(bgColor2);

        ImGui.Spacing();
        ImGui.PushTextWrapPos();
        ImGui.TextColored(new(1, 1, 0, 1), "WARNING! Increasing this can make your CPU (and GPU) cry. The game seems to never use a value above 2.");
        ImGui.PopTextWrapPos();

        double animScale = info.AnimScale;
        if (ImGuiEx.InputDouble("Render quality", ref animScale))
        {
            info.AnimScale = animScale;
            loader.AssetLoader.ClearSwfShapeCache();
        }

        ImGui.SeparatorText("Costume Types");
        CostumeTypeSection(loader, info);

        ImGui.SeparatorText("Weapon skin Types");
        WeaponSkinTypeSection(loader, info);

        ImGui.SeparatorText("Color scheme");
        ColorSchemeSection(loader, info);

        ImGui.SeparatorText("Custom colors");
        _customColors.Show();

        ImGui.End();
    }

    private void CostumeTypeSection(Loader loader, GfxInfo gfxInfo)
    {
        CostumeTypes costumeTypes = loader.SwzFiles.Game.CostumeTypes;
        HeroTypes heroTypes = loader.SwzFiles.Game.HeroTypes;
        ImGui.InputText("Filter costumes", ref _costumeTypeFilter, 256);
        if (ImGui.BeginListBox("###costumeselect"))
        {
            if (ImGui.Selectable("None##none", gfxInfo.CostumeType is null))
            {
                gfxInfo.CostumeType = null;
                OnSelect(loader);
            }

            foreach (string costumeType in costumeTypes.Costumes)
            {
                string costumeName = costumeType;
                if (costumeTypes.TryGetInfo(costumeType, out CostumeTypeInfo info))
                {
                    // First check if we have a hero and it's the default skin (highest priority case)
                    if (heroTypes.TryGetHero(info.OwnerHero, out HeroType? hero) && info.CostumeIndex == 0)
                    {
                        // Default skin always uses hero name
                        // e.g. `Bödvar (Viking)`
                        costumeName = $"{hero.BioName} ({costumeType})";
                    }
                    // Otherwise try to get the display name
                    else if (loader.TryGetStringName(info.DisplayNameKey, out string? realCostumeName))
                    {
                        // We have the display name, now see if we also have hero info
                        if (hero is not null && !string.IsNullOrEmpty(hero.BioName))
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

                if (!costumeName.Contains(_costumeTypeFilter, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if (ImGui.Selectable(costumeName, costumeType == gfxInfo.CostumeType))
                {
                    gfxInfo.CostumeType = costumeType;
                    OnSelect(loader);
                }
            }

            ImGui.EndListBox();
        }
    }

    private void WeaponSkinTypeSection(Loader loader, GfxInfo gfxInfo)
    {
        WeaponSkinTypes weaponSkinTypes = loader.SwzFiles.Game.WeaponSkinTypes;
        ImGui.InputText("Filter weapon skins", ref _weaponSkinTypeFilter, 256);
        if (ImGui.BeginListBox("###weaponselect"))
        {
            if (ImGui.Selectable("None##none", gfxInfo.WeaponSkinType is null))
            {
                gfxInfo.WeaponSkinType = null;
            }

            foreach (string weaponSkinType in weaponSkinTypes.WeaponSkins)
            {
                string weaponSkinName = weaponSkinType;
                if (weaponSkinTypes.TryGetInfo(weaponSkinType, out WeaponSkinTypeInfo info))
                {
                    string displayNameKey = info.DisplayNameKey;
                    if (loader.TryGetStringName(displayNameKey, out string? realWeaponSkinName))
                        weaponSkinName = $"{realWeaponSkinName} ({weaponSkinType})";
                }

                if (!weaponSkinName.Contains(_weaponSkinTypeFilter, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if (ImGui.Selectable(weaponSkinName, weaponSkinType == gfxInfo.WeaponSkinType))
                    gfxInfo.WeaponSkinType = weaponSkinType;
            }

            ImGui.EndListBox();
        }
    }

    private void ColorSchemeSection(Loader loader, GfxInfo info)
    {
        ColorSchemeTypes colorSchemeTypes = loader.SwzFiles.Game.ColorSchemeTypes;
        ImGui.InputText("Filter color schemes", ref _colorSchemeFilter, 256);
        if (ImGui.BeginListBox("###colorselect"))
        {
            foreach (string colorScheme in colorSchemeTypes.ColorSchemes)
            {
                string colorSchemeName = colorScheme;
                if (colorSchemeTypes.TryGetColorScheme(colorScheme, out ColorScheme? scheme))
                {
                    string? displayNameKey = scheme.DisplayNameKey;
                    if (displayNameKey is not null && loader.TryGetStringName(displayNameKey, out string? realSchemeName))
                        colorSchemeName = $"{realSchemeName} ({colorScheme})";
                }

                if (!colorSchemeName.Contains(_colorSchemeFilter, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if (ImGui.Selectable(colorSchemeName, colorScheme == info.ColorScheme))
                {
                    info.ColorScheme = colorScheme;
                    OnSelect(loader);
                }
            }

            if (ImGui.Selectable("DEBUG (not a real color scheme)", info.ColorScheme == "DEBUG"))
            {
                info.ColorScheme = "DEBUG";
                OnSelect(loader);
            }

            ImGui.EndListBox();
        }
    }

    private static void OnSelect(Loader loader)
    {
        loader.AssetLoader.ClearSwfShapeCache();
    }
}