using System;
using System.IO;
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

    public static void LoadConfig(BeyondStorage context)
    {
        var path = Path.Combine(ModPathManager.GetConfigPath(true), ConfigFileName);
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

    private static void ValidateConfig()
    {
        ValidateRangeOption();
        ValidateVersion();
    }

    private static void ValidateRangeOption()
    {
        if (ClientConfig.range <= 0.0f && ClientConfig.range != -1.0f)
        {
            ModLogger.Warning($"Invalid range value {ClientConfig.range} in config, resetting to -1.0 (maximum range).");
            ClientConfig.range = -1.0f;
        }
    }

    private static void ValidateVersion()
    {
        if (string.IsNullOrEmpty(ClientConfig.version))
        {
            ModLogger.Warning("Config missing version field, setting to current version");
            ClientConfig.version = ConfigVersioning.CurrentVersion;
        }
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
}