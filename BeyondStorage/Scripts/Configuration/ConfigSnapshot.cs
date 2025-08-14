using Newtonsoft.Json;

namespace BeyondStorage.Scripts.Configuration;

/// <summary>
/// Configuration snapshot that captures all relevant settings at a single point in time
/// to ensure consistency throughout method execution.
/// </summary>
public sealed class ConfigSnapshot
{
    // ========== Source selection / eligibility =========
    public float Range { get; }
    public bool PullFromDrones { get; }
    public bool PullFromDewCollectors { get; }
    public bool PullFromWorkstationOutputs { get; }
    public bool OnlyStorageCrates { get; }
    public bool PullFromVehicleStorage { get; }

    // ========== Functionality =========
    public bool EnableForBlockRepair { get; }
    public bool EnableForBlockTexture { get; }
    public bool EnableForBlockUpgrade { get; }
    public bool EnableForGeneratorRefuel { get; }
    public bool EnableForItemRepair { get; }
    public bool EnableForReload { get; }
    public bool EnableForVehicleRefuel { get; }
    public bool EnableForVehicleRepair { get; }

    // ========== Multiplayer =========
    public bool ServerSyncConfig { get; }

    // ========== Housekeeping =========
    public bool IsDebug { get; }
    public bool IsDebugLogSettingsAccess { get; }

    private ConfigSnapshot()
    {
        // ========== Source selection / eligibility =========
        Range = ModConfig.Range();
        PullFromDrones = ModConfig.PullFromDrones();
        PullFromDewCollectors = ModConfig.PullFromDewCollectors();
        PullFromWorkstationOutputs = ModConfig.PullFromWorkstationOutputs();
        OnlyStorageCrates = ModConfig.OnlyStorageCrates();
        PullFromVehicleStorage = ModConfig.PullFromVehicleStorage();

        // ========== Functionality =========
        EnableForBlockRepair = ModConfig.EnableForBlockRepair();
        EnableForBlockTexture = ModConfig.EnableForBlockTexture();
        EnableForBlockUpgrade = ModConfig.EnableForBlockUpgrade();
        EnableForGeneratorRefuel = ModConfig.EnableForGeneratorRefuel();
        EnableForItemRepair = ModConfig.EnableForItemRepair();
        EnableForReload = ModConfig.EnableForReload();
        EnableForVehicleRefuel = ModConfig.EnableForVehicleRefuel();
        EnableForVehicleRepair = ModConfig.EnableForVehicleRepair();

        // ========== Multiplayer =========
        ServerSyncConfig = ModConfig.ServerSyncConfig();

        // ========== Housekeeping =========
        IsDebug = ModConfig.IsDebug();
        IsDebugLogSettingsAccess = ModConfig.IsDebugLogSettingsAccess();
    }

    public static ConfigSnapshot Current => new();

    /// <summary>
    /// Returns a pretty-printed JSON representation of all configuration options as a flat list.
    /// </summary>
    /// <returns>Formatted JSON string containing all configuration attributes</returns>
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}