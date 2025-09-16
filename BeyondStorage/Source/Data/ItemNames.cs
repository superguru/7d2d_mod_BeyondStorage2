using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

public static class ItemNames
{
    private static readonly Dictionary<int, string> s_itemTypeNames = [];

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
}
