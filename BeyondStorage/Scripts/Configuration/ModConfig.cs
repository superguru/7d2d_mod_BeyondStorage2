using System.IO;
using BeyondStorage.Scripts.Server;
using BeyondStorage.Scripts.Utils;
using Newtonsoft.Json;

namespace BeyondStorage.Scripts.Configuration;

public static class ModConfig
{
    private const string ConfigFileName = "config.json";
    public static BsConfig ClientConfig { get; private set; }
    public static BsConfig ServerConfig { get; } = new();

    public static void LoadConfig(BeyondStorage context)
    {
        var path = Path.Combine(FileUtil.GetConfigPath(true), ConfigFileName);
        LogUtil.DebugLog($"Loading config from {path}");

        bool config_loaded = false;

        if (File.Exists(path))
        {
            try
            {
                ClientConfig = JsonConvert.DeserializeObject<BsConfig>(File.ReadAllText(path));
                config_loaded = true;
                LogUtil.DebugLog($"Loaded config: {JsonConvert.SerializeObject(ClientConfig, Formatting.Indented)}");

                ValidateConfig();
            }
            catch (JsonException e)
            {
                LogUtil.Error($"Failed to load config from {path}: {e.Message}");
            }
        }

        if (!config_loaded)
        {
            LogUtil.Warning($"Config file {path} not found or invalid, using default config.");
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
        if (ClientConfig.range <= 0.0f)
        {
            LogUtil.Warning($"Invalid range value {ClientConfig.range} in config, resetting to -1.0f (infinite range).");
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
#endif
    public static bool EnableForBlockRepair()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog(
                $"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.enableForBlockRepair}; server {ServerConfig.enableForBlockRepair}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.enableForBlockRepair : ClientConfig.enableForBlockRepair;
    }

    public static bool EnableForBlockUpgrade()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog(
                $"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.enableForBlockUpgrade}; server {ServerConfig.enableForBlockUpgrade}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.enableForBlockUpgrade : ClientConfig.enableForBlockUpgrade;
    }

    public static bool EnableForGeneratorRefuel()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog(
                $"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.enableForGeneratorRefuel}; server {ServerConfig.enableForGeneratorRefuel}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.enableForGeneratorRefuel : ClientConfig.enableForGeneratorRefuel;
    }

    public static bool EnableForItemRepair()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog(
                $"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.enableForItemRepair}; server {ServerConfig.enableForItemRepair}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.enableForItemRepair : ClientConfig.enableForItemRepair;
    }

    public static bool EnableForReload()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog($"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.enableForReload}; server {ServerConfig.enableForReload}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.enableForReload : ClientConfig.enableForReload;
    }

    public static bool EnableForVehicleRefuel()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog(
                $"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.enableForVehicleRefuel}; server {ServerConfig.enableForVehicleRefuel}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.enableForVehicleRefuel : ClientConfig.enableForVehicleRefuel;
    }

    public static bool EnableForVehicleRepair()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog(
                $"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.enableForVehicleRepair}; server {ServerConfig.enableForVehicleRepair}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.enableForVehicleRepair : ClientConfig.enableForVehicleRepair;
    }

    public static bool IsDebug()
    {
        return ClientConfig.isDebug;
    }

    public static bool OnlyStorageCrates()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog(
                $"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.onlyStorageCrates}; server {ServerConfig.onlyStorageCrates}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.onlyStorageCrates : ClientConfig.onlyStorageCrates;
    }

    public static bool PullFromVehicleStorage()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog(
                $"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.pullFromVehicleStorage}; server {ServerConfig.pullFromVehicleStorage}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.pullFromVehicleStorage : ClientConfig.pullFromVehicleStorage;
    }

    public static bool ServerSyncConfig()
    {
        return ClientConfig.serverSyncConfig;
    }

    public static float Range()
    {
#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog($"using server config: {ServerUtils.HasServerConfig}; usingServer {UsingServerConfig()}; client {ClientConfig.range}; server {ServerConfig.range}");
        }
#endif
        return ServerUtils.HasServerConfig ? ServerConfig.range : ClientConfig.range;
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

        // if set to true it will try and pull items from nearby vehicle storages
        public bool pullFromVehicleStorage = true;

        // ========== Functionality =========
        // if set true nearby containers will be used for block repairs
        public bool enableForBlockRepair = true;

        // if set true nearby containers will be used for block upgrades
        public bool enableForBlockUpgrade = true;

        // if set true will allow refueling generators
        public bool enableForGeneratorRefuel = true;

        // if set true nearby containers will be used for item repairs
        // disable if you experience lag
        public bool enableForItemRepair = true;

        // if set true nearby containers will be used for gun reloading
        public bool enableForReload = true;

        // if set true will allow refueling vehicles from nearby storage
        public bool enableForVehicleRefuel = true;

        // if set true will allow repairing vehicles from nearby storage
        public bool enableForVehicleRepair = true;

        // ========== Multiplayer =========
        // if set true on a server it will force all clients to use server settings for Beyond Storage
        public bool serverSyncConfig = true;

        // ========== Housekeeping =========
        // if set true additional logging will be printed to logs/console
        public bool isDebug = false;
    }
}