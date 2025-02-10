using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace WallyAnmRenderer;

public static class LogCallback
{
    public static void Init()
    {
#if DEBUG
        Rl.SetTraceLogLevel(TraceLogLevel.All);
#else
        Rl.SetTraceLogLevel(TraceLogLevel.Warning);
#endif
        unsafe { Rl.SetTraceLogCallback(&TraceLogCallback); }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void TraceLogCallback(int logLevel, sbyte* fmtPtr, sbyte* argsPtr)
    {
        string text = NativeVsPrintf.GetString((nint)fmtPtr, (nint)argsPtr);

        switch ((TraceLogLevel)logLevel)
        {
            case TraceLogLevel.Trace:
                Console.WriteLine($"TRACE: {text}");
                break;
            case TraceLogLevel.Debug:
                Console.WriteLine($"DEBUG: {text}");
                break;
            case TraceLogLevel.Info:
                Console.WriteLine($"INFO: {text}");
                break;
            case TraceLogLevel.Warning:
                Console.WriteLine($"WARNING: {text}");
                break;
            case TraceLogLevel.Error:
                Console.WriteLine($"ERROR: {text}");
                break;
            case TraceLogLevel.Fatal:
                Console.WriteLine($"FATAL: {text}");
                break;
            default:
                break;
        }

        if (logLevel == (int)TraceLogLevel.Fatal)
        {
            Environment.Exit(1);
        }
    }
}