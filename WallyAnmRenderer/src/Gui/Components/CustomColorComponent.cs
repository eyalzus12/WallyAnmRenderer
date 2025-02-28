using BrawlhallaAnimLib.Gfx;
using ImGuiNET;

namespace WallyAnmRenderer;

public static class CustomColorComponent
{
    // to shorten stuff
    private const ColorSchemeSwapEnum HairLt = ColorSchemeSwapEnum.HairLt;
    private const ColorSchemeSwapEnum Hair = ColorSchemeSwapEnum.Hair;
    private const ColorSchemeSwapEnum HairDk = ColorSchemeSwapEnum.HairDk;
    private const ColorSchemeSwapEnum Body1VL = ColorSchemeSwapEnum.Body1VL;
    private const ColorSchemeSwapEnum Body1Lt = ColorSchemeSwapEnum.Body1Lt;
    private const ColorSchemeSwapEnum Body1 = ColorSchemeSwapEnum.Body1;
    private const ColorSchemeSwapEnum Body1Dk = ColorSchemeSwapEnum.Body1Dk;
    private const ColorSchemeSwapEnum Body1VD = ColorSchemeSwapEnum.Body1VD;
    private const ColorSchemeSwapEnum Body1Acc = ColorSchemeSwapEnum.Body1Acc;
    private const ColorSchemeSwapEnum Body2VL = ColorSchemeSwapEnum.Body2VL;
    private const ColorSchemeSwapEnum Body2Lt = ColorSchemeSwapEnum.Body2Lt;
    private const ColorSchemeSwapEnum Body2 = ColorSchemeSwapEnum.Body2;
    private const ColorSchemeSwapEnum Body2Dk = ColorSchemeSwapEnum.Body2Dk;
    private const ColorSchemeSwapEnum Body2VD = ColorSchemeSwapEnum.Body2VD;
    private const ColorSchemeSwapEnum Body2Acc = ColorSchemeSwapEnum.Body2Acc;
    private const ColorSchemeSwapEnum SpecialVL = ColorSchemeSwapEnum.SpecialVL;
    private const ColorSchemeSwapEnum SpecialLt = ColorSchemeSwapEnum.SpecialLt;
    private const ColorSchemeSwapEnum Special = ColorSchemeSwapEnum.Special;
    private const ColorSchemeSwapEnum SpecialDk = ColorSchemeSwapEnum.SpecialDk;
    private const ColorSchemeSwapEnum SpecialVD = ColorSchemeSwapEnum.SpecialVD;
    private const ColorSchemeSwapEnum SpecialAcc = ColorSchemeSwapEnum.SpecialAcc;
    private const ColorSchemeSwapEnum HandsLt = ColorSchemeSwapEnum.HandsLt;
    private const ColorSchemeSwapEnum HandsDk = ColorSchemeSwapEnum.HandsDk;
    private const ColorSchemeSwapEnum HandsSkinLt = ColorSchemeSwapEnum.HandsSkinLt;
    private const ColorSchemeSwapEnum HandsSkinDk = ColorSchemeSwapEnum.HandsSkinDk;
    private const ColorSchemeSwapEnum ClothVL = ColorSchemeSwapEnum.ClothVL;
    private const ColorSchemeSwapEnum ClothLt = ColorSchemeSwapEnum.ClothLt;
    private const ColorSchemeSwapEnum Cloth = ColorSchemeSwapEnum.Cloth;
    private const ColorSchemeSwapEnum ClothDk = ColorSchemeSwapEnum.ClothDk;
    private const ColorSchemeSwapEnum WeaponVL = ColorSchemeSwapEnum.WeaponVL;
    private const ColorSchemeSwapEnum WeaponLt = ColorSchemeSwapEnum.WeaponLt;
    private const ColorSchemeSwapEnum Weapon = ColorSchemeSwapEnum.Weapon;
    private const ColorSchemeSwapEnum WeaponDk = ColorSchemeSwapEnum.WeaponDk;
    private const ColorSchemeSwapEnum WeaponAcc = ColorSchemeSwapEnum.WeaponAcc;

    private static readonly (string, ColorSchemeSwapEnum?[])[] MAIN_TABLE = [
        ("Very Light", [null, Body1VL, Body2VL, SpecialVL, ClothVL, WeaponVL]),
        ("Light", [HairLt, Body1Lt, Body2Lt, SpecialLt, ClothLt, WeaponLt]),
        ("Base", [Hair, Body1, Body2, Special, Cloth, Weapon]),
        ("Dark", [HairDk, Body1Dk, Body2Dk, SpecialDk, ClothDk, WeaponDk]),
        ("Very Dark", [null, Body1VD, Body2VD, SpecialVD, null, null]),
        ("Accent", [null, Body1Acc, Body2Acc, SpecialAcc, null, WeaponAcc]),
    ];

    private static readonly (string, ColorSchemeSwapEnum[])[] HANDS_TABLE = [
        ("Light", [HandsLt, HandsSkinLt]),
        ("Dark", [HandsDk, HandsSkinDk]),
    ];

    public static bool MainTable(string label, ColorScheme colorScheme)
    {
        bool changed = false;
        ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedSame;
        if (ImGui.BeginTable($"##{label}_main", 7, flags))
        {
            ImGui.TableNextColumn();
            ImGui.TableHeader("##");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Hair");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Body 1");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Body 2");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Special");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Cloth");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Weapon");

            foreach ((string name, ColorSchemeSwapEnum?[] row) in MAIN_TABLE)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TableHeader($"##{name}");
                ImGui.SameLine();
                ImGui.Text(name);
                foreach (ColorSchemeSwapEnum? col in row)
                {
                    ImGui.TableNextColumn();
                    if (col is null) continue;
                    uint currentColor = colorScheme[col.Value];
                    uint newColor = ImGuiEx.ColorPicker3Hex($"##${col}", currentColor, new(45, 45));
                    if (currentColor != newColor)
                    {
                        colorScheme[col.Value] = newColor;
                        changed = true;
                    }
                }
            }

            ImGui.EndTable();
        }
        return changed;
    }

    public static bool HandsTable(string label, ColorScheme colorScheme)
    {
        bool changed = false;
        ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.SizingFixedSame;
        if (ImGui.BeginTable($"##{label}_hands", 3, flags))
        {
            ImGui.TableNextColumn();
            ImGui.TableHeader("##");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Hands");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Hands Skin");

            foreach ((string name, ColorSchemeSwapEnum[] row) in HANDS_TABLE)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TableHeader($"##{name}");
                ImGui.SameLine();
                ImGui.Text(name);
                foreach (ColorSchemeSwapEnum col in row)
                {
                    ImGui.TableNextColumn();
                    uint currentColor = colorScheme[col];
                    uint newColor = ImGuiEx.ColorPicker3Hex($"##${col}", currentColor, new(45, 45));
                    if (currentColor != newColor)
                    {
                        colorScheme[col] = newColor;
                        changed = true;
                    }
                }
            }

            ImGui.EndTable();
        }
        return changed;
    }
}