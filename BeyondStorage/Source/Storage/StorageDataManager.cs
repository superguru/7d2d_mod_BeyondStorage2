using System;
using System.Collections.Generic;
using BeyondStorage.Source.Data;
using BeyondStorage.Source.Entities;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.Storage;

/// <summary>
/// Holds and manages collections of storage sources and their associated ItemStacks.
/// Each source type exposes two funcs: one that returns all raw slots (no filtering),
/// and one that returns the packed lock state (null for sources without slot lock support).
/// Classification of consumable, pushable, and empty slots is the responsibility of
/// <see cref="StorageSourceItemDataStore"/>, not the funcs defined here.
/// </summary>
public class StorageDataManager
{
    internal readonly StorageSourceItemDataStore _dataStore;
    internal StorageSourceItemDataStore DataStore => _dataStore;

    // ── Drone ────────────────────────────────────────────────────────────────
    public readonly Func<EntityDrone, EntityDrone, bool> EqualsDroneEntityFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<EntityDrone, ItemStack[]> GetDroneEntityAllItemsFunc = drone => LootableHandler.GetAllSlotItems(drone.lootContainer);
    public readonly Func<EntityDrone, PackedBoolArray> GetDroneEntityLockedSlotsFunc = (drone) => drone.bag?.LockedSlots;
    public readonly Action<EntityDrone> MarkDroneEntityModifiedFunc = drone => LootableHandler.MarkLootableModified(drone.lootContainer);
    public readonly Func<EntityDrone, string> GetDroneEntityNameFunc = drone => EntityHandler.GetEntityName(drone);

    // ── Collector ────────────────────────────────────────────────────────────
    public readonly Func<TileEntityCollector, TileEntityCollector, bool> EqualsCollectorFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<TileEntityCollector, ItemStack[]> GetCollectorAllItemsFunc = col => col.Items;
    public readonly Func<TileEntityCollector, PackedBoolArray> GetCollectorLockedSlotsFunc = _ => null; // Collectors have no slot lock support
    public readonly Action<TileEntityCollector> MarkCollectorModifiedFunc = col => CollectorHandler.MarkCollectorStorageModified(col);
    public readonly Func<TileEntityCollector, string> GetCollectorNameFunc = col => CollectorHandler.GetCollectorName(col);

    // ── Workstation ──────────────────────────────────────────────────────────
    public readonly Func<TileEntityWorkstation, TileEntityWorkstation, bool> EqualsWorkstationFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<TileEntityWorkstation, ItemStack[]> GetWorkstationAllItemsFunc = workstation => WorkstationHandler.GetAllSlotItems(workstation);
    public readonly Func<TileEntityWorkstation, PackedBoolArray> GetWorkstationLockedSlotsFunc = _ => null; // Workstations have no slot lock support
    public Action<TileEntityWorkstation> MarkWorkstationModifiedFunc = workstation => WorkstationHandler.MarkWorkstationStorageModified(workstation);
    public readonly Func<TileEntityWorkstation, string> GetWorkstationNameFunc = workstation => WorkstationHandler.GetWorkstationName(workstation);

    // ── Lootable ─────────────────────────────────────────────────────────────
    public readonly Func<ITileEntityLootable, ITileEntityLootable, bool> EqualsLootableFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<ITileEntityLootable, ItemStack[]> GetLootableAllItemsFunc = lootable => LootableHandler.GetAllSlotItems(lootable);
    public readonly Func<ITileEntityLootable, PackedBoolArray> GetLootableLockedSlotsFunc = lootable => LootableHandler.GetLootableLockedSlots(lootable);
    public Action<ITileEntityLootable> MarkLootableModifiedFunc = lootable => LootableHandler.MarkLootableModified(lootable);
    public readonly Func<ITileEntityLootable, string> GetLootableNameFunc = lootable => LootableHandler.GetLootableName(lootable);

    // ── Vehicle ──────────────────────────────────────────────────────────────
    public readonly Func<EntityVehicle, EntityVehicle, bool> EqualsVehicleFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<EntityVehicle, ItemStack[]> GetVehicleAllItemsFunc = vehicle => EntityHandler.GetAllSlotItems(vehicle);
    public readonly Func<EntityVehicle, PackedBoolArray> GetVehicleLockedSlotsFunc = vehicle => vehicle.bag?.LockedSlots;
    public Action<EntityVehicle> MarkVehicleModifiedFunc = vehicle => EntityHandler.MarkVehicleStorageModified(vehicle);
    public readonly Func<EntityVehicle, string> GetVehicleNameFunc = vehicle => EntityHandler.GetEntityName(vehicle);

    // ── Player ───────────────────────────────────────────────────────────────
    public readonly Func<EntityPlayerLocal, EntityPlayerLocal, bool> EqualsPlayerLootableFunc = (a, b) => ReferenceEquals(a, b);
    public readonly Func<EntityPlayerLocal, ItemStack[]> GetPlayerAllItemsFunc = player => EntityHandler.GetAllSlotItems(player);
    public readonly Func<EntityPlayerLocal, PackedBoolArray> GetPlayerLockedSlotsFunc = player => player.bag?.LockedSlots;
    public Action<EntityPlayerLocal> MarkPlayerLootableModifiedFunc = player => EntityHandler.MarkPlayerInventoryModified(player);
    public readonly Func<EntityPlayerLocal, string> GetPlayerLootableNameFunc = player => EntityHandler.GetPlayerName(player);

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