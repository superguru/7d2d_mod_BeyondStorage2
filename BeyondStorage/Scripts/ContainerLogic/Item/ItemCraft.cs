using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

public class ItemCraft
{
    // Used By:
    //      XUiC_RecipeCraftCount.calcMaxCraftable
    //          Item Crafting - gets max craftable amount
    public static List<ItemStack> ItemCraft_MaxGetAllStorageStacks(List<ItemStack> stacks)
    {
        const string d_MethodName = nameof(ItemCraft_MaxGetAllStorageStacks);

        if (stacks == null)
        {
            // Looks like there can be ghost containers, just like there can be those trees that are visible but not interactable after chopping them down
            LogUtil.Error($"{d_MethodName} called with null items");
            return stacks;  // We're not fixing the caller's mistakes
        }

        LogUtil.DebugLog($"{d_MethodName} | stacks.Count before {stacks.Count}");
        ItemUtil.PurgeInvalidItemStacks(stacks);

        stacks.AddRange(ContainerUtils.GetPullableSourceItemStacks(null, out int totalItemsAddedCount));
        LogUtil.DebugLog($"{d_MethodName} | stacks.Count after {stacks.Count}, totalItemCountAdded {totalItemsAddedCount}");

        return stacks;
    }

    // Used By:
    //      XUiC_RecipeList.Update
    //          Item Crafts - shown as available in the list
    public static void ItemCraft_AddPullableSourceStorageStacks(List<ItemStack> stacks)
    {
        const string d_MethodName = nameof(ItemCraft_AddPullableSourceStorageStacks);

        if (stacks == null)
        {
            // Looks like there can be ghost containers, just like there can be those trees that are visible but not interactable after chopping them down
            LogUtil.Error($"{d_MethodName} called with null items");
            return;
        }

        LogUtil.DebugLog($"{d_MethodName} | stacks.Count at the start {stacks.Count} (not stripped)");

        ItemUtil.PurgeInvalidItemStacks(stacks);
        LogUtil.DebugLog($"{d_MethodName} | stacks.Count after stripping {stacks.Count}");

        // Todo: Add item filtering here, if needed
        stacks.AddRange(ContainerUtils.GetPullableSourceItemStacks(null, out int totalItemsAddedCount));
        LogUtil.DebugLog($"{d_MethodName} | stacks.Count after pulling {stacks.Count}, totalItemCountAdded {totalItemsAddedCount}");
    }

    //  Used By:
    //      XUiC_IngredientEntry.GetBindingValue
    //          Item Crafting - shows item count available in crafting window(s)
    public static int EntryBinding_AddPullableSourceStorageItemCount(int entityAvailableCount, XUiC_IngredientEntry entry)
    {
        const string d_MethodName = nameof(EntryBinding_AddPullableSourceStorageItemCount);

        // Todo: this should use an upper limit for the number if items required

        var itemValue = entry.Ingredient.itemValue;
        var itemName = itemValue.ItemClass.GetItemName();

        var storageCount = ContainerUtils.GetItemCount(null, itemValue);

        if (storageCount > 0)
        {
            LogUtil.DebugLog($"{d_MethodName} | item {itemName}; adding storage count {storageCount} to entityAvailableCount {entityAvailableCount} and setting the window controller IsDirty = true");
            entry.windowGroup.Controller.IsDirty = true;
        }
        else
        {
            LogUtil.DebugLog($"{d_MethodName} | item {itemName}; initialCount {entityAvailableCount}; storageCount {storageCount} but resetting it to 0");
            storageCount = 0;
        }

        return entityAvailableCount + storageCount;
    }


    // Used By:
    //      XUiM_PlayerInventory.HasItems
    //          Item Crafting -
    public static int ItemCraft_GetRemainingItemCount(IList<ItemStack> itemStacks, int i, int stillNeeded)
    {
        const string d_MethodName = nameof(ItemCraft_GetRemainingItemCount);

        // Fast path: early return if nothing needed
        if (stillNeeded <= 0)
        {
            return stillNeeded;
        }

        // Essential validation only
        if (itemStacks == null || i < 0 || i >= itemStacks.Count)
        {
            return stillNeeded;
        }

        var itemStack = itemStacks[i];
        if (itemStack?.itemValue == null || itemStack.itemValue.IsEmpty())
        {
            return stillNeeded;
        }

        // Get storage count and return result
        var storageCount = ContainerUtils.GetItemCount(null, itemStack.itemValue);
        var result = stillNeeded - storageCount;

        LogUtil.DebugLog($"{d_MethodName} | item {itemStack.itemValue.ItemClass.GetItemName()}; stillNeeded {stillNeeded}; storageCount {storageCount}; result {result}");
        return result;
    }
}