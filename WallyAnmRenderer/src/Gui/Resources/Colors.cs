using System.Numerics;

namespace WallyAnmRenderer;

public static class Colors
{
    public static Vector4 BLACK { get; } = ImGuiEx.RGBHexToVec4(0x000000);

    public static Vector4 NOTE_TEXT { get; } = ImGuiEx.RGBHexToVec4(0x00AAFF);
    public static Vector4 WARN_TEXT { get; } = ImGuiEx.RGBHexToVec4(0xFFFF00);
    public static Vector4 ERROR_TEXT { get; } = ImGuiEx.RGBHexToVec4(0xFF0000);
    public static Vector4 SELECTED_OPTION { get; } = ImGuiEx.RGBHexToVec4(0xFF7F00);

    public static Vector4 RECOVERY_START_FRAME { get; } = ImGuiEx.RGBHexToVec4(0xFF7F00);
    public static Vector4 NORMAL_FRAME { get; } = ImGuiEx.RGBHexToVec4(0x00FF00);
    public static Vector4 LOOP_START_FRAME { get; } = ImGuiEx.RGBHexToVec4(0x00FFFF);
    public static Vector4 CURRENT_FRAME { get; } = ImGuiEx.RGBHexToVec4(0xFFFF00);
}