using System;
using System.Collections.Generic;
using BeyondStorage.Source.Data;
using BeyondStorage.Source.Entities;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.Storage;

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
    public readonly Func<EntityDrone, ItemStack[]> GetDroneEntityConsumableItemsFunc = (dr) => LootableHandler.GetConsumableItems(dr.lootContainer);
    public readonly Func<EntityDrone, ItemStack[]> GetDroneEntityPushableItemsFunc = (dr) => LootableHandler.GetPushableItems(dr.lootContainer);
    public readonly Func<EntityDrone, ItemStack[]> GetDroneEntityLoadoutItemsFunc = (dr) => LootableHandler.GetLoadoutItems(dr.lootContainer);
    public readonly Func<EntityDrone, ItemStack[]> GetDroneEntityAllSlotItemsFunc = (dr) => LootableHandler.GetAllSlotItems(dr.lootContainer);
    public readonly Action<EntityDrone> MarkDroneEntityModifiedFunc = (dr) => LootableHandler.MarkLootableModified(dr.lootContainer);
    public readonly Func<EntityDrone, string> GetDroneEntityNameFunc = (dr) => EntityHandler.GetEntityName(dr);

    public readonly Func<TileEntityCollector, TileEntityCollector, bool> EqualsCollectorFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<TileEntityCollector, ItemStack[]> GetCollectorConsumableItemsFunc = (col) => col.Items;
    public readonly Func<TileEntityCollector, ItemStack[]> GetCollectorPushableItemsFunc = (col) => col.Items;
    public readonly Func<TileEntityCollector, ItemStack[]> GetCollectorLoadoutItemsFunc = (col) => []; // No loadout capability for now
    public readonly Func<TileEntityCollector, ItemStack[]> GetCollectorAllSlotItemsFunc = (col) => col.Items;
    public readonly Action<TileEntityCollector> MarkCollectorModifiedFunc = (col) => CollectorHandler.MarkCollectorModified(col);
    public readonly Func<TileEntityCollector, string> GetCollectorNameFunc = (col) => CollectorHandler.GetCollectorName(col);

    public readonly Func<TileEntityWorkstation, TileEntityWorkstation, bool> EqualsWorkstationFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<TileEntityWorkstation, ItemStack[]> GetWorkstationConsumableItemsFunc = (workstation) => WorkstationHandler.GetConsumableItems(workstation);
    public readonly Func<TileEntityWorkstation, ItemStack[]> GetWorkstationPushableItemsFunc = (workstation) => WorkstationHandler.GetPushableItems(workstation);
    public readonly Func<TileEntityWorkstation, ItemStack[]> GetWorkstationLoadoutItemsFunc = (workstation) => []; // No loadout capability for now
    public readonly Func<TileEntityWorkstation, ItemStack[]> GetWorkstationAllSlotItemsFunc = (workstation) => WorkstationHandler.GetAllSlotItems(workstation);
    public Action<TileEntityWorkstation> MarkWorkstationModifiedFunc = (workstation) => WorkstationHandler.MarkWorkstationModified(workstation);
    public readonly Func<TileEntityWorkstation, string> GetWorkstationNameFunc = (workstation) => WorkstationHandler.GetWorkstationName(workstation);

    public readonly Func<ITileEntityLootable, ITileEntityLootable, bool> EqualsLootableFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<ITileEntityLootable, ItemStack[]> GetLootableConsumableItemsFunc = (lootable) => LootableHandler.GetConsumableItems(lootable);
    public readonly Func<ITileEntityLootable, ItemStack[]> GetLootablePushableItemsFunc = (lootable) => LootableHandler.GetPushableItems(lootable);
    public readonly Func<ITileEntityLootable, ItemStack[]> GetLootableLoadoutItemsFunc = (lootable) => LootableHandler.GetLoadoutItems(lootable);
    public readonly Func<ITileEntityLootable, ItemStack[]> GetLootableAllSlotItemsFunc = (lootable) => LootableHandler.GetAllSlotItems(lootable);
    public Action<ITileEntityLootable> MarkLootableModifiedFunc = (lootable) => LootableHandler.MarkLootableModified(lootable);
    public readonly Func<ITileEntityLootable, string> GetLootableNameFunc = (lootable) => LootableHandler.GetLootableName(lootable);

    public readonly Func<EntityVehicle, EntityVehicle, bool> EqualsVehicleFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<EntityVehicle, ItemStack[]> GetVehicleConsumableItemsFunc = vehicle => LootableHandler.GetConsumableItems(vehicle);
    public readonly Func<EntityVehicle, ItemStack[]> GetVehiclePushableItemsFunc = vehicle => LootableHandler.GetPushableItems(vehicle);
    public readonly Func<EntityVehicle, ItemStack[]> GetVehicleLoadoutItemsFunc = vehicle => LootableHandler.GetLoadoutItems(vehicle);
    public readonly Func<EntityVehicle, ItemStack[]> GetVehicleAllSlotItemsFunc = vehicle => LootableHandler.GetAllSlotItems(vehicle);
    public Action<EntityVehicle> MarkVehicleModifiedFunc = vehicle => LootableHandler.MarkLootableModified(vehicle);
    public readonly Func<EntityVehicle, string> GetVehicleNameFunc = (vehicle) => EntityHandler.GetEntityName(vehicle);

    public readonly Func<EntityPlayerLocal, EntityPlayerLocal, bool> EqualsPlayerLootableFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<EntityPlayerLocal, ItemStack[]> GetPlayerConsumableItemsFunc = player => LootableHandler.GetConsumableItems(player);
    public readonly Func<EntityPlayerLocal, ItemStack[]> GetPlayerPushableItemsFunc = player => LootableHandler.GetPushableItems(player);
    public readonly Func<EntityPlayerLocal, ItemStack[]> GetPlayerLoadoutItemsFunc = player => LootableHandler.GetLoadoutItems(player);
    public readonly Func<EntityPlayerLocal, ItemStack[]> GetPlayerAllSlotItemsFunc = player => LootableHandler.GetAllSlotItems(player);
    public Action<EntityPlayerLocal> MarkPlayerLootableModifiedFunc = player => LootableHandler.MarkLootableModified(player);
    public readonly Func<EntityPlayerLocal, string> GetPlayerLootableNameFunc = (player) => EntityHandler.GetPlayerName(player);

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

    internal IReadOnlyList<StorageTargetAdapter> GetClosestStorageSources(AllowedSourcesList allowedSourcePolicy, ItemScope filter)
    {
        var storages = DataStore.GetClosestStorageSources(allowedSourcePolicy, filter);
        return storages;
    }
}