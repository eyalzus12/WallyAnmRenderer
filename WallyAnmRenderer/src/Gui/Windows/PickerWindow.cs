using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using WallyAnmSpinzor;

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
        ImGui.Begin("Picker", ref _open);

        ImGui.SeparatorText("Animation");
        AnimationSection(loader, info);

        ImGui.SeparatorText("Costume Types");
        CostumeTypeSection(loader, info);

        ImGui.SeparatorText("Weapon skin Types");
        WeaponSkinTypeSection(loader, info);

        ImGui.SeparatorText("Color scheme");
        ColorSchemeSection(loader, info);

        ImGui.End();
    }

    private readonly Dictionary<string, string> _animationFilterState = [];

    private void AnimationSection(Loader loader, GfxInfo info)
    {
        if (!loader.AssetLoader.AnmLoadingFinished)
        {
            ImGui.Text("Loading anm files...");
        }

        var groups = loader.AssetLoader.AnmClasses.Keys.Select(s =>
        {
            string[] parts = s.Split('/', 2);
            string animFile = parts[0];
            string animClass = parts[1];
            return (animFile, animClass);
        }).GroupBy((item) => item.animFile, (item) => item.animClass);

        foreach (IGrouping<string, string> group in groups)
        {
            string animFile = group.Key;
            if (ImGui.TreeNode(animFile))
            {
                foreach (string animClass in group)
                {
                    AnmClass anmClass = loader.AssetLoader.AnmClasses[$"{animFile}/{animClass}"];

                    string filter = _animationFilterState.GetValueOrDefault(animClass, "");
                    if (ImGui.InputText("Filter animations", ref filter, 256))
                    {
                        _animationFilterState[animClass] = filter;
                    }

                    if (ImGui.BeginListBox(animClass))
                    {
                        IEnumerable<string> filteredAnimations = anmClass.Animations.Keys.Where(a => a.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
                        foreach (string animation in filteredAnimations)
                        {
                            if (ImGui.Selectable(animation, animation == info.Animation))
                            {
                                info.AnimFile = animFile;
                                info.AnimClass = animClass;
                                info.Animation = animation;
                            }
                        }
                        ImGui.EndListBox();
                    }
                }
                ImGui.TreePop();
            }
        }
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
                    info.CostumeType = costumeType;
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
        IEnumerable<string> filteredColorSchemes = colorSchemeTypes.ColorSchemes.Where(s => s.Contains(_weaponSkinTypeFilter, StringComparison.InvariantCultureIgnoreCase));
        ImGui.InputText("Filter color schemes", ref _colorSchemeFilter, 256);
        if (ImGui.BeginListBox("###colorselect"))
        {
            foreach (string? colorScheme in filteredColorSchemes.Prepend<string?>(null))
            {
                if (ImGui.Selectable(colorScheme ?? "None##none", colorScheme == info.ColorScheme))
                {
                    info.ColorScheme = colorScheme;
                    loader.ClearCache();
                }
            }
            ImGui.EndListBox();
        }
    }
}