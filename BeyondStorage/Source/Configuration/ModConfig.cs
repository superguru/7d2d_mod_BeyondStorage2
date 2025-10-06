using System;
using System.IO;
using System.Linq;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Multiplayer;
using Newtonsoft.Json;

#if DEBUG
using System.Reflection;
#endif

namespace BeyondStorage.Scripts.Configuration;

public static class ModConfig
{
    private const string ConfigFileName = "config.json";
    private const string ConfigBackupPrefix = "config.backup.";

    /// <summary>
    /// Maximum allowed config file size in bytes (1KB) to prevent abuse
    /// </summary>
    private const long MaxConfigFileSize = 1024;

    public static BsConfig ClientConfig { get; private set; }
    public static BsConfig ServerConfig { get; } = new();
    private static bool IsConfigLoaded { get; set; } = false;

    /// <summary>
    /// Gets the full path to the configuration file
    /// </summary>
    /// <returns>Full path to the config.json file</returns>
    private static string GetConfigFilePath()
    {
        return Path.Combine(ModPathManager.GetConfigPath(true), ConfigFileName);
    }

    /// <summary>
    /// Gets the full path to the legacy configuration file location
    /// </summary>
    /// <returns>Full path to the legacy config.json file</returns>
    private static string GetLegacyConfigFilePath()
    {
        return Path.Combine(ModPathManager.GetLegacyConfigPath(), ConfigFileName);
    }

    public static void LoadConfig(BeyondStorageMod context)
    {
        // Check for config migration needs (v2.4.0+)
        MigrateConfigLocation();

        var path = GetConfigFilePath();
        ModLogger.DebugLog($"Loading config from {path}");

        if (File.Exists(path))
        {
            try
            {
                // Check file size before loading to prevent abuse
                var fileInfo = new FileInfo(path);
                if (fileInfo.Length > MaxConfigFileSize)
                {
                    ModLogger.Error($"Config file is too large ({fileInfo.Length} bytes, max {MaxConfigFileSize} bytes). Using default config to prevent abuse.");
                    ClientConfig = new BsConfig();
                    IsConfigLoaded = true;
                    return;
                }

                // Read and validate config content
                string configJson;
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(fileStream))
                {
                    var buffer = new char[MaxConfigFileSize];
                    var charsRead = reader.Read(buffer, 0, buffer.Length);

                    if (charsRead == buffer.Length && !reader.EndOfStream)
                    {
                        ModLogger.Error($"Config file content exceeds {MaxConfigFileSize} bytes. Truncated and using default config to prevent abuse.");
                        ClientConfig = new BsConfig();
                        IsConfigLoaded = true;
                        return;
                    }

                    configJson = new string(buffer, 0, charsRead);
                }

                // Validate that we have actual JSON content
                if (string.IsNullOrWhiteSpace(configJson))
                {
                    ModLogger.Warning("Config file is empty or contains only whitespace. Using default config.");
                    ClientConfig = new BsConfig();
                    IsConfigLoaded = true;
                    return;
                }

                ModLogger.DebugLog($"Config file size: {fileInfo.Length} bytes (within {MaxConfigFileSize} byte limit)");

                BsConfig loadedConfig;
                bool configWasMigrated = false;

                if (ConfigVersioning.IsLegacyConfig(configJson))
                {
                    // Handle legacy config (pre-2.3.0)
                    ModLogger.Info("Detected legacy config file, migrating to versioned format");
                    CreateConfigBackup(path, "legacy");

                    loadedConfig = ConfigVersioning.MigrateLegacyConfig(configJson);
                    configWasMigrated = true;
                }
                else
                {
                    // Load versioned config with additional validation
                    loadedConfig = SafeDeserializeConfig(configJson);
                    if (loadedConfig == null)
                    {
                        ModLogger.Error("Failed to deserialize config JSON. Using default config.");
                        ClientConfig = new BsConfig();
                        IsConfigLoaded = true;
                        return;
                    }

                    // Check if migration is needed
                    if (loadedConfig.version != ConfigVersioning.CurrentVersion)
                    {
                        CreateConfigBackup(path, loadedConfig.version);
                        loadedConfig = ConfigVersioning.MigrateVersionedConfig(loadedConfig);
                        configWasMigrated = true;
                    }
                }

                ClientConfig = loadedConfig;
                IsConfigLoaded = true;

                // Save migrated config back to file
                if (configWasMigrated)
                {
                    SaveConfig(path);
                    ModLogger.Info($"Config migrated and saved to version {ConfigVersioning.CurrentVersion}");
                }

                ModLogger.DebugLog($"Successfully loaded config");
                ValidateConfig();
            }
            catch (JsonException e)
            {
                ModLogger.Error($"Failed to parse config from {path}: {e.Message}. Using default config.", e);
                ClientConfig = new BsConfig();
                IsConfigLoaded = true;
            }
            catch (IOException e)
            {
                ModLogger.Error($"Failed to read config file from {path}: {e.Message}. Using default config.", e);
                ClientConfig = new BsConfig();
                IsConfigLoaded = true;
            }
            catch (UnauthorizedAccessException e)
            {
                ModLogger.Error($"Access denied reading config file from {path}: {e.Message}. Using default config.", e);
                ClientConfig = new BsConfig();
                IsConfigLoaded = true;
            }
            catch (Exception e)
            {
                ModLogger.Error($"Unexpected error loading config from {path}: {e.Message}. Using default config.", e);
                ClientConfig = new BsConfig();
                IsConfigLoaded = true;
            }
        }

