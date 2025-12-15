using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Configuration;

/// <summary>
/// Helper class for displaying configuration information to the console.
/// Provides reusable methods for config display across multiple commands.
/// </summary>
public static class ConfigDisplayHelper
{
    /// <summary>
    /// Displays the current active config settings using ConfigSnapshot.
    /// </summary>
    public static void ShowConfig()
    {
        var snapshot = ConfigSnapshot.Current;
        string configJson = snapshot.ToJson();
        ModLogger.Info($"Current Config Snapshot:\n{configJson}\nDo not copy and paste this into the config.json file. The values above are formatted for reading in the console.");
    }
}