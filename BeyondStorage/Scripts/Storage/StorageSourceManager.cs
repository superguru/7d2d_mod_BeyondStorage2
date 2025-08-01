using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.TileEntities;

namespace BeyondStorage.Scripts.Storage
{
    /// <summary>
    /// Holds and manages collections of storage sources and their associated ItemStacks.
    /// The source collections (DewCollectors, Lootables, etc.) contain the actual storage entities,
    /// while the item collections (DewCollectorItems, LootableItems, etc.) contain cached ItemStacks
    /// extracted from those sources for performance optimization.
    /// </summary>
    public class StorageSourceManager
    {
        internal readonly StorageSourceItemDataStore StorageItemDataStore = new StorageSourceItemDataStore();

        private readonly Action<TileEntityDewCollector> _markDewCollectorModified = dc => DewCollectorStateManager.MarkDewCollectorModified(dc);
        private readonly Func<TileEntityDewCollector, TileEntityDewCollector, bool> _equalsDewCollectorFunc = (a, b) => a.Equals(b);

        private Action<TileEntityWorkstation> _markWorkstationModified = workstation => WorkstationStateManager.MarkWorkstationModified(workstation);
        private readonly Func<TileEntityWorkstation, TileEntityWorkstation, bool> _equalsWorkstationFunc = (a, b) => a.entityId == b.entityId;

        private Action<ITileEntityLootable> _markLootableModified = lootable => lootable.SetModified();
        private readonly Func<ITileEntityLootable, ITileEntityLootable, bool> _lootableEquals = (a, b) => a.EntityId == b.EntityId;

        private Action<EntityVehicle> _markVehicleModified = vehicle => vehicle.SetBagModified();
        private readonly Func<EntityVehicle, EntityVehicle, bool> _vehicleEquals = (a, b) => a.entityId == b.entityId;

        private readonly List<IStorageSource> _allSources = CollectionFactory.CreateStorageSourceList();


        internal List<TileEntityDewCollector> DewCollectors { get; set; }
        internal List<ITileEntityLootable> Lootables { get; set; }
        internal List<EntityVehicle> Vehicles { get; set; }
        internal List<TileEntityWorkstation> Workstations { get; set; }

        internal List<ItemStack> DewCollectorItems { get; set; }
        internal List<ItemStack> WorkstationItems { get; set; }
        internal List<ItemStack> LootableItems { get; set; }
        internal List<ItemStack> VehicleItems { get; set; }

        public StorageSourceManager() { }

        public void ClearAll()
        {
            StorageItemDataStore.Clear();
            _allSources.Clear();
        }

        public string GetSourceSummary()
        {
            return $"Lootables: {Lootables?.Count ?? 0}, DewCollectors: {DewCollectors?.Count ?? 0}, Workstations: {Workstations?.Count ?? 0}, Vehicles: {Vehicles?.Count ?? 0}";
        }

        public string GetItemStackSummary()
        {
            int dewCount = DewCollectorItems?.Count ?? 0;
            int workstationCount = WorkstationItems?.Count ?? 0;
            int lootableCount = LootableItems?.Count ?? 0;
            int vehicleCount = VehicleItems?.Count ?? 0;

            var totalStacks = dewCount + workstationCount + lootableCount + vehicleCount;
            return $"ItemStacks - DC:{dewCount}, WS:{workstationCount}, LT:{lootableCount}, VH:{vehicleCount}, Total:{totalStacks} stacks";
        }

        public bool IsValid()
        {
            return DewCollectors != null && Lootables != null && Vehicles != null && Workstations != null &&
                   DewCollectorItems != null && WorkstationItems != null && LootableItems != null && VehicleItems != null;
        }
    }
}