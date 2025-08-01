using System.Collections.Generic;

namespace BeyondStorage.Scripts.Data;

public static class CollectionFactory
{
    private const int DEFAULT_DEW_COLLECTOR_LIST_CAPACITY = 16;
    private const int DEFAULT_ITEMSTACK_LIST_CAPACITY = 128;
    private const int DEFAULT_LOOTBLE_LIST_CAPACITY = 16;
    private const int DEFAULT_STORAGESOURCE_LIST_CAPACITY = 32;
    private const int DEFAULT_VEHICLE_LIST_CAPACITY = 8;
    private const int DEFAULT_WORKSTATION_LIST_CAPACITY = 16;

    public static List<TileEntityDewCollector> CreateDewCollectorList()
    {
        return new List<TileEntityDewCollector>(DEFAULT_DEW_COLLECTOR_LIST_CAPACITY);
    }

    public static List<ItemStack> CreateItemStackList()
    {
        return new List<ItemStack>(DEFAULT_ITEMSTACK_LIST_CAPACITY);
    }

    public static List<ITileEntityLootable> CreateLootableList()
    {
        return new List<ITileEntityLootable>(DEFAULT_LOOTBLE_LIST_CAPACITY);
    }

    public static List<IStorageSource> CreateStorageSourceList()
    {
        return new List<IStorageSource>(DEFAULT_STORAGESOURCE_LIST_CAPACITY);
    }

    public static List<EntityVehicle> CreateVehicleList()
    {
        return new List<EntityVehicle>(DEFAULT_VEHICLE_LIST_CAPACITY);
    }

    public static List<TileEntityWorkstation> CreateWorkstationList()
    {
        return new List<TileEntityWorkstation>(DEFAULT_WORKSTATION_LIST_CAPACITY);
    }
}