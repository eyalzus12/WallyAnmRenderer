using System;
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

    private static void AnimationSection(Loader loader, GfxInfo info)
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
                    if (ImGui.BeginListBox(animClass))
                    {
                        foreach (string animation in anmClass.Animations.Keys)
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
        string[] filteredCostumeTypes = [.. costumeTypes.Costumes.Where(s => s.Contains(_costumeTypeFilter, StringComparison.InvariantCultureIgnoreCase))];
        ImGui.InputText("Filter costumes", ref _costumeTypeFilter, 256);
        if (ImGui.BeginListBox("###costumeselect"))
        {
            foreach (string costumeType in filteredCostumeTypes)
            {
                if (ImGui.Selectable(costumeType, costumeType == info.CostumeType))
                    info.CostumeType = costumeType;
            }
            ImGui.EndListBox();
        }
    }

    private void WeaponSkinTypeSection(Loader loader, GfxInfo info)
    {
        WeaponSkinTypes weaponSkinTypes = loader.SwzFiles.Game.WeaponSkinTypes;
        string[] filteredWeaponSkinTypes = [.. weaponSkinTypes.WeaponSkins.Where(s => s.Contains(_weaponSkinTypeFilter, StringComparison.InvariantCultureIgnoreCase))];
        ImGui.InputText("Filter weapon skins", ref _weaponSkinTypeFilter, 256);
        if (ImGui.BeginListBox("###weaponselect"))
        {
            foreach (string weaponSkinType in filteredWeaponSkinTypes)
            {
                if (ImGui.Selectable(weaponSkinType, weaponSkinType == info.WeaponSkinType))
                    info.WeaponSkinType = weaponSkinType;
            }
            ImGui.EndListBox();
        }
    }

    private static void ColorSchemeSection(Loader loader, GfxInfo info)
    {
        ColorSchemeTypes colorSchemeTypes = loader.SwzFiles.Game.ColorSchemeTypes;
        if (ImGui.BeginListBox("###colorselect"))
        {
            foreach (string? colorScheme in colorSchemeTypes.ColorSchemes.Prepend<string?>(null))
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