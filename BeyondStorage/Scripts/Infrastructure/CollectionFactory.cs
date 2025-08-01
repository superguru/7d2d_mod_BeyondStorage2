using System.Collections.Generic;

namespace BeyondStorage.Scripts.Infrastructure;

public static class CollectionFactory
{
    private const int DEFAULT_DEW_COLLECTOR_LIST_CAPACITY = 16;
    private const int DEFAULT_ITEMSTACK_LIST_CAPACITY = 128;
    private const int DEFAULT_LOOTBLE_LIST_CAPACITY = 16;
    private const int DEFAULT_VEHICLE_LIST_CAPACITY = 8;
    private const int DEFAULT_WORKSTATION_LIST_CAPACITY = 16;

    public static List<TileEntityDewCollector> GetEmptyDewCollectorList()
    {
        return new List<TileEntityDewCollector>(DEFAULT_DEW_COLLECTOR_LIST_CAPACITY);
    }

    public static List<ItemStack> GetEmptyItemStackList()
    {
        return new List<ItemStack>(DEFAULT_ITEMSTACK_LIST_CAPACITY);
    }

    public static List<ITileEntityLootable> GetEmptyLootableList()
    {
        return new List<ITileEntityLootable>(DEFAULT_LOOTBLE_LIST_CAPACITY);
    }

    public static List<EntityVehicle> GetEmptyVehicleList()
    {
        return new List<EntityVehicle>(DEFAULT_VEHICLE_LIST_CAPACITY);
    }

    public static List<TileEntityWorkstation> GetEmptyWorkstationList()
    {
        return new List<TileEntityWorkstation>(DEFAULT_WORKSTATION_LIST_CAPACITY);
    }
}