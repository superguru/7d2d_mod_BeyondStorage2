using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public sealed class BatchRemovalContext
{
    public ConfigSnapshot Config { get; }

    public List<TileEntityDewCollector> DewCollectors { get; }
    public List<ITileEntityLootable> Lootables { get; }
    public List<EntityVehicle> Vehicles { get; }
    public List<TileEntityWorkstation> Workstations { get; }
    public DateTime CreatedAt { get; }
    internal BatchRemovalContext()
    {
        Config = ConfigSnapshot.Current;

        ContainerUtils.DiscoverTileEntitySources(Config, out var dewCollectors, out var workstations, out var lootables);

        DewCollectors = dewCollectors;
        Workstations = workstations;
        Lootables = lootables;
        Vehicles = VehicleUtils.GetAvailableVehicleStorages(Config);

        CreatedAt = DateTime.Now;

        LogUtil.DebugLog($"BatchRemovalContext created: {Lootables.Count} lootables, {DewCollectors.Count} dew collectors, {Workstations.Count} workstations");
    }

    public double AgeInSeconds => (DateTime.Now - CreatedAt).TotalSeconds;

    public bool HasExpired(double lifetimeSeconds) => AgeInSeconds > lifetimeSeconds;

    public string GetSourceSummary()
    {
        return $"Lootables: {Lootables.Count}, DewCollectors: {DewCollectors.Count}, Workstations: {Workstations.Count}, Age: {AgeInSeconds:F1}s";
    }
}