using BeyondStorage.Scripts.Configuration;

namespace BeyondStorage.Scripts.ContainerLogic;

/// <summary>
/// Configuration snapshot that captures all relevant settings at a single point in time
/// to ensure consistency throughout method execution.
/// </summary>
public sealed class ConfigSnapshot
{
    public bool PullFromDewCollectors { get; }
    public bool PullFromWorkstationOutputs { get; }
    public bool PullFromVehicleStorage { get; }
    public bool OnlyStorageCrates { get; }
    public float Range { get; }

    private ConfigSnapshot()
    {
        PullFromDewCollectors = ModConfig.PullFromDewCollectors();
        PullFromWorkstationOutputs = ModConfig.PullFromWorkstationOutputs();
        PullFromVehicleStorage = ModConfig.PullFromVehicleStorage();
        OnlyStorageCrates = ModConfig.OnlyStorageCrates();
        Range = ModConfig.Range();
    }

    public static ConfigSnapshot Current => new ConfigSnapshot();
}