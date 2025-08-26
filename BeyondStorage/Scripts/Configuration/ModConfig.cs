using System;
using System.IO;
using BeyondStorage.Scripts.Multiplayer;
using BeyondStorage.Scripts.Infrastructure;
using Newtonsoft.Json;

#if DEBUG
using System.Reflection;
#endif

namespace BeyondStorage.Scripts.Configuration;

public static class ModConfig
{
    private const string ConfigFileName = "config.json";
    private const string ConfigBackupPrefix = "config.backup.";

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
                var configJson = File.ReadAllText(path);
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
                    // Load versioned config
                    loadedConfig = JsonConvert.DeserializeObject<BsConfig>(configJson);

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

                ModLogger.DebugLog($"Loaded config: {JsonConvert.SerializeObject(ClientConfig, Formatting.Indented)}");
                ValidateConfig();
            }
            catch (JsonException e)
            {
                ModLogger.Error($"Failed to load config from {path}: {e.Message}", e);
            }
            catch (Exception e)
            {
                ModLogger.Error($"Unexpected error loading config from {path}: {e.Message}", e);
            }
        }

        if (!IsConfigLoaded)
        {
            ModLogger.Warning($"Config file {path} not found or invalid, using default config.");
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
    /// Saves the current config to file
    /// </summary>
    private static void SaveConfig(string path)
    {
        try
        {
            var configJson = JsonConvert.SerializeObject(ClientConfig, Formatting.Indented);
            File.WriteAllText(path, configJson);
            ModLogger.DebugLog("Config saved successfully");
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
}