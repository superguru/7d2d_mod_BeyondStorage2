using Newtonsoft.Json;

namespace BeyondStorage.Configuration;

/// <summary>
/// Configuration snapshot that captures all relevant settings at a single point in time
/// to ensure consistency throughout method execution.
/// </summary>
public sealed class ConfigSnapshot
{
    // ========== Source selection / eligibility =========
    public float Range { get; }
    public bool ConsumeFromDrones { get; }
    public bool ConsumeFromVehicles { get; }

    // ========== Multiplayer =========
    public bool ServerSyncConfig { get; }

    // ========== Housekeeping =========
    public bool IsDebug { get; }

    private ConfigSnapshot()
    {
        // ========== Source selection / eligibility =========
        Range = ModConfig.Range();
        ConsumeFromDrones = ModConfig.ConsumeFromDrones();
        ConsumeFromVehicles = ModConfig.ConsumeFromVehicles();

        // ========== Multiplayer =========
        ServerSyncConfig = ModConfig.ServerSyncConfig();

        // ========== Housekeeping =========
        IsDebug = ModConfig.IsDebug();
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