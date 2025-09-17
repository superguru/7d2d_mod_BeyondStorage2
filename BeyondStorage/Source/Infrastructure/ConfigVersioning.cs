using System;
using BeyondStorage.Scripts.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeyondStorage.Scripts.Infrastructure;

/// <summary>
/// Handles config versioning, migration, and backwards compatibility
/// </summary>
public static class ConfigVersioning
{
    /// <summary>
    /// The first version to include versioning (2.3.0)
    /// </summary>
    public const string FirstVersionedConfig = "2.3.0";

    /// <summary>
    /// Cached current version - populated on first access
    /// </summary>
    private static string s_currentVersion = null;

    /// <summary>
    /// Current config schema version - always matches ModInfo.Version (lazy loaded)
    /// </summary>
    public static string CurrentVersion
    {
        get
        {
            if (string.IsNullOrEmpty(s_currentVersion))
            {
                s_currentVersion = ModInfo.Version;
            }
            return s_currentVersion;
        }
    }

    /// <summary>
    /// Detects if a config JSON string is legacy (pre-versioning)
    /// </summary>
    /// <param name="configJson">Raw JSON string from config file</param>
    /// <returns>True if legacy config (no version field)</returns>
    public static bool IsLegacyConfig(string configJson)
    {
        try
        {
            var jsonObject = JObject.Parse(configJson);
            return !jsonObject.ContainsKey("version");
        }
        catch (JsonException)
        {
            return true; // If we can't parse it, treat it as legacy
        }
    }

    /// <summary>
    /// Migrates a legacy config (pre-2.3.0) to the current versioned format
    /// </summary>
    /// <param name="legacyConfigJson">Legacy config JSON string</param>
    /// <returns>Migrated BsConfig object</returns>
    public static BsConfig MigrateLegacyConfig(string legacyConfigJson)
    {
        const string d_MethodName = nameof(MigrateLegacyConfig);
        ModLogger.Info($"{d_MethodName}: Migrating legacy config (pre-{FirstVersionedConfig}) to version {CurrentVersion}");

        try
        {
            // Parse legacy config without version field
            var legacyConfig = JsonConvert.DeserializeObject<LegacyBsConfig>(legacyConfigJson);

            // Create new versioned config with migrated values
            var migratedConfig = new BsConfig
            {
                version = CurrentVersion,

                // Migrate existing settings (no changes for 2.3.0)
                range = legacyConfig.range,

                pullFromDrones = legacyConfig.pullFromDrones,
                pullFromDewCollectors = legacyConfig.pullFromDewCollectors,
                pullFromWorkstationOutputs = legacyConfig.pullFromWorkstationOutputs,
                pullFromPlayerCraftedNonCrates = !legacyConfig.onlyStorageCrates,
                pullFromVehicleStorage = legacyConfig.pullFromVehicleStorage,

                serverSyncConfig = legacyConfig.serverSyncConfig,

                isDebug = legacyConfig.isDebug,
                isDebugLogSettingsAccess = legacyConfig.isDebugLogSettingsAccess
            };

            ModLogger.Info($"{d_MethodName}: Successfully migrated legacy config");
            return migratedConfig;
        }
        catch (Exception e)
        {
            ModLogger.Error($"{d_MethodName}: Failed to migrate legacy config: {e.Message}", e);
            throw;
        }
    }

    /// <summary>
    /// Migrates a versioned config to the current version if needed
    /// </summary>
    /// <param name="config">Config to potentially migrate</param>
    /// <returns>Migrated config (may be the same object if no migration needed)</returns>
    public static BsConfig MigrateVersionedConfig(BsConfig config)
    {
        const string d_MethodName = nameof(MigrateVersionedConfig);

        if (config.version == CurrentVersion)
        {
            ModLogger.DebugLog($"{d_MethodName}: Config is already current version {CurrentVersion}");
            return config;
        }

        ModLogger.Info($"{d_MethodName}: Migrating config from version {config.version} to {CurrentVersion}");

        // Parse versions for comparison
        if (!TryParseVersion(config.version, out var fromVersion) ||
            !TryParseVersion(CurrentVersion, out var toVersion))
        {
            ModLogger.Warning($"{d_MethodName}: Unable to parse versions, using config as-is");
            config.version = CurrentVersion; // Update version field
            return config;
        }

        // Apply migrations in sequence
        var migratedConfig = config;

        // Migration to version 2.3.5: Disable debug mode on servers
        if (fromVersion < new Version("2.3.5"))
        {
            migratedConfig = MigrateTo235(migratedConfig);
        }

        // Always update to current version
        migratedConfig.version = CurrentVersion;

        ModLogger.Info($"{d_MethodName}: Successfully migrated config to version {CurrentVersion}");
        return migratedConfig;
    }

    /// <summary>
    /// Attempts to parse a version string into a Version object
    /// </summary>
    private static bool TryParseVersion(string versionString, out System.Version version)
    {
        version = null;
        if (string.IsNullOrEmpty(versionString))
        {
            return false;
        }

        return System.Version.TryParse(versionString, out version);
    }

    /// <summary>
    /// Migrates config to version 2.3.5
    /// Changes: Disables debug mode when running on a server to prevent performance issues
    /// </summary>
    /// <param name="config">Config to migrate</param>
    /// <returns>Migrated config</returns>
    private static BsConfig MigrateTo235(BsConfig config)
    {
        const string d_MethodName = nameof(MigrateTo235);
        ModLogger.Info($"{d_MethodName}: Applying migration to version 2.3.5");

        // Check if we're running on a server and debug mode is enabled
        if (config.isDebug && WorldTools.IsServer())
        {
            ModLogger.Info($"{d_MethodName}: Server environment detected - disabling debug mode for performance optimization");
            config.isDebug = false;

            // Also disable debug logging settings access since it depends on debug mode
            if (config.isDebugLogSettingsAccess)
            {
                config.isDebugLogSettingsAccess = false;
                ModLogger.Info($"{d_MethodName}: Also disabled debug settings access logging");
            }
        }
        else if (config.isDebug)
        {
            ModLogger.Info($"{d_MethodName}: Debug mode remains enabled (not running on server)");
        }
        else
        {
            ModLogger.DebugLog($"{d_MethodName}: Debug mode already disabled - no changes needed");
        }

        ModLogger.Info($"{d_MethodName}: Successfully applied 2.3.5 migration");
        return config;
    }

    /// <summary>
    /// Legacy config structure for migration (pre-2.3.0)
    /// </summary>
    private class LegacyBsConfig
    {
        public float range = -1.0f;
        public bool pullFromDrones = true;
        public bool pullFromDewCollectors = true;
        public bool pullFromWorkstationOutputs = true;
        public bool onlyStorageCrates = false;
        public bool pullFromVehicleStorage = true;
        public bool enableForBlockRepair = true;
        public bool enableForBlockTexture = true;
        public bool enableForBlockUpgrade = true;
        public bool enableForGeneratorRefuel = true;
        public bool enableForItemRepair = true;
        public bool enableForReload = true;
        public bool enableForVehicleRefuel = true;
        public bool enableForVehicleRepair = true;
        public bool serverSyncConfig = true;
        public bool isDebug = false;
        public bool isDebugLogSettingsAccess = false;
    }
}