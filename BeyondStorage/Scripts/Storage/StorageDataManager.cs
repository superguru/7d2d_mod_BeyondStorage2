using System;
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

    public readonly Func<TileEntityDewCollector, TileEntityDewCollector, bool> EqualsDewCollectorFunc = (a, b) => a.Equals(b);
    public readonly Func<TileEntityDewCollector, ItemStack[]> GetItemsDewCollectorFunc = (dc) => dc.items;
    public readonly Action<TileEntityDewCollector> MarkModifiedDewCollectorFunc = (dc) => DewCollectorStateManager.MarkDewCollectorModified(dc);

    public readonly Func<TileEntityWorkstation, TileEntityWorkstation, bool> EqualsWorkstationFunc = (a, b) => a.entityId == b.entityId;
    public readonly Func<TileEntityWorkstation, ItemStack[]> GetItemsWorkstationFunc = (workstation) => workstation.output;
    public Action<TileEntityWorkstation> MarkModifiedWorkstationFunc = (workstation) => WorkstationStateManager.MarkWorkstationModified(workstation);

    public readonly Func<ITileEntityLootable, ITileEntityLootable, bool> EqualsLootableFunc = (a, b) => a.EntityId == b.EntityId;
    public readonly Func<ITileEntityLootable, ItemStack[]> GetItemsLootableFunc = (lootable) => lootable.items;
    public Action<ITileEntityLootable> MarkModifiedLootableFunc = (lootable) => lootable.SetModified();

    public readonly Func<EntityVehicle, EntityVehicle, bool> EqualsVehicleFunc = (a, b) => a.entityId == b.entityId;
    public readonly Func<EntityVehicle, ItemStack[]> GetItemsVehicleFunc = (vehicle) => vehicle.bag.items;
    public Action<EntityVehicle> MarkModifiedVehicleFunc = (vehicle) => vehicle.SetBagModified();

    internal StorageDataManager(StorageSourceItemDataStore dataStore)
    {
        if (dataStore == null)
        {
            var error = $"{nameof(StorageDataManager)}: {nameof(dataStore)} cannot be null.";
            ModLogger.Error(error);
        }

        _dataStore = dataStore;
    }

    public void ClearAll()
    {
        DataStore.ClearAll();
    }

    public string GetSourceSummary()
    {
        return DataStore.GetDiagnosticInfo();
    }

    internal int CountCachedItems(UniqueItemTypes filter)
    {
        return DataStore.CountCachedItems(filter);
    }
}