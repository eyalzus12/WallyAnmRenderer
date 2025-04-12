using System;
using System.Numerics;
using ImGuiNET;

namespace WallyAnmRenderer;

public sealed class PickerWindow
{
    private static readonly Vector4 SELECTED_COLOR = ImGuiEx.RGBHexToVec4(0xFF7F00);

    private bool _open = true;
    public bool Open { get => _open; set => _open = value; }

    private string _costumeTypeFilter = "";
    private string _weaponSkinTypeFilter = "";
    private string _spawnBotTypeFilter = "";
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

        ImGui.SeparatorText("Sidekicks");
        SpawnBotTypesSection(loader, info);

        ImGui.SeparatorText("Color scheme");
        ColorSchemeSection(loader, info);

        ImGui.SeparatorText("Custom colors");
        _customColors.Show(info.ColorScheme);

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
        ImGui.InputText("Filter costumes", ref _costumeTypeFilter, 256);
        if (ImGui.BeginListBox("###costumeselect"))
        {
            bool selected = gfxInfo.CostumeType is null;
            if (selected) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
            if (ImGui.Selectable("None##none", selected))
            {
                gfxInfo.CostumeType = null;
                OnSelect(loader);
            }
            if (selected) ImGui.PopStyleColor();

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

                bool selected2 = costumeType == gfxInfo.CostumeType;
                if (selected2) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
                if (ImGui.Selectable(costumeName, selected2))
                {
                    gfxInfo.CostumeType = costumeType;
                    OnSelect(loader);
                }
                if (selected2) ImGui.PopStyleColor();
            }

            ImGui.EndListBox();
        }
    }

    private void WeaponSkinTypeSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        WeaponSkinTypes weaponSkinTypes = loader.SwzFiles.Game.WeaponSkinTypes;
        ImGui.InputText("Filter weapon skins", ref _weaponSkinTypeFilter, 256);
        if (ImGui.BeginListBox("###weaponselect"))
        {
            bool selected = gfxInfo.WeaponSkinType is null;
            if (selected) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
            if (ImGui.Selectable("None##none", selected))
            {
                gfxInfo.WeaponSkinType = null;
            }
            if (selected) ImGui.PopStyleColor();

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

                bool selected2 = weaponSkinType == gfxInfo.WeaponSkinType;
                if (selected2) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
                if (ImGui.Selectable(weaponSkinName, selected2))
                {
                    gfxInfo.WeaponSkinType = weaponSkinType;
                }
                if (selected2) ImGui.PopStyleColor();
            }

            ImGui.EndListBox();
        }
    }

    private void SpawnBotTypesSection(Loader loader, GfxInfo gfxInfo)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        SpawnBotTypes spawnBotTypes = loader.SwzFiles.Game.SpawnBotTypes;
        ImGui.InputText("Filter sidekicks", ref _spawnBotTypeFilter, 256);
        if (ImGui.BeginListBox("###spawnbotselect"))
        {
            bool selected = gfxInfo.SpawnBotType is null;
            if (selected) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
            if (ImGui.Selectable("None##none", selected))
            {
                gfxInfo.SpawnBotType = null;
            }
            if (selected) ImGui.PopStyleColor();

            foreach (string spawnBotType in spawnBotTypes.SpawnBots)
            {
                string spawnBotName = spawnBotType;
                if (spawnBotTypes.TryGetInfo(spawnBotType, out SpawnBotType? info))
                {
                    string displayNameKey = info.DisplayNameKey;
                    if (loader.TryGetStringName(displayNameKey, out string? realSpawnBotName))
                        spawnBotName = $"{realSpawnBotName} ({spawnBotType})";
                }

                if (!spawnBotName.Contains(_spawnBotTypeFilter, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                bool selected2 = spawnBotType == gfxInfo.SpawnBotType;
                if (selected2) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
                if (ImGui.Selectable(spawnBotName, selected2))
                {
                    gfxInfo.SpawnBotType = spawnBotType;
                }
                if (selected2) ImGui.PopStyleColor();
            }

            ImGui.EndListBox();
        }
    }

    private void ColorSchemeSection(Loader loader, GfxInfo info)
    {
        if (loader.SwzFiles?.Game is null)
        {
            ImGui.Text("Swz files were not loaded");
            return;
        }

        ColorSchemeTypes colorSchemeTypes = loader.SwzFiles.Game.ColorSchemeTypes;
        ImGui.InputText("Filter color schemes", ref _colorSchemeFilter, 256);
        if (ImGui.BeginListBox("###colorselect"))
        {
            foreach (ColorScheme colorScheme in colorSchemeTypes.ColorSchemes)
            {
                string colorSchemeName = colorScheme.Name;
                string? displayNameKey = colorScheme.DisplayNameKey;
                if (displayNameKey is not null && loader.TryGetStringName(displayNameKey, out string? realSchemeName))
                    colorSchemeName = $"{realSchemeName} ({colorSchemeName})";

                if (!colorSchemeName.Contains(_colorSchemeFilter, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                bool selected = colorScheme == info.ColorScheme;
                if (selected) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
                if (ImGui.Selectable(colorSchemeName, selected))
                {
                    ColorSchemeSelected?.Invoke(this, colorScheme);
                }
                if (selected) ImGui.PopStyleColor();
            }

            bool selected2 = info.ColorScheme == ColorScheme.DEBUG;
            if (selected2) ImGui.PushStyleColor(ImGuiCol.Text, SELECTED_COLOR);
            if (ImGui.Selectable("DEBUG (not a real color scheme)", selected2))
            {
                ColorSchemeSelected?.Invoke(this, ColorScheme.DEBUG);
            }
            if (selected2) ImGui.PopStyleColor();

            ImGui.EndListBox();
        }
    }

    private static void OnSelect(Loader loader)
    {
        loader.AssetLoader.ClearSwfShapeCache();
    }
}