using System.Collections.Generic;

namespace BeyondStorage.Scripts.Data;

public static class CollectionFactory
{
    private const int DEFAULT_ITEMSTACK_LIST_CAPACITY = 128;
    private const int DEFAULT_STORAGESOURCE_LIST_CAPACITY = 32;

    public static List<ItemStack> EmptyItemStackList { get; } = [];

    public static List<ItemStack> CreateItemStackList(IReadOnlyCollection<ItemStack> itemStacks)
    {
        return CreateItemStackList(itemStacks.Count);
    }

    public static List<ItemStack> CreateItemStackList(int capacity)
    {
        return capacity <= 0 ? EmptyItemStackList : new List<ItemStack>(capacity);
    }

    public static List<ItemStack> CreateItemStackList()
    {
        return CreateItemStackList(DEFAULT_ITEMSTACK_LIST_CAPACITY);
    }

    public static List<IStorageSource> CreateStorageSourceList()
    {
        return new List<IStorageSource>(DEFAULT_STORAGESOURCE_LIST_CAPACITY);
    }
}