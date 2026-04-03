using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.TileEntities;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Holds and manages collections of storage sources and their associated ItemStacks.
/// The source collections (DewCollectors, Lootables, etc.) contain the actual storage entities,
/// while the item collections (DewCollectorItems, LootableItems, etc.) contain cached ItemStacks
/// extracted from those sources for performance optimization.
/// </summary>
public class StorageDataManager
{
    internal readonly StorageSourceItemDataStore _dataStore;
    internal StorageSourceItemDataStore DataStore => _dataStore;

    public readonly Func<EntityDrone, EntityDrone, bool> EqualsDroneEntityFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<EntityDrone, ItemStack[]> GetDroneEntityItemsPullableFunc = (dr) => LootableItemHandler.GetPullableItems(dr.lootContainer);
    public readonly Func<EntityDrone, ItemStack[]> GetDroneEntityAllSlotItemsFunc = (dr) => LootableItemHandler.GetAllSlotItemsStacks(dr.lootContainer);
    public readonly Action<EntityDrone> MarkDroneEntityModifiedFunc = (dr) => LootableItemHandler.MarkLootableModified(dr.lootContainer);
    public readonly Func<EntityDrone, string> GetDroneEntityNameFunc = (dr) => LootableItemHandler.GetLootableName(dr.lootContainer);

    public readonly Func<TileEntityCollector, TileEntityCollector, bool> EqualsCollectorFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<TileEntityCollector, ItemStack[]> GetCollectorPullableItemsFunc = (dc) => dc.Items;
    public readonly Func<TileEntityCollector, ItemStack[]> GetCollectorAllSlotItemsFunc = (dc) => dc.Items;
    public readonly Action<TileEntityCollector> MarkCollectorModifiedFunc = (dc) => CollectorStateManager.MarkCollectorModified(dc);
    public readonly Func<TileEntityCollector, string> GetCollectorNameFunc = (dc) => CollectorStateManager.GetCollectorName(dc);

    public readonly Func<TileEntityWorkstation, TileEntityWorkstation, bool> EqualsWorkstationFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<TileEntityWorkstation, ItemStack[]> GetWorkstationPullableItemsFunc = (workstation) => workstation.output;
    public readonly Func<TileEntityWorkstation, ItemStack[]> GetWorkstationAllSlotItemsItemsFunc = (workstation) => workstation.output;
    public Action<TileEntityWorkstation> MarkWorkstationModifiedFunc = (workstation) => WorkstationStateManager.MarkWorkstationModified(workstation);
    public readonly Func<TileEntityWorkstation, string> GetWorkstationNameFunc = (workstation) => WorkstationStateManager.GetWorkstationName(workstation);

    public readonly Func<ITileEntityLootable, ITileEntityLootable, bool> EqualsLootableFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<ITileEntityLootable, ItemStack[]> GetLootablePullableItemsFunc = (lootable) => LootableItemHandler.GetPullableItems(lootable);
    public readonly Func<ITileEntityLootable, ItemStack[]> GetLootableAllSlotItemsFunc = (lootable) => LootableItemHandler.GetAllSlotItemsStacks(lootable);
    public Action<ITileEntityLootable> MarkLootableModifiedFunc = (lootable) => LootableItemHandler.MarkLootableModified(lootable);
    public readonly Func<ITileEntityLootable, string> GetLootableNameFunc = (lootable) => LootableItemHandler.GetLootableName(lootable);

    public readonly Func<EntityVehicle, EntityVehicle, bool> EqualsVehicleFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<EntityVehicle, ItemStack[]> GetVehiclePullableItemsFunc = vehicle => LootableItemHandler.GetPullableItems(vehicle);
    public readonly Func<EntityVehicle, ItemStack[]> GetVehicleAllSlotItemsItemsFunc = vehicle => LootableItemHandler.GetAllSlotItems(vehicle);
    public Action<EntityVehicle> MarkVehicleModifiedFunc = vehicle => LootableItemHandler.MarkLootableModified(vehicle);
    public readonly Func<EntityVehicle, string> GetVehicleNameFunc = (vehicle) => LootableItemHandler.GetLootableName(vehicle?.lootContainer);

    public readonly Func<EntityPlayerLocal, EntityPlayerLocal, bool> EqualsPlayerLootableFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<EntityPlayerLocal, ItemStack[]> GetPlayerPullableItemsFunc = player => LootableItemHandler.GetPullableItems(player);
    public readonly Func<EntityPlayerLocal, ItemStack[]> GetPlayerAllSlotItemsFunc = player => LootableItemHandler.GetAllSlotItems(player);
    public Action<EntityPlayerLocal> MarkPlayerLootableModifiedFunc = player => LootableItemHandler.MarkLootableModified(player);
    public readonly Func<EntityPlayerLocal, string> GetPlayerLootableNameFunc = (player) => LootableItemHandler.GetPlayerLootableName(player);

    internal StorageDataManager(StorageSourceItemDataStore dataStore)
    {
        if (dataStore == null)
        {
            var error = $"{nameof(StorageDataManager)}: {nameof(dataStore)} cannot be null.";
            ModLogger.DebugLog(error);
            throw new ArgumentException(error, nameof(dataStore));
        }

        _dataStore = dataStore;
    }

    public void Clear()
    {
        DataStore.Clear();
    }

    public string GetSourceSummary()
    {
        return DataStore.GetDiagnosticInfo();
    }

    internal int CountCachedItems(UniqueItemTypes filter)
    {
        return DataStore.GetFilteredItemCount(filter);
    }

    internal IReadOnlyList<StorageTargetAdapter<ITileEntityLootable>> GetClosestTargetContainers()
    {
        var containers = DataStore.GetClosestTargetContainers();
        return containers;
    }
}