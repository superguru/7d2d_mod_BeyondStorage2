using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

public static class ItemTypeNameLookupCache
{
    private static readonly Dictionary<int, string> s_itemTypeNames = [];

    public static string GetItemTypeName(int itemType)
    {
        const string d_MethodName = nameof(GetItemTypeName);

        if (itemType < -1)
        {
            var invalidResult = $"Invalid Item Type ({itemType})";
            ModLogger.DebugLog($"{d_MethodName}({itemType}) | Invalid item type, returning: {invalidResult}");
            return invalidResult;
        }

        if (itemType == -1)
        {
            //ModLogger.DebugLog($"{d_MethodName}({itemType}) | Wildcard item type, returning: *");
            return "*";
        }

        if (itemType == 0)
        {
            //ModLogger.DebugLog($"{d_MethodName}({itemType}) | Empty item type, returning: Empty Item (0)");
            return "Empty Item (0)";
        }

        if (s_itemTypeNames.TryGetValue(itemType, out var name))
        {
            ModLogger.DebugLog($"{d_MethodName}({itemType}) | Cache hit, returning: {name}");
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
        //ModLogger.DebugLog($"{d_MethodName}({itemType}) | Cache miss, resolved and cached: {name}");

        return name;
    }
}
