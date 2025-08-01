using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage
{
    /// <summary>
    /// Holds and manages collections of storage sources and their associated ItemStacks.
    /// </summary>
    public class StorageSourceCollection
    {
        public List<TileEntityDewCollector> DewCollectors { get; set; }
        public List<ITileEntityLootable> Lootables { get; set; }
        public List<EntityVehicle> Vehicles { get; set; }
        public List<TileEntityWorkstation> Workstations { get; set; }

        public List<ItemStack> DewCollectorItems { get; set; }
        public List<ItemStack> WorkstationItems { get; set; }
        public List<ItemStack> ContainerItems { get; set; }
        public List<ItemStack> VehicleItems { get; set; }

        public StorageSourceCollection()
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
            DewCollectorItems = new List<ItemStack>();
            WorkstationItems = new List<ItemStack>();
            ContainerItems = new List<ItemStack>();
            VehicleItems = new List<ItemStack>();
        }

        public void ClearItemStacks()
        {
            DewCollectorItems.Clear();
            WorkstationItems.Clear();
            ContainerItems.Clear();
            VehicleItems.Clear();
        }

        public string GetSourceSummary()
        {
            return $"Lootables: {Lootables.Count}, DewCollectors: {DewCollectors.Count}, Workstations: {Workstations.Count}, Vehicles: {Vehicles.Count}";
        }

        public string GetItemStackSummary()
        {
            var totalStacks = DewCollectorItems.Count + WorkstationItems.Count + ContainerItems.Count + VehicleItems.Count;
            return $"ItemStacks - DC:{DewCollectorItems.Count}, WS:{WorkstationItems.Count}, CT:{ContainerItems.Count}, VH:{VehicleItems.Count}, Total:{totalStacks} stacks";
        }
    }
}