namespace BeyondStorage.Scripts.Configuration;

/// <summary>
/// Configuration snapshot that captures all relevant settings at a single point in time
/// to ensure consistency throughout method execution.
/// </summary>
public sealed class ConfigSnapshot
{
    public bool PullFromDrones { get; }
    public bool PullFromDewCollectors { get; }
    public bool PullFromWorkstationOutputs { get; }
    public bool OnlyStorageCrates { get; }
    public bool PullFromVehicleStorage { get; }
    public float Range { get; }

    private ConfigSnapshot()
    {
        PullFromDrones = ModConfig.PullFromDrones();
        PullFromDewCollectors = ModConfig.PullFromDewCollectors();
        PullFromWorkstationOutputs = ModConfig.PullFromWorkstationOutputs();
        OnlyStorageCrates = ModConfig.OnlyStorageCrates();
        PullFromVehicleStorage = ModConfig.PullFromVehicleStorage();

        Range = ModConfig.Range();
    }

    public static ConfigSnapshot Current => new();
}