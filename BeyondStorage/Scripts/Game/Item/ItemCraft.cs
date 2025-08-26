using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Item;

public static class ItemCraft
{
    /// <summary>
    /// Common logic for adding storage stacks to an existing list of item stacks.
    /// Validates context, purges invalid stacks, and adds storage items.
    /// </summary>
    /// <param name="stacks">The list to add storage stacks to</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="shouldReturnInput">Whether to return the input list on validation failure</param>
    /// <returns>The modified list (for methods that return), or null if void method</returns>
    private static List<ItemStack> AddStorageStacksToList(List<ItemStack> stacks, string methodName, bool shouldReturnInput = true)
    {
        if (stacks == null)
        {
            // Looks like there can be ghost containers, just like there can be those trees that are visible but not interactable after chopping them down
            ModLogger.DebugLog($"{methodName}: called with null items{(shouldReturnInput ? "" : ", doing nothing")}");
            return shouldReturnInput ? stacks : null;
        }

        if (!ValidationHelper.ValidateStorageContext(methodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{methodName}: Failed to create StorageContext{(shouldReturnInput ? ", returning the input stacks as is" : ", doing nothing")}");
            return shouldReturnInput ? stacks : null;
        }

        ItemX.PurgeInvalidItemStacks(stacks);

        var storageStacks = context.GetAllAvailableItemStacks(UniqueItemTypes.Unfiltered);
        stacks.AddRange(storageStacks);

        return stacks;
    }

    // Used By:
    //      XUiC_RecipeCraftCount.calcMaxCraftable
    //          Item Crafting - gets max craftable amount
    public static List<ItemStack> ItemCraft_MaxGetAllStorageStacks(List<ItemStack> stacks)
    {
        const string d_MethodName = nameof(ItemCraft_MaxGetAllStorageStacks);
        return AddStorageStacksToList(stacks, d_MethodName, shouldReturnInput: true);
    }

    // Used By:
    //      XUiC_RecipeList.Update
    //          Item Crafts - shown as available in the list
    public static void ItemCraft_AddPullableSourceStorageStacks(List<ItemStack> stacks)
    {
        const string d_MethodName = nameof(ItemCraft_AddPullableSourceStorageStacks);
        _ = AddStorageStacksToList(stacks, d_MethodName, shouldReturnInput: false);
    }

    //  Used By:
    //      XUiC_IngredientEntry.GetBindingValue
    //          Item Crafting - shows item count available in crafting window(s)
    public static int EntryBinding_AddPullableSourceStorageItemCount(int entityAvailableCount, XUiC_IngredientEntry entry)
    {
        const string d_MethodName = nameof(EntryBinding_AddPullableSourceStorageItemCount);
        int DEFAULT_RETURN_VALUE = entityAvailableCount;

        if (entry == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: ingredient entry is null, returning entityAvailableCount {DEFAULT_RETURN_VALUE}");
            return DEFAULT_RETURN_VALUE;
        }

        var itemValue = entry.Ingredient?.itemValue;
        if (!ValidationHelper.ValidateItemValue(itemValue, d_MethodName, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Failed to create StorageContext");
            return DEFAULT_RETURN_VALUE;
        }

        var storageCount = context.GetItemCount(itemValue);

        if (storageCount > 0)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: item {itemName}; adding storage count {storageCount} to entityAvailableCount {entityAvailableCount} and setting the window controller IsDirty = true");
#endif
            entry.windowGroup.Controller.IsDirty = true;
        }
        else
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: item {itemName}; initialCount {entityAvailableCount}; storageCount {storageCount}, so returning {DEFAULT_RETURN_VALUE}");
#endif
            return DEFAULT_RETURN_VALUE;
        }

        return entityAvailableCount + storageCount;
    }


    // Used By:
    //      XUiM_PlayerInventory.HasItems
    //          Item Crafting -
    public static int ItemCraft_GetRemainingItemCount(IList<ItemStack> itemStacks, int i, int stillNeeded)
    {
        int DEFAULT_RETURN_VALUE = stillNeeded;

        // Fast path: early return if nothing needed
        if (stillNeeded <= 0)
        {
            return DEFAULT_RETURN_VALUE;
        }

        // Essential validation only
        if (itemStacks == null || i < 0 || i >= itemStacks.Count)
        {
            return DEFAULT_RETURN_VALUE;
        }

        var itemValue = itemStacks[i]?.itemValue;
        if (itemValue == null || itemValue.IsEmpty())
        {
            return DEFAULT_RETURN_VALUE;
        }

        // Use the common storage item count method
        var storageCount = ItemCommon.ItemCommon_GetStorageItemCount(itemValue);
        var result = stillNeeded - storageCount;

#if DEBUG
        //var itemName = ItemX.NameOf(itemValue);
        //ModLogger.DebugLog($"{nameof(ItemCraft_GetRemainingItemCount)}: item {itemName}; stillNeeded {stillNeeded}; storageCount {storageCount}; result {result}");
#endif
        return result;
    }
}