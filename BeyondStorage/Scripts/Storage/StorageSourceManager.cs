using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

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
        internal List<TileEntityDewCollector> DewCollectors { get; set; }
        internal List<ITileEntityLootable> Lootables { get; set; }
        internal List<EntityVehicle> Vehicles { get; set; }
        internal List<TileEntityWorkstation> Workstations { get; set; }

        internal List<ItemStack> DewCollectorItems { get; set; }
        internal List<ItemStack> WorkstationItems { get; set; }
        internal List<ItemStack> LootableItems { get; set; }
        internal List<ItemStack> VehicleItems { get; set; }

        public StorageSourceManager()
        {
            InitializeSourceCollections();
            InitializeItemStackLists();
        }

        private void InitializeSourceCollections()
        {
            DewCollectors = CollectionFactory.GetEmptyDewCollectorList();
            Workstations = CollectionFactory.GetEmptyWorkstationList();
            Lootables = CollectionFactory.GetEmptyLootableList();
            Vehicles = CollectionFactory.GetEmptyVehicleList();
        }

        private void InitializeItemStackLists()
        {
            DewCollectorItems = CollectionFactory.GetEmptyItemStackList();
            WorkstationItems = CollectionFactory.GetEmptyItemStackList();
            LootableItems = CollectionFactory.GetEmptyItemStackList();
            VehicleItems = CollectionFactory.GetEmptyItemStackList();
        }

        public void ClearItemStacks()
        {
            DewCollectorItems?.Clear();
            WorkstationItems?.Clear();
            LootableItems?.Clear();
            VehicleItems?.Clear();
        }

        public void ClearSourceCollections()
        {
            DewCollectors?.Clear();
            Lootables?.Clear();
            Vehicles?.Clear();
            Workstations?.Clear();
        }

        public void ClearAll()
        {
            ClearSourceCollections();
            ClearItemStacks();
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