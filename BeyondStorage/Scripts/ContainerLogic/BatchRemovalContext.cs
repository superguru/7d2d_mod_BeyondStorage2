using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public sealed class BatchRemovalContext
{
    public ConfigSnapshot Config { get; }
    public WorldPlayerContext WorldPlayerContext { get; }

    public List<TileEntityDewCollector> DewCollectors { get; private set; }
    public List<ITileEntityLootable> Lootables { get; private set; }
    public List<EntityVehicle> Vehicles { get; private set; }
    public List<TileEntityWorkstation> Workstations { get; private set; }
    public DateTime CreatedAt { get; }

    internal BatchRemovalContext()
    {
        Config = ConfigSnapshot.Current;

        // Create WorldPlayerContext first - if this fails, the entire context creation should fail
        WorldPlayerContext = WorldPlayerContext.TryCreate(nameof(BatchRemovalContext));
        if (WorldPlayerContext == null)
        {
            LogUtil.Error($"{nameof(BatchRemovalContext)}: Failed to create WorldPlayerContext, aborting context creation.");
            // In this case, you might want to throw an exception or return null
            // For now, we'll continue but the collections will remain empty
            DewCollectors = new List<TileEntityDewCollector>(0);
            Workstations = new List<TileEntityWorkstation>(0);
            Lootables = new List<ITileEntityLootable>(0);
            Vehicles = new List<EntityVehicle>(0);
            CreatedAt = DateTime.Now;
            return;
        }

        // Initialize collections with appropriate capacity
        DewCollectors = new List<TileEntityDewCollector>(ContainerUtils.DEFAULT_DEW_COLLECTOR_LIST_CAPACITY);
        Workstations = new List<TileEntityWorkstation>(ContainerUtils.DEFAULT_WORKSTATION_LIST_CAPACITY);
        Lootables = new List<ITileEntityLootable>(ContainerUtils.DEFAULT_LOOTBLE_LIST_CAPACITY);
        Vehicles = new List<EntityVehicle>(VehicleUtils.DEFAULT_VEHICLE_LIST_CAPACITY);

        // Let utility classes populate the collections
        ContainerUtils.DiscoverTileEntitySources(this);
        VehicleUtils.GetAvailableVehicleStorages(this);

        CreatedAt = DateTime.Now;

        LogUtil.DebugLog($"BatchRemovalContext created: {Lootables.Count} lootables, {DewCollectors.Count} dew collectors, {Workstations.Count} workstations, {Vehicles.Count} vehicles");
    }

    public double AgeInSeconds => (DateTime.Now - CreatedAt).TotalSeconds;

    public bool HasExpired(double lifetimeSeconds) => AgeInSeconds > lifetimeSeconds;

    public string GetSourceSummary()
    {
        return $"Lootables: {Lootables.Count}, DewCollectors: {DewCollectors.Count}, Workstations: {Workstations.Count}, Vehicles: {Vehicles.Count}, Age: {AgeInSeconds:F1}s";
    }
}