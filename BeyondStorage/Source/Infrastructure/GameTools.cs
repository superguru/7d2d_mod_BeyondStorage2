using System;

namespace BeyondStorage.Source.Infrastructure;

public static class GameTools
{
    public static string GetLocalisedValue(string methodName, string localisationKey, object[] formatArgs = null)
    {
        (bool passed, string value) = CheckGamePrerequisites(methodName);
        if (!passed)
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: Game prerequisites not met, returning key as fallback: '{localisationKey}'");
#endif
            return localisationKey; // Return the key as fallback, not empty string
        }

        string localisedMessageFmt = Localization.Get(localisationKey, _caseInsensitive: true);

        // Fallback to key if localization not found
        if (string.IsNullOrEmpty(localisedMessageFmt))
        {
            ModLogger.DebugLog($"{methodName}: No localization found for key '{localisationKey}', using key as fallback");
            return localisationKey;
        }

        if (formatArgs == null || formatArgs.Length == 0)
        {
            ModLogger.DebugLog($"{methodName}: No format arguments provided, returning localised message: '{localisedMessageFmt}'");
            return localisedMessageFmt;
        }

        try
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: Formatting localised message with {formatArgs.Length} argument(s)");
#endif
            string localisedMessage = string.Format(localisedMessageFmt, formatArgs);
            return localisedMessage;
        }
        catch (FormatException ex)
        {
            ModLogger.DebugLog($"{methodName}: Format exception - '{ex.Message}'. Returning unformatted message: '{localisedMessageFmt}'");
            return localisedMessageFmt; // Return unformatted message as fallback
        }
    }

    private static (bool passed, string value) CheckGamePrerequisites(string methodName)
    {
        if (!WorldTools.IsWorldExists())
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: World does not exist, skipping localisation attempt");
#endif
            return (passed: false, value: "");
        }

        if (!WorldTools.IsWorldHasPrimaryPlayer())
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: World does not have a primary player, skipping localisation attempt");
#endif
            return (passed: false, value: "");
        }

        if (GameManager.Instance == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: GameManager reference is null, skipping localisation attempt");
#endif
            return (passed: false, value: "");
        }

        return (passed: true, value: null);
    }
}
