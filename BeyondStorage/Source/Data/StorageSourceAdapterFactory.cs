using BeyondStorage.Storage;

namespace BeyondStorage.Data;

/// <summary>
/// Factory for creating <see cref="StorageSourceAdapter{T}"/> instances from a <see cref="StorageContext"/>.
/// </summary>
internal static class StorageSourceAdapterFactory
{
    internal static StorageSourceAdapter<TileEntityCollector> CreateCollectorStorageSourceAdapter(StorageContext context, TileEntityCollector collector)
    {
        var sources = context.Sources;
        return new StorageSourceAdapter<TileEntityCollector>(
            collector,
            sources.EqualsCollectorFunc,
            sources.GetCollectorAllItemsFunc,
            sources.GetCollectorLockedSlotsFunc,
            sources.MarkCollectorModifiedFunc,
            sources.GetCollectorNameFunc
        );
    }

    internal static StorageSourceAdapter<EntityDrone> CreateDroneStorageSourceAdapter(StorageContext context, EntityDrone drone)
    {
        var sources = context.Sources;
        return new StorageSourceAdapter<EntityDrone>(
            drone,
            sources.EqualsDroneEntityFunc,
            sources.GetDroneEntityAllItemsFunc,
            sources.GetDroneEntityLockedSlotsFunc,
            sources.MarkDroneEntityModifiedFunc,
            sources.GetDroneEntityNameFunc
        );
    }

    internal static StorageSourceAdapter<ITileEntityLootable> CreateLootableStorageSourceAdapter(StorageContext context, ITileEntityLootable lootable)
    {
        var sources = context.Sources;
        return new StorageSourceAdapter<ITileEntityLootable>(
            lootable,
            sources.EqualsLootableFunc,
            sources.GetLootableAllItemsFunc,
            sources.GetLootableLockedSlotsFunc,
            sources.MarkLootableModifiedFunc,
            sources.GetLootableNameFunc
        );
    }

    internal static StorageSourceAdapter<EntityPlayerLocal> CreatePlayerBackpackSourceAdapter(StorageContext context, EntityPlayerLocal player)
    {
        var sources = context.Sources;
        return new StorageSourceAdapter<EntityPlayerLocal>(
            player,
            sources.EqualsPlayerLootableFunc,
            sources.GetPlayerBackpackAllItemsFunc,
            sources.GetPlayerLockedSlotsFunc,
            sources.MarkPlayerLootableModifiedFunc,
            sources.GetPlayerLootableNameFunc
        );
    }

    internal static StorageSourceAdapter<EntityVehicle> CreateVehicleStorageSourceAdapter(StorageContext context, EntityVehicle vehicle)
    {
        var sources = context.Sources;
        return new StorageSourceAdapter<EntityVehicle>(
            vehicle,
            sources.EqualsVehicleFunc,
            sources.GetVehicleAllItemsFunc,
            sources.GetVehicleLockedSlotsFunc,
            sources.MarkVehicleModifiedFunc,
            sources.GetVehicleNameFunc
        );
    }

    internal static StorageSourceAdapter<TileEntityWorkstation> CreateWorkstationStorageSourceAdapter(StorageContext context, TileEntityWorkstation workstation)
    {
        var sources = context.Sources;
        return new StorageSourceAdapter<TileEntityWorkstation>(
            workstation,
            sources.EqualsWorkstationFunc,
            sources.GetWorkstationAllItemsFunc,
            sources.GetWorkstationLockedSlotsFunc,
            sources.MarkWorkstationModifiedFunc,
            sources.GetWorkstationNameFunc
        );
    }
}