        if (!IsConfigLoaded)
        {
            ModLogger.Warning($"Config file {path} not found, using default config.");
            ClientConfig = new BsConfig();
            IsConfigLoaded = true;

            // Create and save default config file to new location
            CreateDefaultConfigFile(path);
        }
    }

    /// <summary>
    /// Creates and saves a default config file to the specified path
    /// </summary>
    /// <param name="configPath">Path where to create the config file</param>
    private static void CreateDefaultConfigFile(string configPath)
    {
        try
        {
            SaveConfig(configPath);
            ModLogger.Info($"Created default config file at {configPath}");
        }
        catch (Exception ex)
        {
            ModLogger.Warning($"Failed to create default config file at {configPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Migrates config backup files from legacy directory to new directory
    /// </summary>
    /// <param name="legacyDir">Legacy config directory path</param>
    /// <param name="newDir">New config directory path</param>
    private static void MigrateBackupFiles(string legacyDir, string newDir)
    {
        try
        {
            var backupFiles = Directory.GetFiles(legacyDir, $"{ConfigBackupPrefix}*.json");
            int migratedCount = 0;

            foreach (var legacyBackupFile in backupFiles)
            {
                var fileName = Path.GetFileName(legacyBackupFile);
                var newBackupFile = Path.Combine(newDir, fileName);

                // Only migrate if destination doesn't exist
                if (!File.Exists(newBackupFile))
                {
                    File.Move(legacyBackupFile, newBackupFile);
                    migratedCount++;
                    ModLogger.DebugLog($"Migrated backup file: {fileName}");
                }
                else
                {
                    // Remove duplicate from legacy location
                    File.Delete(legacyBackupFile);
                    ModLogger.DebugLog($"Removed duplicate backup file from legacy location: {fileName}");
                }
            }

            if (migratedCount > 0)
            {
                ModLogger.Info($"Migrated {migratedCount} config backup files to new location");
            }
        }
        catch (Exception ex)
        {
            ModLogger.Warning($"Failed to migrate backup files: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a backup of the current config file before migration
    /// </summary>
    private static void CreateConfigBackup(string configPath, string fromVersion)
    {
        try
        {
            // Also check backup file size to prevent abuse
            var sourceInfo = new FileInfo(configPath);
            if (sourceInfo.Length > MaxConfigFileSize)
            {
                ModLogger.Warning($"Skipping backup creation - source file too large ({sourceInfo.Length} bytes)");
                return;
            }

            var backupPath = Path.Combine(
                Path.GetDirectoryName(configPath),
                $"{ConfigBackupPrefix}{fromVersion}.json"
            );

            File.Copy(configPath, backupPath, overwrite: true);
            ModLogger.Info($"Created config backup: {Path.GetFileName(backupPath)}");
        }
        catch (Exception e)
        {
            ModLogger.Warning($"Failed to create config backup: {e.Message}");
        }
    }

    /// <summary>
    /// Saves the current config to the default config file location
    /// </summary>
    public static void SaveConfig()
    {
        var configPath = GetConfigFilePath();
        SaveConfig(configPath);
    }

    /// <summary>
    /// Saves the current config to file with size validation
    /// </summary>
    private static void SaveConfig(string path)
    {
        try
        {
            var configJson = JsonConvert.SerializeObject(ClientConfig, Formatting.Indented);

            // Validate serialized config size before writing
            var configBytes = System.Text.Encoding.UTF8.GetByteCount(configJson);
            if (configBytes > MaxConfigFileSize)
            {
                ModLogger.Error($"Generated config is too large ({configBytes} bytes, max {MaxConfigFileSize} bytes). Not saving to prevent abuse.");
                return;
            }

            File.WriteAllText(path, configJson);
            ModLogger.DebugLog($"Config saved successfully ({configBytes} bytes)");
        }
        catch (Exception e)
        {
            ModLogger.Warning($"Failed to save config to {path}: {e.Message}");
        }
    }

    /// <summary>
    /// Validates and corrects configuration values. Saves config if any changes are made.
    /// </summary>
    private static void ValidateConfig()
    {
        bool configChanged = false;

        // Track if any validation methods make changes
        configChanged |= ValidateRangeOption();
        configChanged |= ValidateVersion();

        // Save config if any changes were made during validation
        if (configChanged)
        {
            try
            {
                var configPath = GetConfigFilePath();
                SaveConfig(configPath);
                ModLogger.Info("Config validation made corrections and saved updated config to file.");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to save config after validation corrections: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Validates and corrects the range option.
    /// </summary>
    /// <returns>True if the config was modified, false otherwise</returns>
    private static bool ValidateRangeOption()
    {
        if (ClientConfig.range <= 0.0f && ClientConfig.range != -1.0f)
        {
            ModLogger.Warning($"Invalid range value {ClientConfig.range} in config, resetting to -1.0 (maximum range).");
            ClientConfig.range = -1.0f;
            return true; // Config was modified
        }
        return false; // No changes made
    }

    /// <summary>
    /// Validates and corrects the version field.
    /// </summary>
    /// <returns>True if the config was modified, false otherwise</returns>
    private static bool ValidateVersion()
    {
        if (string.IsNullOrEmpty(ClientConfig.version))
        {
            ModLogger.Warning("Config missing version field, setting to current version");
            ClientConfig.version = ConfigVersioning.CurrentVersion;
            return true; // Config was modified
        }
        return false; // No changes made
    }

#if DEBUG
    private static bool UsingServerConfig()
    {
        // if we don't have a server config don't try and use it
        if (!ServerUtils.HasServerConfig)
        {
            return false;
        }
        // If server skip as the config we're using is the server config
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            return false;
        }
        // If singleplayer use client config, otherwise we're a client on a server
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;
    }

    private static void LogSettingsAccess(string name, bool serverValue, bool clientValue)
    {
        if (IsDebugLogSettingsAccess())
        {
            ModLogger.DebugLog(
                $"Setting ({name}): " +
                $"server {serverValue}; " +
                $"client {clientValue}; " +
                $"usingServer: {UsingServerConfig()}; " +
                $"hasServerConfig: {ServerUtils.HasServerConfig};");
        }
    }
    private static void LogSettingsAccess(string name, float serverValue, float clientValue)
    {
        if (IsDebugLogSettingsAccess())
        {
            ModLogger.DebugLog(
                $"Setting ({name}): " +
                $"server {serverValue}; " +
                $"client {clientValue}; " +
                $"usingServer: {UsingServerConfig()}; " +
                $"hasServerConfig: {ServerUtils.HasServerConfig};");
        }
    }
#endif

    public static float Range()
    {
        float serverValue = ServerConfig.range;
        float clientValue = ClientConfig.range;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool PullFromDrones()
    {
        bool serverValue = ServerConfig.pullFromDrones;
        bool clientValue = ClientConfig.pullFromDrones;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool PullFromDewCollectors()
    {
        bool serverValue = ServerConfig.pullFromDewCollectors;
        bool clientValue = ClientConfig.pullFromDewCollectors;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool PullFromWorkstationOutputs()
    {
        bool serverValue = ServerConfig.pullFromWorkstationOutputs;
        bool clientValue = ClientConfig.pullFromWorkstationOutputs;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool PullFromPlayerCraftedNonCrates()
    {
        bool serverValue = ServerConfig.pullFromPlayerCraftedNonCrates;
        bool clientValue = ClientConfig.pullFromPlayerCraftedNonCrates;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool PullFromVehicleStorage()
    {
        bool serverValue = ServerConfig.pullFromVehicleStorage;
        bool clientValue = ClientConfig.pullFromVehicleStorage;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool IsDebug()
    {
        return IsConfigLoaded && ClientConfig.isDebug;
    }

    public static bool IsDebugLogSettingsAccess()
    {
        return IsConfigLoaded && ClientConfig.isDebug && ClientConfig.isDebugLogSettingsAccess;
    }

    public static bool ServerSyncConfig()
    {
        return ClientConfig.serverSyncConfig;
    }

    /// <summary>
    /// Safely deserialize config JSON with additional error handling
    /// </summary>
    /// <param name="configJson">JSON string to deserialize</param>
    /// <returns>Deserialized BsConfig or null if failed</returns>
    private static BsConfig SafeDeserializeConfig(string configJson)
    {
        try
        {
            // Use JsonConvert with strict settings for security
            var settings = new JsonSerializerSettings
            {
                // Prevent potential security issues
                TypeNameHandling = TypeNameHandling.None,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                // Handle missing properties gracefully
                MissingMemberHandling = MissingMemberHandling.Ignore,
                // Don't crash on unknown properties (for forward compatibility)
                Error = (sender, args) =>
                {
                    ModLogger.Warning($"JSON deserialization warning: {args.ErrorContext.Error.Message}");
                    args.ErrorContext.Handled = true;
                }
            };

            return JsonConvert.DeserializeObject<BsConfig>(configJson, settings);
        }
        catch (JsonException e)
        {
            ModLogger.Error($"JSON deserialization failed: {e.Message}", e);
            return null;
        }
        catch (Exception e)
        {
            ModLogger.Error($"Unexpected error during config deserialization: {e.Message}", e);
            return null;
        }
    }

    /// <summary>
    /// Migrates config files from the legacy Config subdirectory to the mod assembly directory (v2.4.0+)
    /// </summary>
    private static void MigrateConfigLocation()
    {
        var legacyConfigDir = ModPathManager.GetLegacyConfigPath();
        var newConfigDir = ModPathManager.GetConfigPath();

        // If legacy config directory doesn't exist, no migration needed
        if (!Directory.Exists(legacyConfigDir))
        {
            return;
        }

        var legacyConfigFile = GetLegacyConfigFilePath();
        var newConfigFile = GetConfigFilePath();

        try
        {
            // Check if we have a legacy config file to migrate from
            if (File.Exists(legacyConfigFile))
            {
                ModLogger.Info("Migrating config values from legacy Config subdirectory");

                BsConfig legacyConfig = null;
                BsConfig newConfig = null;

                // Load legacy config
                try
                {
                    var legacyConfigJson = File.ReadAllText(legacyConfigFile);
                    legacyConfig = SafeDeserializeConfig(legacyConfigJson);
                    if (legacyConfig == null)
                    {
                        ModLogger.Warning("Failed to load legacy config for migration, using simple file move");
                        if (!File.Exists(newConfigFile))
                        {
                            File.Move(legacyConfigFile, newConfigFile);
                            ModLogger.Info($"Moved legacy config file to {newConfigFile}");
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Warning($"Error reading legacy config for migration: {ex.Message}");
                    return;
                }

                // Load existing new config if it exists
                if (File.Exists(newConfigFile))
                {
                    try
                    {
                        var newConfigJson = File.ReadAllText(newConfigFile);
                        newConfig = SafeDeserializeConfig(newConfigJson);
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Warning($"Error reading existing config during migration: {ex.Message}");
                        newConfig = null;
                    }
                }

                // Create merged config (legacy values take priority unless overridden)
                var mergedConfig = MergeConfigs(legacyConfig, newConfig);

                // Create backup of legacy config before deletion
                CreateConfigBackup(legacyConfigFile, "legacy-migration");

                // Save merged config to new location
                var mergedConfigJson = JsonConvert.SerializeObject(mergedConfig, Formatting.Indented);
                File.WriteAllText(newConfigFile, mergedConfigJson);

                // Remove legacy config file after successful migration
                File.Delete(legacyConfigFile);

                ModLogger.Info($"Successfully migrated config values from legacy location to {newConfigFile}");
            }

            // Migrate all backup files
            MigrateBackupFiles(legacyConfigDir, newConfigDir);

            // Clean up empty legacy config directory
            if (Directory.Exists(legacyConfigDir) && !Directory.EnumerateFileSystemEntries(legacyConfigDir).Any())
            {
                Directory.Delete(legacyConfigDir);
                ModLogger.Info("Removed empty legacy Config directory");
            }
        }
        catch (Exception ex)
        {
            ModLogger.Warning($"Failed to migrate config files from legacy location: {ex.Message}");
        }
    }

    /// <summary>
    /// Merges legacy config with new config, prioritizing legacy values unless special overrides apply
    /// </summary>
    /// <param name="legacyConfig">Config from legacy location</param>
    /// <param name="newConfig">Config from new location (can be null)</param>
    /// <returns>Merged configuration</returns>
    private static BsConfig MergeConfigs(BsConfig legacyConfig, BsConfig newConfig)
    {
        // Start with legacy config as base
        var mergedConfig = new BsConfig
        {
            version = ConfigVersioning.CurrentVersion, // Always use current version
            range = legacyConfig.range,
            pullFromDrones = legacyConfig.pullFromDrones,
            pullFromDewCollectors = legacyConfig.pullFromDewCollectors,
            pullFromWorkstationOutputs = legacyConfig.pullFromWorkstationOutputs,
            pullFromPlayerCraftedNonCrates = legacyConfig.pullFromPlayerCraftedNonCrates,
            pullFromVehicleStorage = legacyConfig.pullFromVehicleStorage,
            serverSyncConfig = legacyConfig.serverSyncConfig,
            isDebug = legacyConfig.isDebug,
            isDebugLogSettingsAccess = legacyConfig.isDebugLogSettingsAccess
        };

        // Apply special migration overrides here
        ApplyMigrationOverrides(mergedConfig, legacyConfig, newConfig);

        // If no legacy value exists but new config has a value, use new config value
        // (This handles cases where new properties were added)
        if (newConfig != null)
        {
            // Example of fallback logic for new properties that might not exist in legacy
            // Add specific property checks here as needed for future properties

            // For now, legacy takes complete priority unless overridden above
            // Future properties can be handled with null checks and fallbacks
        }

        return mergedConfig;
    }

    /// <summary>
    /// Applies special override rules during config migration for version-specific changes
    /// </summary>
    /// <param name="mergedConfig">The config being built</param>
    /// <param name="legacyConfig">Original legacy config</param>
    /// <param name="newConfig">New location config (can be null)</param>
    private static void ApplyMigrationOverrides(BsConfig mergedConfig, BsConfig legacyConfig, BsConfig newConfig)
    {
        // Add version-specific override logic here
        // Example:
        // if (ShouldOverrideForVersion("2.4.0"))
        // {
        //     mergedConfig.someNewSetting = defaultValue;
        //     ModLogger.Info("Applied migration override for someNewSetting");
        // }

        // Example: Reset debug settings for security in certain versions
        // if (ShouldOverrideForVersion("2.5.0"))
        // {
        //     if (legacyConfig.isDebug)
        //     {
        //         mergedConfig.isDebug = false;
        //         ModLogger.Info("Reset debug mode during migration for security");
        //     }
        // }
    }

    /// <summary>
    /// Helper method to determine if a specific override should be applied based on version
    /// </summary>
    /// <param name="targetVersion">Version to check for override rules</param>
    /// <returns>True if override should be applied</returns>
    private static bool ShouldOverrideForVersion(string targetVersion)
    {
        // Implement version comparison logic as needed
        // This is a placeholder for when you need version-specific overrides
        return false;
    }
}