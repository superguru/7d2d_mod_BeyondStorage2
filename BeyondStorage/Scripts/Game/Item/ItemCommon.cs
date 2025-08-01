using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Item;

public static class ItemCommon
{
    // Used By:
    //      XUiM_PlayerInventory.RemoveItems
    //          Item Crafting (Remove items on craft)
    //          Item Repair (Remove items on repair)
    public static int ItemRemoveRemaining(int originalResult, ItemValue itemValue, int totalRequiredAmount, bool ignoreModdedItems = false, List<ItemStack> removedItems = null)
    {
        var itemName = itemValue.ItemClass.GetItemName();

        // stillNeeded = totalRequiredAmount (_count1) - originalResult (DecItem(...))
        var stillNeeded = totalRequiredAmount - originalResult;
        ModLogger.DebugLog($"ItemRemoveRemaining | item: {itemName}; stillNeeded: {stillNeeded}; lastRemoved: {originalResult}; totalNeeded: {totalRequiredAmount}; ignoreModded: {ignoreModdedItems}");

        // If we don't need anything else return the original result
        if (stillNeeded <= 0)
        {
            return originalResult;
        }

        // Get what we can from storage up to required amount
        var context = StorageContextFactory.Create(nameof(ItemRemoveRemaining));
        var totalRemoved = context?.RemoveRemaining(itemValue, stillNeeded, ignoreModdedItems, removedItems) ?? 0;

        var newStillNeeded = stillNeeded - totalRemoved;
        ModLogger.DebugLog($"ItemRemoveRemaining | item: {itemName}; removedFromStorage {totalRemoved}; newStillNeeded {newStillNeeded}");

        return newStillNeeded;
    }

    public static List<ItemStack> ItemCommon_GetAllAvailableItemStacksFromXui(XUi xui)
    {
        const string d_MethodName = nameof(ItemCommon_GetAllAvailableItemStacksFromXui);

        var result = CollectionFactory.GetEmptyItemStackList();
        if (xui != null)
        {
            ModLogger.DebugLog($"{d_MethodName} adding all player items");
            result.AddRange(xui.PlayerInventory.GetAllItemStacks());
            ModLogger.DebugLog($"{d_MethodName} added {result.Count} player items (not stripped)");
        }
        else
        {
            ModLogger.Error($"{d_MethodName} called with null xui");
        }

        ItemCraft.ItemCraft_AddPullableSourceStorageStacks(result);
        ModLogger.DebugLog($"{d_MethodName} returning {result.Count} items");

        return result;
    }
}