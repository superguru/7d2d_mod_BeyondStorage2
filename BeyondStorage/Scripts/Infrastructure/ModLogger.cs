using BeyondStorage.Scripts.Configuration;

namespace BeyondStorage.Scripts.Infrastructure;

public static class ModLogger
{
    private const string Prefix = "[BeyondStorage2]";

    public static void Info(string text)
    {
        Log.Out($"{Prefix}(Info) {text}");
    }

    public static void Error(string text)
    {
        Log.Error($"{Prefix}(Error) {text}");
    }

    /// <summary>
    /// Call to put debug information into the log that you might want users to send you when they report issues.
    /// </summary>
    public static void DebugLog(string text)
    {
        if (ModConfig.IsDebug())
        {
            Log.Out($"{Prefix}(Debug) {text}");
        }
    }

    public static void Warning(string text)
    {
        Log.Warning($"{Prefix}(Warn) {text}");
    }
}