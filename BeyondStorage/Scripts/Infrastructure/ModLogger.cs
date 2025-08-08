using System;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Diagnostics;

namespace BeyondStorage.Scripts.Infrastructure;

public static class ModLogger
{
    private const string Prefix = "[BeyondStorage2]";

    public static void Info(string text)
    {
        Log.Out($"{Prefix}(Info) {text}");
    }

    public static void Error(string error, Exception e = null)
    {
#if DEBUG
        error = StackTraceProvider.AppendStackTrace(error, e);
#endif
        Log.Error($"{Prefix}(Error) {error}");
    }

    /// <summary>
    /// Call to put debug information into the log that you might want users to send you when they report issues.
    /// </summary>
    public static void DebugLog(string text, Exception e = null)
    {
        if (ModConfig.IsDebug())
        {
            if (e != null)
            {
                text = StackTraceProvider.AppendStackTrace(text, e);
            }
            Log.Out($"{Prefix}(Debug) {text}");
        }
    }

    public static void Warning(string text)
    {
        Log.Warning($"{Prefix}(Warn) {text}");
    }
}