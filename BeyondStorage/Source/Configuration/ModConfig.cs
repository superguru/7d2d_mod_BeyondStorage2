using System;
using System.IO;
using System.Linq;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.Multiplayer;
using Newtonsoft.Json;

namespace BeyondStorage.Source.Configuration;

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

    public static void LoadConfig()
    {
        // Reset loaded state so reload calls correctly track state
        IsConfigLoaded = false;

        MigrateConfigLocation();

        var path = GetConfigFilePath();
        ModLogger.Info($"Loading config from {path}");

        if (File.Exists(path))
        {
            LoadExistingConfig(path);
        }
        else
        {
            LoadDefaultConfig(path);
        }
    }

    /// <summary>
    /// Loads configuration from an existing config file
    /// </summary>
    /// <param name="path">Path to the config file</param>
    private static void LoadExistingConfig(string path)
    {
        try
        {
            if (!ValidateConfigFileSize(path))
            {
                SetDefaultConfigAndMarkLoaded();
                return;
            }

            var configJson = ReadConfigFile(path);
            if (configJson == null)
            {
                SetDefaultConfigAndMarkLoaded();
                return;
            }

            var loadedConfig = LoadAndMigrateConfig(path, configJson);
            if (loadedConfig == null)
            {
                SetDefaultConfigAndMarkLoaded();
                return;
            }

            FinalizeConfigLoad(loadedConfig);
        }
        catch (JsonException e)
        {
            ModLogger.Error($"Failed to parse config from {path}: {e.Message}. Using default config.", e);
            SetDefaultConfigAndMarkLoaded();
        }
        catch (IOException e)
        {
            ModLogger.Error($"Failed to read config file from {path}: {e.Message}. Using default config.", e);
            SetDefaultConfigAndMarkLoaded();
        }
        catch (UnauthorizedAccessException e)
        {
            ModLogger.Error($"Access denied reading config file from {path}: {e.Message}. Using default config.", e);
            SetDefaultConfigAndMarkLoaded();
        }
        catch (Exception e)
        {
            ModLogger.Error($"Unexpected error loading config from {path}: {e.Message}. Using default config.", e);
            SetDefaultConfigAndMarkLoaded();
        }
    }

    /// <summary>
    /// Loads default configuration when no config file exists
    /// </summary>
    /// <param name="path">Path where to create the config file</param>
    private static void LoadDefaultConfig(string path)
    {
        ModLogger.Warning($"Config file {path} not found, using default config.");
        SetDefaultConfigAndMarkLoaded();
        CreateDefaultConfigFile(path);
    }

    /// <summary>
    /// Validates that the config file size is within acceptable limits
    /// </summary>
    /// <param name="path">Path to the config file</param>
    /// <returns>True if file size is valid, false otherwise</returns>
    private static bool ValidateConfigFileSize(string path)
    {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaxConfigFileSize)
        {
            ModLogger.Error($"Config file is too large ({fileInfo.Length} bytes, max {MaxConfigFileSize} bytes). Using default config to prevent abuse.");
            return false;
        }

        ModLogger.DebugLog($"Config file size: {fileInfo.Length} bytes (within {MaxConfigFileSize} byte limit)");
        return true;
    }

    /// <summary>
    /// Reads and validates config file content
    /// </summary>
    /// <param name="path">Path to the config file</param>
    /// <returns>Config JSON string, or null if invalid</returns>
    private static string ReadConfigFile(string path)
    {
        string configJson;
        using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (var reader = new StreamReader(fileStream))
        {
            var buffer = new char[MaxConfigFileSize];
            var charsRead = reader.Read(buffer, 0, buffer.Length);

            if (charsRead == buffer.Length && !reader.EndOfStream)
            {
                ModLogger.Error($"Config file content exceeds {MaxConfigFileSize} bytes. Truncated and using default config to prevent abuse.");
                return null;
            }

            configJson = new string(buffer, 0, charsRead);
        }

        if (string.IsNullOrWhiteSpace(configJson))
        {
            ModLogger.Warning("Config file is empty or contains only whitespace. Using default config.");
            return null;
        }

        return configJson;
    }

    /// <summary>
    /// Loads config from JSON and applies migrations if necessary
    /// </summary>
    /// <param name="path">Path to the config file</param>
    /// <param name="configJson">Raw JSON content</param>
    /// <returns>Loaded and migrated config, or null if failed</returns>
    private static BsConfig LoadAndMigrateConfig(string path, string configJson)
    {
        if (ConfigVersioning.IsLegacyConfig(configJson))
        {
            return LoadLegacyConfig(path, configJson);
        }

        return LoadVersionedConfig(path, configJson);
    }

    /// <summary>
    /// Loads and migrates a legacy config (pre-2.3.0)
    /// </summary>
    /// <param name="path">Path to the config file</param>
    /// <param name="configJson">Raw JSON content</param>
    /// <returns>Migrated config</returns>
    private static BsConfig LoadLegacyConfig(string path, string configJson)
    {
        ModLogger.Info("Detected legacy config file, migrating to versioned format");
        CreateConfigBackup(path, "legacy");

        var loadedConfig = ConfigVersioning.MigrateLegacyConfig(configJson);

        // Set ClientConfig before saving so SaveConfig has something to serialize
        ClientConfig = loadedConfig;
        SaveConfigAfterMigration(path);

        return loadedConfig;
    }

    /// <summary>
    /// Loads a versioned config and migrates if version doesn't match current
    /// </summary>
    /// <param name="path">Path to the config file</param>
    /// <param name="configJson">Raw JSON content</param>
    /// <returns>Loaded and migrated config, or null if failed</returns>
    private static BsConfig LoadVersionedConfig(string path, string configJson)
    {
        var loadedConfig = SafeDeserializeConfig(configJson);
        if (loadedConfig == null)
        {
            ModLogger.Error("Failed to deserialize config JSON. Using default config.");
            return null;
        }

        if (loadedConfig.version != ConfigVersioning.CurrentVersion)
        {
            CreateConfigBackup(path, loadedConfig.version);
            loadedConfig = ConfigVersioning.MigrateVersionedConfig(loadedConfig);

            // Set ClientConfig before saving so SaveConfig has something to serialize
            ClientConfig = loadedConfig;
            SaveConfigAfterMigration(path);
        }

        return loadedConfig;
    }

    /// <summary>
    /// Saves config after migration and logs success
    /// </summary>
    /// <param name="path">Path to save the config file</param>
    private static void SaveConfigAfterMigration(string path)
    {
        SaveConfig(path);
        ModLogger.Info($"Config migrated and saved to version {ConfigVersioning.CurrentVersion}");
    }

    /// <summary>
    /// Finalizes config loading by setting ClientConfig and validating
    /// </summary>
    /// <param name="loadedConfig">The config to finalize</param>
    private static void FinalizeConfigLoad(BsConfig loadedConfig)
    {
        ClientConfig = loadedConfig;
        IsConfigLoaded = true;
        ModLogger.DebugLog($"Successfully loaded config");
        ValidateConfig();
    }

    /// <summary>
    /// Sets default config and marks it as loaded
    /// </summary>
    private static void SetDefaultConfigAndMarkLoaded()
    {
        ClientConfig = new BsConfig();
        IsConfigLoaded = true;
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

    public static bool PullFromDrones()
    {
        bool serverValue = ServerConfig.pullFromDrones;
        bool clientValue = ClientConfig.pullFromDrones;

        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool PullFromCollectors()
    {
        bool serverValue = ServerConfig.pullFromCollectors;
        bool clientValue = ClientConfig.pullFromCollectors;

        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool PullFromWorkstationOutputs()
    {
        bool serverValue = ServerConfig.pullFromWorkstationOutputs;
        bool clientValue = ClientConfig.pullFromWorkstationOutputs;

        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool PullFromVehicleStorage()
    {
        bool serverValue = ServerConfig.pullFromVehicleStorage;
        bool clientValue = ClientConfig.pullFromVehicleStorage;

        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool IsDebug()
    {
        return IsConfigLoaded && ClientConfig.isDebug;
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
            // Rename fields for configs created before field renames were introduced
            configJson = ConfigVersioning.PreprocessConfigJson(configJson);

            // Use JsonConvert with strict settings for security
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
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
                MigrateLegacyConfigFile(legacyConfigFile, newConfigFile);
            }

            // Migrate all backup files
            MigrateBackupFiles(legacyConfigDir, newConfigDir);

            // Clean up empty legacy config directory
            CleanupLegacyDirectory(legacyConfigDir);
        }
        catch (Exception ex)
        {
            ModLogger.Warning($"Failed to migrate config files from legacy location: {ex.Message}");
        }
    }

    /// <summary>
    /// Migrates a single legacy config file to the new location
    /// </summary>
    /// <param name="legacyConfigFile">Path to legacy config file</param>
    /// <param name="newConfigFile">Path to new config file location</param>
    private static void MigrateLegacyConfigFile(string legacyConfigFile, string newConfigFile)
    {
        ModLogger.Info("Migrating config values from legacy Config subdirectory");

        var legacyConfig = LoadLegacyConfigForMigration(legacyConfigFile);
        if (legacyConfig == null)
        {
            // Simple file move if config can't be parsed
            if (!File.Exists(newConfigFile))
            {
                File.Move(legacyConfigFile, newConfigFile);
                ModLogger.Info($"Moved legacy config file to {newConfigFile}");
            }
            return;
        }

        var newConfig = LoadExistingNewConfig(newConfigFile);
        var mergedConfig = MergeConfigs(legacyConfig, newConfig);

        // Create backup of legacy config before deletion
        CreateConfigBackup(legacyConfigFile, "legacy-migration");

        // Save merged config to new location
        SaveMergedConfig(mergedConfig, newConfigFile);

        // Remove legacy config file after successful migration
        File.Delete(legacyConfigFile);

        ModLogger.Info($"Successfully migrated config values from legacy location to {newConfigFile}");
    }

    /// <summary>
    /// Loads legacy config for migration purposes
    /// </summary>
    /// <param name="legacyConfigFile">Path to legacy config file</param>
    /// <returns>Loaded config or null if failed</returns>
    private static BsConfig LoadLegacyConfigForMigration(string legacyConfigFile)
    {
        try
        {
            if (!ValidateConfigFileSize(legacyConfigFile))
            {
                ModLogger.Warning("Legacy config file is too large to migrate safely, using simple file move.");
                return null;
            }

            var legacyConfigJson = ReadConfigFile(legacyConfigFile);
            if (legacyConfigJson == null)
            {
                ModLogger.Warning("Failed to load legacy config for migration, using simple file move");
                return null;
            }

            var legacyConfig = SafeDeserializeConfig(legacyConfigJson);
            if (legacyConfig == null)
            {
                ModLogger.Warning("Failed to load legacy config for migration, using simple file move");
            }

            return legacyConfig;
        }
        catch (Exception ex)
        {
            ModLogger.Warning($"Error reading legacy config for migration: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads existing config from new location if it exists
    /// </summary>
    /// <param name="newConfigFile">Path to new config file</param>
    /// <returns>Loaded config or null if doesn't exist or failed</returns>
    private static BsConfig LoadExistingNewConfig(string newConfigFile)
    {
        if (!File.Exists(newConfigFile))
        {
            return null;
        }

        try
        {
            if (!ValidateConfigFileSize(newConfigFile))
            {
                ModLogger.Warning("Existing config at new location is too large to read safely during migration.");
                return null;
            }

            var newConfigJson = ReadConfigFile(newConfigFile);
            if (newConfigJson == null)
            {
                return null;
            }

            return SafeDeserializeConfig(newConfigJson);
        }
        catch (Exception ex)
        {
            ModLogger.Warning($"Error reading existing config during migration: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves merged config to the new location with size validation
    /// </summary>
    /// <param name="mergedConfig">Config to save</param>
    /// <param name="newConfigFile">Path to save the config</param>
    private static void SaveMergedConfig(BsConfig mergedConfig, string newConfigFile)
    {
        // Temporarily set ClientConfig so SaveConfig can serialize it
        ClientConfig = mergedConfig;
        SaveConfig(newConfigFile);
    }

    /// <summary>
    /// Cleans up empty legacy config directory
    /// </summary>
    /// <param name="legacyConfigDir">Path to legacy config directory</param>
    private static void CleanupLegacyDirectory(string legacyConfigDir)
    {
        if (Directory.Exists(legacyConfigDir) && !Directory.EnumerateFileSystemEntries(legacyConfigDir).Any())
        {
            Directory.Delete(legacyConfigDir);
            ModLogger.Info("Removed empty legacy Config directory");
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
            version = ConfigVersioning.CurrentVersion,
            pullFromDrones = legacyConfig.pullFromDrones,
            pullFromCollectors = legacyConfig.pullFromCollectors,
            pullFromWorkstationOutputs = legacyConfig.pullFromWorkstationOutputs,
            pullFromVehicleStorage = legacyConfig.pullFromVehicleStorage,
            serverSyncConfig = legacyConfig.serverSyncConfig,
            isDebug = legacyConfig.isDebug,
            metaDescription = legacyConfig.metaDescription
        };

        // For now, legacy takes complete priority
        // Future properties can be handled with null checks and fallbacks against newConfig

        return mergedConfig;
    }
}