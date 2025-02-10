using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WallyAnmRenderer;

internal partial class NativeVsPrintf
{
    private const string MSVCRT = "msvcrt";
    private const string LIBC = "libc";
    private const string LIBSYSTEM = "libSystem";

    [LibraryImport(MSVCRT, EntryPoint = "vsprintf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeVsPrintf_Windows(nint buffer, nint format, nint args);

    [LibraryImport(MSVCRT, EntryPoint = "vsnprintf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeVsnPrintf_Windows(nint buffer, nuint size, nint format, nint args);

    [LibraryImport(LIBC, EntryPoint = "vsprintf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeVsPrintf_Linux(nint buffer, nint format, nint args);

    [LibraryImport(LIBC, EntryPoint = "vsnprintf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeVsnPrintf_Linux(nint buffer, nuint size, nint format, nint args);

    [LibraryImport(LIBSYSTEM, EntryPoint = "vasprintf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int NativeVasPrintf_Osx(ref nint buffer, nint format, nint args);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct VaListLinux64
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private uint _gpOffset;
        private uint _fpOffset;
        private nint _overflowArgsArea;
        private nint _regSaveArea;
#pragma warning restore IDE0044 // Add readonly modifier
    }

    public static string GetString(nint format, nint args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return VsPrintf_Windows(format, args);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (nint.Size == 8)
                return VsPrintf_Linux64(format, args);

            return VsPrintf_Linux86(format, args);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return VsPrintf_Osx(format, args);

        throw new PlatformNotSupportedException("Logging with va_list not supported on this platform");
    }

    private static string VsPrintf_Windows(nint format, nint args)
    {
        nint buffer = nint.Zero;

        try
        {
            int byteSize = NativeVsnPrintf_Windows(nint.Zero, nuint.Zero, format, args) + 1;

            if (byteSize <= 1)
                return string.Empty;

            buffer = Marshal.AllocHGlobal(byteSize);
            _ = NativeVsPrintf_Windows(buffer, format, args);

            return Marshal.PtrToStringUTF8(buffer)!;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string VsPrintf_Linux64(nint format, nint args)
    {
        nint listPointer = nint.Zero;
        nint buffer = nint.Zero;

        try
        {
            VaListLinux64 str = Marshal.PtrToStructure<VaListLinux64>(args);

            listPointer = Marshal.AllocHGlobal(Marshal.SizeOf(str));
            Marshal.StructureToPtr(str, listPointer, false);
            int byteSize = NativeVsnPrintf_Linux(nint.Zero, nuint.Zero, format, listPointer) + 1;

            Marshal.StructureToPtr(str, listPointer, false);
            buffer = Marshal.AllocHGlobal(byteSize);

            _ = NativeVsPrintf_Linux(buffer, format, listPointer);
            return Marshal.PtrToStringUTF8(buffer)!;
        }
        finally
        {
            Marshal.FreeHGlobal(listPointer);
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string VsPrintf_Linux86(nint format, nint args)
    {
        nint buffer = nint.Zero;

        try
        {
            int byteSize = NativeVsnPrintf_Linux(nint.Zero, nuint.Zero, format, args) + 1;

            if (byteSize <= 1)
                return string.Empty;

            buffer = Marshal.AllocHGlobal(byteSize);
            _ = NativeVsPrintf_Linux(buffer, format, args);

            return Marshal.PtrToStringUTF8(buffer)!;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string VsPrintf_Osx(nint format, nint args)
    {
        nint buffer = nint.Zero;

        try
        {
            int byteSize = NativeVasPrintf_Osx(ref buffer, format, args);

            if (byteSize == -1)
                return string.Empty;

            return Marshal.PtrToStringUTF8(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}