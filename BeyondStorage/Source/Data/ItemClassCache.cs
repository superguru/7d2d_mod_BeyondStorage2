using System.Collections.Generic;
using BeyondStorage.Infrastructure;

namespace BeyondStorage.Data;

public static class ItemClassCache
{
    private static readonly Dictionary<int, string> s_itemTypeNames = [];
    private static readonly Dictionary<int, int> s_itemMaxStackSizes = [];

    private static int s_totalMaxStackSize = 0;

    public static string LookupItemName(int itemType)
    {
        const string d_MethodName = nameof(LookupItemName);

        if (itemType < UniqueItemTypes.WILDCARD)
        {
            var invalidResult = $"Invalid Item Type ({itemType})";
            ModLogger.DebugLog($"{d_MethodName}({itemType}) | Invalid item type, returning: {invalidResult}");
            return invalidResult;
        }

        if (itemType == UniqueItemTypes.WILDCARD)
        {
            return "*";  // Don't cache constants
        }

        if (itemType == UniqueItemTypes.EMPTY)
        {
            return "null";  // Don't cache constants, use consistent return value
        }

        if (s_itemTypeNames.TryGetValue(itemType, out var name))
        {
            return name;
        }

        name = ResolveItemName(itemType);
        s_itemTypeNames[itemType] = name;
        return name;
    }

    /// <summary>
    /// Resolves the name for a given item type by looking up the ItemClass and handling fallbacks.
    /// </summary>
    /// <param name="itemType">The item type to resolve</param>
    /// <returns>The resolved item name or a fallback name if not found</returns>
    private static string ResolveItemName(int itemType)
    {
        // Lookup the item class and get its name
        var itemClass = ItemClass.GetForId(itemType);
        var itemName = itemClass?.GetItemName();

        // Handle null or empty item names more robustly
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return $"Unknown Item Type {itemType}";
        }
        else
        {
            return itemName;
        }
    }

    public static string LookupItemName(ItemValue itemValue)
    {
        return LookupItemName(itemValue?.type ?? UniqueItemTypes.EMPTY);
    }

    public static string LookupItemName(ItemStack itemStack)
    {
        return LookupItemName(itemStack?.itemValue);
    }

    public static int LookupMaxStackSize(int itemType)
    {
        const string d_MethodName = nameof(LookupMaxStackSize);

        if (itemType < UniqueItemTypes.WILDCARD)
        {
            ModLogger.DebugLog($"{d_MethodName}({itemType}) | Invalid item type, returning 0");
            return 0;  // Don't cache constants
        }

        if (itemType == UniqueItemTypes.WILDCARD)
        {
            return 0;  // Don't cache constants
        }

        if (itemType == UniqueItemTypes.EMPTY)
        {
            return 0;  // Don't cache constants
        }

        if (s_itemMaxStackSizes.TryGetValue(itemType, out var maxStackSize))
        {
            return maxStackSize;
        }

        maxStackSize = ResolveMaxStackSize(itemType);
        s_itemMaxStackSizes[itemType] = maxStackSize;

        if (maxStackSize > 0)
        {
            s_totalMaxStackSize = maxStackSize;
        }

        return maxStackSize;
    }

    /// <summary>
    /// Resolves the max stack size for a given item type by looking up the ItemClass and handling fallbacks.
    /// </summary>
    /// <param name="itemType">The item type to resolve</param>
    /// <returns>The resolved max stack size, or 1 if not found</returns>
    private static int ResolveMaxStackSize(int itemType)
    {
        var itemClass = ItemClass.GetForId(itemType);
        return itemClass?.Stacknumber?.Value ?? 0;
    }

    public static int LookupMaxStackSize(ItemValue itemValue)
    {
        return LookupMaxStackSize(itemValue?.type ?? UniqueItemTypes.EMPTY);
    }

    public static int LookupMaxStackSize(ItemStack itemStack)
    {
        return LookupMaxStackSize(itemStack?.itemValue);
    }

    public static int GetAverageMaxStackSize()
    {
        int totalStacks = s_itemMaxStackSizes.Count;

        return totalStacks > 0 ? s_totalMaxStackSize / totalStacks : CollectionFactory.DEFAULT_ITEMSTACK_LIST_CAPACITY;
    }
}
