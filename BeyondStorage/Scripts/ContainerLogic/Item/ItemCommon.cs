using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

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
        LogUtil.DebugLog($"ItemRemoveRemaining | item: {itemName}; stillNeeded: {stillNeeded}; lastRemoved: {originalResult}; totalNeeded: {totalRequiredAmount}; ignoreModded: {ignoreModdedItems}");

        // If we don't need anything else return the original result
        if (stillNeeded <= 0)
        {
            return originalResult;
        }

        // Get what we can from storage up to required amount
        var totalRemoved = ContainerUtils.RemoveRemaining(itemValue, stillNeeded, ignoreModdedItems, removedItems);

        var newStillNeeded = stillNeeded - totalRemoved;
        LogUtil.DebugLog($"ItemRemoveRemaining | item: {itemName}; removedFromStorage {totalRemoved}; newStillNeeded {newStillNeeded}");

        return newStillNeeded;
    }
}