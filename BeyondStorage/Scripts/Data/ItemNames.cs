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

        // Fallback to a default name if not found
        var itemClass = ItemClass.GetForId(itemType);
        var itemName = itemClass?.GetItemName();

        // Handle null or empty item names more robustly
        if (string.IsNullOrWhiteSpace(itemName))
        {
            name = $"Unknown Item Type {itemType}";
        }
        else
        {
            name = itemName;
        }

        s_itemTypeNames[itemType] = name;
        return name;
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
