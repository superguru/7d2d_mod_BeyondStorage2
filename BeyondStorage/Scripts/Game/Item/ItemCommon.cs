using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Item;

public static class ItemCommon
{
    // Used By:
    //      XUiM_PlayerInventory.RemoveItems
    //          Item Crafting (ClearStacksForFilter items on craft)
    //          Item Repair (ClearStacksForFilter items on repair)
    public static int ItemRemoveRemaining(int originalResult, ItemValue itemValue, int totalRequiredAmount, bool ignoreModdedItems = false, List<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(ItemRemoveRemaining);

        if (itemValue == null)
        {
            ModLogger.Warning($"{d_MethodName}: itemValue is null, returning originalResult {originalResult}");
            return originalResult;
        }

        var context = StorageContextFactory.Create(nameof(ItemRemoveRemaining));

        var itemName = itemValue.ItemClass.GetItemName();

        // stillNeeded = totalRequiredAmount (_count1) - originalResult (DecItem(...))
        var stillNeeded = totalRequiredAmount - originalResult;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item: {itemName}; stillNeeded: {stillNeeded}; lastRemoved: {originalResult}; totalNeeded: {totalRequiredAmount}; ignoreModded: {ignoreModdedItems}");
#endif
        // If we don't need anything else return the original result
        if (stillNeeded <= 0)
        {
            return originalResult;
        }

        // Get what we can from storage up to required amount
        var totalRemoved = context.RemoveRemaining(itemValue, stillNeeded, ignoreModdedItems, removedItems);

        var newStillNeeded = stillNeeded - totalRemoved;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item: {itemName}; removedFromStorage {totalRemoved}; newStillNeeded {newStillNeeded}");
#endif
        return newStillNeeded;
    }

    public static List<ItemStack> ItemCommon_GetAllAvailableItemStacksFromXui(XUi xui)
    {
        const string d_MethodName = nameof(ItemCommon_GetAllAvailableItemStacksFromXui);

        var result = CollectionFactory.CreateItemStackList();
        if (xui != null)
        {
            result.AddRange(xui.PlayerInventory.GetAllItemStacks());
        }
        else
        {
            ModLogger.DebugLog($"{d_MethodName}: called with null xui");
        }

        ItemCraft.ItemCraft_AddPullableSourceStorageStacks(result);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: returning {result.Count} items");
#endif
        return result;
    }
}