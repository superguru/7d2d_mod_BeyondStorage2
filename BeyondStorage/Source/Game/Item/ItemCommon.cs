using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using BeyondStorage.Scripts.UI;

namespace BeyondStorage.Scripts.Game.Item;

public static class ItemCommon
{
    /// <summary>
    /// Removes multiple items from inventories following the priority order: Bag → Toolbelt → Storage
    /// This is a common pattern used across multiple systems for sequential item removal.
    /// </summary>
    /// <param name="bag">The bag inventory to try first</param>
    /// <param name="toolbelt">The toolbelt inventory to try second</param>
    /// <param name="itemStacks">The list of items to remove</param>
    /// <param name="multiplier">Multiplier to apply to each item count</param>
    /// <param name="ignoreModdedItems">Whether to ignore modded items during removal</param>
    /// <param name="removedItems">Optional list to store removed item stacks</param>
    /// <returns>Total amount removed from all sources across all items</returns>
    public static int RemoveItemsSequential(Bag bag, Inventory toolbelt, IList<ItemStack> itemStacks, int multiplier = 1, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        int totalRemovedAllItems = 0;

        // Use foreach - it's faster for IList<T> and avoids repeated bounds checking
        foreach (var itemStack in itemStacks)
        {
            // Cache the current item stack reference and its properties
            var itemValue = itemStack.itemValue;
            int amountNeeded = itemStack.count * multiplier;

            int totalRemovedThisItem = 0;
            int stillNeeded = amountNeeded;

            // Step 1: Try to remove from bag first
            int removed = bag.DecItem(itemValue, stillNeeded, ignoreModdedItems, removedItems);
            totalRemovedThisItem += removed;
            stillNeeded -= removed;

            // Step 2: If still need more, try toolbelt
            if (stillNeeded > 0)
            {
                removed = toolbelt.DecItem(itemValue, stillNeeded, ignoreModdedItems, removedItems);
                totalRemovedThisItem += removed;
                stillNeeded -= removed;

                // Step 3: If still need more, try storage
                if (stillNeeded > 0)
                {
                    removed = ItemRemoveRemaining(itemValue, stillNeeded, ignoreModdedItems, removedItems);
                    totalRemovedThisItem += removed;
                    stillNeeded -= removed;
                }
            }

            totalRemovedAllItems += totalRemovedThisItem;
        }

        return totalRemovedAllItems;
    }

    /// <summary>
    /// Removes a single item from inventories following the priority order: Bag → Toolbelt → Storage
    /// This is a convenience method for single item removal.
    /// </summary>
    /// <param name="bag">The bag inventory to try first</param>
    /// <param name="toolbelt">The toolbelt inventory to try second</param>
    /// <param name="itemValue">The item to remove</param>
    /// <param name="amountNeeded">Total amount needed to remove</param>
    /// <param name="ignoreModdedItems">Whether to ignore modded items during removal</param>
    /// <param name="removedItems">Optional list to store removed item stacks</param>
    /// <returns>Total amount removed from all sources</returns>
    public static int RemoveItemsSequential(Bag bag, Inventory toolbelt, ItemValue itemValue, int amountNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        // Create a single-item list and use the multi-item method
        var singleItemList = new List<ItemStack> { new ItemStack(itemValue, amountNeeded) };
        return RemoveItemsSequential(bag, toolbelt, singleItemList, 1, ignoreModdedItems, removedItems);
    }

    // Used during:
    //          Item Crafting (Remove items on craft)
    //          Item Repair (Remove items on repair)
    // Returns: count of items removed from storage
    internal static int ItemRemoveRemaining(ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(ItemRemoveRemaining);
        int DEFAULT_RETURN_VALUE = stillNeeded;

        // If we don't need anything else return the original result
        if (stillNeeded <= 0)
        {
            return DEFAULT_RETURN_VALUE;
        }

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

        // Get what we can from storage up to required amount
        var totalRemoved = context.RemoveRemaining(itemValue, stillNeeded, ignoreModdedItems, removedItems);
        var newStillNeeded = stillNeeded - totalRemoved;

        return totalRemoved;
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

        return playerInventoryCount + storageCount;
    }

    public static int ItemCommon_GetStorageItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(ItemCommon_GetStorageItemCount);
        const int DEFAULT_RETURN_VALUE = 0;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out string itemName))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning {DEFAULT_RETURN_VALUE}");
            return DEFAULT_RETURN_VALUE;
        }

        var itemCount = context.GetItemCount(itemValue);

        return itemCount;
    }

    public static bool HasItemInStorage(ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItemInStorage);

        const bool DEFAULT_RETURN_VALUE = false;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var result = context.HasItem(itemValue);

        return result;
    }

    public static bool HasItemInStorage(ItemStack stack)
    {
        return HasItemInStorage(stack?.itemValue);
    }
}