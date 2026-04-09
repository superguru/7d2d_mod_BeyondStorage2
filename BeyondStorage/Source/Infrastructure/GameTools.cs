namespace BeyondStorage.Scripts.Infrastructure;

public static class GameTools
{
    public static string GetLocalisedMessage(string methodName, string localisationKey, object[] formatArgs)
    {
        if (!WorldTools.IsWorldExists())
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: World does not exist, skipping local notification");
#endif
            return "";
        }

        if (!WorldTools.IsWorldHasPrimaryPlayer())
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: World does not have a primary player, skipping local notification");
#endif
            return "";
        }

        if (GameManager.Instance == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: GameManager reference is null, cannot show notification");
#endif
            return "";
        }

        string localisedMessageFmt = Localization.Get(localisationKey);
        string localisedMessage = string.Format(localisedMessageFmt, formatArgs);

        return localisedMessage;
    }
}
