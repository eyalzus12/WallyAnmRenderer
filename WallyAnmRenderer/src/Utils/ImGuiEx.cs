using System;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace WallyAnmRenderer;

public static class ImGuiEx
{
    public static bool InputUInt(string label, ref uint value, uint step = 1, uint stepFast = 100)
    {
        unsafe
        {
            IntPtr valuePtr = (IntPtr)Unsafe.AsPointer(ref value);
            IntPtr stepPtr = (IntPtr)(&step);
            IntPtr stepFastPtr = (IntPtr)(&stepFast);
            return ImGui.InputScalar(label, ImGuiDataType.U32, valuePtr, stepPtr, stepFastPtr);
        }
    }

    public static uint InputUInt(string label, uint value, uint step = 1, uint stepFast = 100)
    {
        uint v = value;
        InputUInt(label, ref v, step, stepFast);
        return v;
    }
}