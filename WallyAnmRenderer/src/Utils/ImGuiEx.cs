using System;
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
}