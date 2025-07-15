using BeyondStorage.Scripts.Configuration;
namespace BeyondStorage.Scripts.Utils;

public static class LogUtil
{
    private const string Prefix = "[BeyondStorage2]";

    public static bool IsDebug()
    {
        return ModConfig.IsDebug();
    }

    public static bool IsDebugLogSettingsAccess()
    {
        // Independent of IsDebug, this is used to control whether settings access logs are printed
        return ModConfig.IsDebugLogSettingsAccess();
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
        Log.Out($"{Prefix}(Debug) {text}");
    }

    public static void Warning(string text)
    {
        Log.Warning($"{Prefix}(Warn) {text}");
    }
}