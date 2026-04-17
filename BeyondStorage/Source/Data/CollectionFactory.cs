using System.Collections.Generic;

namespace BeyondStorage.Source.Data;

public static class CollectionFactory
{
    public const int DEFAULT_ITEMSTACK_LIST_CAPACITY = 128;
    public const int DEFAULT_STORAGESOURCE_LIST_CAPACITY = 32;

    public static List<ItemStack> EmptyItemStackList { get; } = [];

    public static List<ItemStack> CreateItemStackList(IReadOnlyCollection<ItemStack> itemStacks)
    {
        return CreateItemStackList(itemStacks.Count);
    }

    public static List<ItemStack> CreateItemStackList(int capacity)
    {
#pragma warning disable IDE0028 // Simplify collection initialization
        return capacity <= 0 ? EmptyItemStackList : new List<ItemStack>(capacity);
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    public static List<ItemStack> CreateItemStackList()
    {
        return CreateItemStackList(DEFAULT_ITEMSTACK_LIST_CAPACITY);
    }

    public static List<IStorageSource> CreateStorageSourceList()
    {
#pragma warning disable IDE0028 // Simplify collection initialization
        return new List<IStorageSource>(DEFAULT_STORAGESOURCE_LIST_CAPACITY);
#pragma warning restore IDE0028 // Simplify collection initialization
    }
}