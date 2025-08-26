using System;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Diagnostics;

namespace BeyondStorage.Scripts.Infrastructure;

public static class ModLogger
{
    private static string s_prefix = "";
    private static string Prefix
    {
        get
        {
            if (string.IsNullOrEmpty(s_prefix))
            {
                try
                {
                    s_prefix = $"{ModInfo.ModName}(v{ModInfo.Version})";
                }
                catch (Exception)
                {
                    // Fallback to just ModName if version retrieval fails
                    s_prefix = ModInfo.ModName;
                }
            }
            return s_prefix;
        }
    }

    public static void Info(string text)
    {
        Log.Out($"{Prefix}(Info) {text}");
    }

    public static void Error(string error, Exception e = null)
    {
        // Do NOT call this if you don't absolutely have to.
        // It will cause Red log message in the game console, which will also open because of that.
        // This disrupts players, server admins, and everyone else. Don't do it.
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
#if DEBUG
            text = StackTraceProvider.AppendStackTrace(text, e);
#endif
            Log.Out($"{Prefix}(Debug) {text}");
        }
    }

    public static void Warning(string text)
    {
        Log.Warning($"{Prefix}(Warn) {text}");
    }
}