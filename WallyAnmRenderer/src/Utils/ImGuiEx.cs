using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace WallyAnmRenderer;

public static class ImGuiEx
{
    public static unsafe bool InputUInt(string label, ref uint value, uint step = 1, uint stepFast = 100)
    {
        IntPtr valuePtr = (IntPtr)Unsafe.AsPointer(ref value);
        IntPtr stepPtr = (IntPtr)(&step);
        IntPtr stepFastPtr = (IntPtr)(&stepFast);
        return ImGui.InputScalar(label, ImGuiDataType.U32, valuePtr, stepPtr, stepFastPtr);
    }

    public static uint InputUInt(string label, uint value, uint step = 1, uint stepFast = 100)
    {
        uint v = value;
        InputUInt(label, ref v, step, stepFast);
        return v;
    }

    public static unsafe bool InputDouble(string label, ref double value, double step = 1, double stepFast = 100)
    {
        IntPtr valuePtr = (IntPtr)Unsafe.AsPointer(ref value);
        IntPtr stepPtr = (IntPtr)(&step);
        IntPtr stepFastPtr = (IntPtr)(&stepFast);
        return ImGui.InputScalar(label, ImGuiDataType.Double, valuePtr, stepPtr, stepFastPtr);
    }

    public static unsafe bool DragDouble(string label, ref double value, float speed = 1, double minValue = double.MinValue, double maxValue = double.MaxValue)
    {
        IntPtr valuePtr = (IntPtr)Unsafe.AsPointer(ref value);
        IntPtr minValuePtr = (IntPtr)(&minValue);
        IntPtr maxValuePtr = (IntPtr)(&maxValue);
        return ImGui.DragScalar(label, ImGuiDataType.Double, valuePtr, speed, minValuePtr, maxValuePtr);
    }

    public static uint ColorPicker3Hex(string label, uint col, Vector2 size = default)
    {
        byte r = (byte)(col >> 16), g = (byte)(col >> 8), b = (byte)col;
        Vector3 imCol = new((float)r / 255, (float)g / 255, (float)b / 255);
        if (ImGui.ColorButton(label, new(imCol, 1), ImGuiColorEditFlags.NoAlpha, size))
            ImGui.OpenPopup(label);
        if (ImGui.BeginPopup(label))
        {
            ImGui.ColorPicker3(label, ref imCol);
            ImGui.EndPopup();
        }
        r = (byte)(imCol.X * 255); g = (byte)(imCol.Y * 255); b = (byte)(imCol.Z * 255);
        return ((uint)r << 16) | ((uint)g << 8) | b;
    }

    public static bool DisabledButton(string label, bool disabled)
    {
        if (disabled) ImGui.BeginDisabled();
        bool result = ImGui.Button(label);
        if (disabled) ImGui.EndDisabled();
        return result;
    }

    public static bool DisabledSelectable(string label, bool disabled)
    {
        if (disabled) ImGui.BeginDisabled();
        bool result = ImGui.Selectable(label);
        if (disabled) ImGui.EndDisabled();
        return result;
    }

    public static void BeginStyledChild(string label)
    {
        unsafe { ImGui.PushStyleColor(ImGuiCol.ChildBg, *ImGui.GetStyleColorVec4(ImGuiCol.FrameBg)); }
        ImGui.BeginChild(label, new Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 8), ImGuiChildFlags.ResizeY | ImGuiChildFlags.Borders);
    }

    public static void EndStyledChild()
    {
        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    public static Vector4 RGBHexToVec4(uint hex)
    {
        float r = ((hex >> 16) & 0xFF) / 255f;
        float g = ((hex >> 8) & 0xFF) / 255f;
        float b = (hex & 0xFF) / 255f;
        return new(r, g, b, 1);
    }
}