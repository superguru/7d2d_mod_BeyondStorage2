using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using BeyondStorage.Scripts.UI;

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
        int DEFAULT_RETURN_VALUE = originalResult;

        if (!ValidationHelper.ValidateItemValue(itemValue, d_MethodName, out string itemName))
        {
            ModLogger.DebugLog($"{d_MethodName}: itemValue validation failed, returning originalResult {DEFAULT_RETURN_VALUE}");
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Failed to create StorageContext, returning originalResult {DEFAULT_RETURN_VALUE}");
            return DEFAULT_RETURN_VALUE;
        }

        // stillNeeded = totalRequiredAmount (_count1) - originalResult (DecItem(...))
        var stillNeeded = totalRequiredAmount - originalResult;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item: {itemName}; stillNeeded: {stillNeeded}; lastRemoved: {originalResult}; totalNeeded: {totalRequiredAmount}; ignoreModded: {ignoreModdedItems}");
#endif
        // If we don't need anything else return the original result
        if (stillNeeded <= 0)
        {
            return DEFAULT_RETURN_VALUE;
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

        var result = CollectionFactory.EmptyItemStackList;
        if (xui != null)
        {
            result = CollectionFactory.CreateItemStackList();
            result.AddRange(xui.PlayerInventory.GetAllItemStacks());
            ItemCraft.ItemCraft_AddPullableSourceStorageStacks(result);
        }
        else
        {
            ModLogger.DebugLog($"{d_MethodName}: called with null xui");
        }

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: returning {result.Count} items");
#endif
        return result;
    }


    public static int ItemCommon_GetTotalAvailableItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(ItemCommon_GetTotalAvailableItemCount);
        const int DEFAULT_RETURN_VALUE = 0;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out string itemName))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning {DEFAULT_RETURN_VALUE}");
            return DEFAULT_RETURN_VALUE;
        }

        int playerInventoryCount = 0;

        if (UIRefreshHelper.ValidateUIComponents(context, d_MethodName))
        {
            var playerInventory = context.WorldPlayerContext.Player.playerUI.xui.PlayerInventory;
            playerInventoryCount = playerInventory.GetItemCount(itemValue);
        }

        var storageCount = context.GetItemCount(itemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: {itemName} has {playerInventoryCount} in player inventory and {storageCount} in storage");
#endif
        return playerInventoryCount + storageCount;
    }

    public static int ItemCommon_GetAvailableItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(ItemCommon_GetAvailableItemCount);
        const int DEFAULT_RETURN_VALUE = 0;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out string itemName))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning {DEFAULT_RETURN_VALUE}");
            return DEFAULT_RETURN_VALUE;
        }

        var itemCount = context.GetItemCount(itemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: {itemName} has {itemCount} available");
#endif
        return itemCount;
    }
}