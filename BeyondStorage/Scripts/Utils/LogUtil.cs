using BeyondStorage.Scripts.Configuration;
namespace BeyondStorage.Scripts.Utils;

public static class LogUtil
{
    private const string Prefix = "[BeyondStorage2]";

    public static bool IsDebugLogSettingsAccess()
    {
        // This is used to control whether settings access logs are printed
        return ModConfig.IsDebug() && ModConfig.IsDebugLogSettingsAccess();
    }

    public static void Info(string text)
    {
        Log.Out($"{Prefix}(Info) {text}");
    }

    public static void Error(string text)
    {
        Log.Error($"{Prefix}(Error) {text}");
    }

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