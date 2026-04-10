using BeyondStorage.Source.Storage;

namespace BeyondStorage.Source.Data;

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
            sources.GetCollectorPullableItemsFunc,
            sources.GetCollectorPushableItemsFunc,
            sources.GetCollectorAllSlotItemsFunc,
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
            sources.GetDroneEntityPullableItemsFunc,
            sources.GetDroneEntityPushableItemsFunc,
            sources.GetDroneEntityAllSlotItemsFunc,
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
            sources.GetLootablePullableItemsFunc,
            sources.GetLootablePushableItemsFunc,
            sources.GetLootableAllSlotItemsFunc,
            sources.MarkLootableModifiedFunc,
            sources.GetLootableNameFunc
        );
    }

    internal static StorageSourceAdapter<EntityPlayerLocal> CreatePlayerLootableSourceAdapter(StorageContext context, EntityPlayerLocal player)
    {
        var sources = context.Sources;
        return new StorageSourceAdapter<EntityPlayerLocal>(
            player,
            sources.EqualsPlayerLootableFunc,
            sources.GetPlayerPullableItemsFunc,
            sources.GetPlayerPushableItemsFunc,
            sources.GetPlayerAllSlotItemsFunc,
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
            sources.GetVehiclePullableItemsFunc,
            sources.GetVehiclePushableItemsFunc,
            sources.GetVehicleAllSlotItemsItemsFunc,
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
            sources.GetWorkstationPullableItemsFunc,
            sources.GetWorkstationPushableItemsFunc,
            sources.GetWorkstationAllSlotItemsItemsFunc,
            sources.MarkWorkstationModifiedFunc,
            sources.GetWorkstationNameFunc
        );
    }
}