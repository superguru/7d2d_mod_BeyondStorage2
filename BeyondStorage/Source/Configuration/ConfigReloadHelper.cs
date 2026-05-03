using System;
using BeyondStorage.Infrastructure;

namespace BeyondStorage.Configuration;

/// <summary>
/// Helper class for reloading configuration from disk.
/// Provides centralized config reload functionality across multiple commands.
/// </summary>
public static class ConfigReloadHelper
{
    /// <summary>
    /// Reloads configuration from disk.
    /// </summary>
    public static void ReloadConfig()
    {
        try
        {
            ModConfig.LoadConfig();
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Failed to reload config: {ex.Message}", ex);
        }
    }
}