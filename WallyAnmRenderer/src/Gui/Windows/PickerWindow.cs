using System;
using System.Collections.Generic;
using System.Linq;
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

    public void Show(Loader loader, GfxInfo info, ref RlColor bgColor)
    {
        ImGui.Begin("Options", ref _open);

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
                    heroTypes.TryGetBioName(info.OwnerHero, out string? bioName);
                    if (info.CostumeIndex == 0 && !string.IsNullOrEmpty(bioName)) costumeName = $"{bioName} ({costumeType})";
                    else if (loader.TryGetStringName(info.DisplayNameKey, out string? realCostumeName))
                    {
                        costumeName = !string.IsNullOrEmpty(bioName)
                            ? $"{realCostumeName} ({bioName}: {costumeType})"
                            : $"{realCostumeName} ({costumeType})";
                    }
                }

                if (!costumeName.Contains(_costumeTypeFilter, StringComparison.InvariantCultureIgnoreCase))
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
                gfxInfo.CostumeType = null;
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

                if (ImGui.Selectable(weaponSkinName, weaponSkinType == gfxInfo.WeaponSkinType))
                    gfxInfo.WeaponSkinType = weaponSkinType;
            }
            ImGui.EndListBox();
        }
    }

    private void ColorSchemeSection(Loader loader, GfxInfo info)
    {
        ColorSchemeTypes colorSchemeTypes = loader.SwzFiles.Game.ColorSchemeTypes;
        IEnumerable<string> filteredColorSchemes = colorSchemeTypes.ColorSchemes.Where(s => s.Contains(_colorSchemeFilter, StringComparison.InvariantCultureIgnoreCase));
        ImGui.InputText("Filter color schemes", ref _colorSchemeFilter, 256);
        if (ImGui.BeginListBox("###colorselect"))
        {
            if (ImGui.Selectable("None##none", info.ColorScheme is null))
            {
                info.ColorScheme = null;
                OnSelect(loader);
            }

            foreach (string colorScheme in filteredColorSchemes)
            {
                if (ImGui.Selectable(colorScheme, colorScheme == info.ColorScheme))
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