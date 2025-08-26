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
    public bool PullFromPlayerCraftedNonCrates { get; }
    public bool PullFromVehicleStorage { get; }

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
        PullFromPlayerCraftedNonCrates = ModConfig.PullFromPlayerCraftedNonCrates();
        PullFromVehicleStorage = ModConfig.PullFromVehicleStorage();

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