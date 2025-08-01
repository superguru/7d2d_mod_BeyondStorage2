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
                ClientConfig = JsonConvert.DeserializeObject<BsConfig>(File.ReadAllText(path));
                IsConfigLoaded = true;
                ModLogger.DebugLog($"Loaded config: {JsonConvert.SerializeObject(ClientConfig, Formatting.Indented)}");

                ValidateConfig();
            }
            catch (JsonException e)
            {
                ModLogger.Error($"Failed to load config from {path}: {e.Message}");
            }
        }

        if (!IsConfigLoaded)
        {
            ModLogger.Warning($"Config file {path} not found or invalid, using default config.");
            ClientConfig = new BsConfig();

            // No need to write the default config back to file, as the config file is packaged with the mod as of 2.0.1.
            // If we reach this point, it means the config file was either deleted or corrupted, but that is not our concern, we can just work on dfaults.
        }
    }

    private static void ValidateConfig()
    {
        ValidateRangeOption();
    }

    private static void ValidateRangeOption()
    {
        if (ClientConfig.range <= 0.0f && ClientConfig.range != -1.0f)
        {
            ModLogger.Warning($"Invalid range value {ClientConfig.range} in config, resetting to -1.0 (maximum range).");
            ClientConfig.range = -1.0f;
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

    public static bool OnlyStorageCrates()
    {
        bool serverValue = ServerConfig.onlyStorageCrates;
        bool clientValue = ClientConfig.onlyStorageCrates;
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

    public static bool PullFromVehicleStorage()
    {
        bool serverValue = ServerConfig.pullFromVehicleStorage;
        bool clientValue = ClientConfig.pullFromVehicleStorage;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool EnableForBlockRepair()
    {
        bool serverValue = ServerConfig.enableForBlockRepair;
        bool clientValue = ClientConfig.enableForBlockRepair;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool EnableForBlockTexture()
    {
        bool serverValue = ServerConfig.enableForBlockTexture;
        bool clientValue = ClientConfig.enableForBlockTexture;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool EnableForBlockUpgrade()
    {
        bool serverValue = ServerConfig.enableForBlockUpgrade;
        bool clientValue = ClientConfig.enableForBlockUpgrade;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool EnableForGeneratorRefuel()
    {
        bool serverValue = ServerConfig.enableForGeneratorRefuel;
        bool clientValue = ClientConfig.enableForGeneratorRefuel;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool EnableForItemRepair()
    {
        bool serverValue = ServerConfig.enableForItemRepair;
        bool clientValue = ClientConfig.enableForItemRepair;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool EnableForReload()
    {
        bool serverValue = ServerConfig.enableForReload;
        bool clientValue = ClientConfig.enableForReload;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool EnableForVehicleRefuel()
    {
        bool serverValue = ServerConfig.enableForVehicleRefuel;
        bool clientValue = ClientConfig.enableForVehicleRefuel;
#if DEBUG
        LogSettingsAccess(MethodBase.GetCurrentMethod().Name, serverValue, clientValue);
#endif
        return ServerUtils.HasServerConfig ? serverValue : clientValue;
    }

    public static bool EnableForVehicleRepair()
    {
        bool serverValue = ServerConfig.enableForVehicleRepair;
        bool clientValue = ClientConfig.enableForVehicleRepair;
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

    // TODO: Update NetPackageBeyondStorageConfig if new settings added and should be synced between server/client
    public class BsConfig
    {
        // ========== Source selection / eligibility =========
        // How far to pull from (-1 is infinite range, only limited by chunks loaded)
        public float range = -1.0f;

        // if set to true it will ignore tile entities that aren't Storage Containers (crates)
        // otherwise will check all lootable containers placed by player(s)
        public bool onlyStorageCrates = false;

        // if set to true it will try and pull items from nearby dew collectors
        public bool pullFromDewCollectors = true;

        // if set to true it will try and pull items from nearby vehicle storages
        public bool pullFromVehicleStorage = true;

        // if set to true it will try and pull items from nearby workstation output stacks
        public bool pullFromWorkstationOutputs = true;

        // ========== Functionality =========
        // if set true will allow block repairs
        public bool enableForBlockRepair = true;

        // if set true will allow block textures (painting)
        public bool enableForBlockTexture = true;

        // if set true will allow block upgrades
        public bool enableForBlockUpgrade = true;

        // if set true will allow refueling generators
        public bool enableForGeneratorRefuel = true;

        // if set true will allow item repairs
        public bool enableForItemRepair = true;

        // if set true will allow gun reloading
        public bool enableForReload = true;

        // if set true will allow refueling vehicles
        public bool enableForVehicleRefuel = true;

        // if set true will allow repairing vehicles
        public bool enableForVehicleRepair = true;

        // ========== Multiplayer =========
        // if set true on a server it will force all clients to use server settings for Beyond Storage
        public bool serverSyncConfig = true;

        // ========== Housekeeping =========
        // if set true additional logging will be printed to logs/console
        public bool isDebug = false;
        public bool isDebugLogSettingsAccess = false;  // This will only work if isDebug is true and DEBUG is defined
    }
}