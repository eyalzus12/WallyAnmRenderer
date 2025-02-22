using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class PickerWindow
{
    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    private string _costumeTypeFilter = "";
    private string _weaponSkinTypeFilter = "";
    private string _colorSchemeFilter = "";

    public void Show(Loader loader, GfxInfo info)
    {
        ImGui.Begin("Options", ref _open);

        ImGui.SeparatorText("Config");

        bool flip = info.Flip;
        if (ImGui.Checkbox("Flip", ref flip))
            info.Flip = flip;

        ImGui.SeparatorText("Costume Types");
        CostumeTypeSection(loader, info);

        ImGui.SeparatorText("Weapon skin Types");
        WeaponSkinTypeSection(loader, info);

        ImGui.SeparatorText("Color scheme");
        ColorSchemeSection(loader, info);

        ImGui.End();
    }

    private void CostumeTypeSection(Loader loader, GfxInfo info)
    {
        CostumeTypes costumeTypes = loader.SwzFiles.Game.CostumeTypes;
        IEnumerable<string> filteredCostumeTypes = costumeTypes.Costumes.Where(s => s.Contains(_costumeTypeFilter, StringComparison.InvariantCultureIgnoreCase));
        ImGui.InputText("Filter costumes", ref _costumeTypeFilter, 256);
        if (ImGui.BeginListBox("###costumeselect"))
        {
            foreach (string? costumeType in filteredCostumeTypes.Prepend<string?>(null))
            {
                if (ImGui.Selectable(costumeType ?? "None##none", costumeType == info.CostumeType))
                {
                    info.CostumeType = costumeType;
                    OnSelect(loader);
                }
            }
            ImGui.EndListBox();
        }
    }

    private void WeaponSkinTypeSection(Loader loader, GfxInfo info)
    {
        WeaponSkinTypes weaponSkinTypes = loader.SwzFiles.Game.WeaponSkinTypes;
        IEnumerable<string> filteredWeaponSkinTypes = weaponSkinTypes.WeaponSkins.Where(s => s.Contains(_weaponSkinTypeFilter, StringComparison.InvariantCultureIgnoreCase));
        ImGui.InputText("Filter weapon skins", ref _weaponSkinTypeFilter, 256);
        if (ImGui.BeginListBox("###weaponselect"))
        {
            foreach (string? weaponSkinType in filteredWeaponSkinTypes.Prepend<string?>(null))
            {
                if (ImGui.Selectable(weaponSkinType ?? "None##none", weaponSkinType == info.WeaponSkinType))
                    info.WeaponSkinType = weaponSkinType;
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
            foreach (string? colorScheme in filteredColorSchemes.Prepend<string?>(null))
            {
                if (ImGui.Selectable(colorScheme ?? "None##none", colorScheme == info.ColorScheme))
                {
                    info.ColorScheme = colorScheme;
                    OnSelect(loader);
                }
            }
            ImGui.EndListBox();
        }
    }

    private static void OnSelect(Loader loader)
    {
        loader.AssetLoader.ClearSwfShapeCache();
    }
}