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

        LogUtil.DebugLog($"{d_MethodName} | itemCount before {stacks.Count}");

        stacks.AddRange(ContainerUtils.GetPullableSourceItemStacks());
        LogUtil.DebugLog($"{d_MethodName} | itemCount after {stacks.Count}");

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

        LogUtil.DebugLog($"{d_MethodName} | items.Count at the start {stacks.Count} (not stripped)");

        ItemUtil.PurgeInvalidItemStacks(stacks);
        LogUtil.DebugLog($"{d_MethodName} | items.Count after stripping {stacks.Count}");

        stacks.AddRange(ContainerUtils.GetPullableSourceItemStacks());
        LogUtil.DebugLog($"{d_MethodName} | items.Count after pulling {stacks.Count}");
    }

    public static List<ItemStack> ItemCraft_GetAllAvailableItemStacksFromXui(XUi xui)
    {
        const string d_MethodName = nameof(ItemCraft_GetAllAvailableItemStacksFromXui);

        var result = new List<ItemStack>(ContainerUtils.DEFAULT_ITEMSTACK_LIST_CAPACITY);
        if (xui != null)
        {
            LogUtil.DebugLog($"{d_MethodName} adding all player items");
            result.AddRange(xui.PlayerInventory.GetAllItemStacks());
            LogUtil.DebugLog($"{d_MethodName} added {result.Count} player items (not stripped)");
        }
        else
        {
            LogUtil.Error($"{d_MethodName} called with null xui");
        }

        ItemCraft_AddPullableSourceStorageStacks(result);
        LogUtil.DebugLog($"{d_MethodName} returning {result.Count} items");

        return result;
    }

    //  Used By:
    //      XUiC_IngredientEntry.GetBindingValue
    //          Item Crafting - shows item count available in crafting window(s)
    public static int EntryBinding_AddPullableSourceStorageItemCount(int count, XUiC_IngredientEntry entry)
    {
        const string d_MethodName = nameof(EntryBinding_AddPullableSourceStorageItemCount);

        var itemValue = entry.Ingredient.itemValue;
        var itemName = itemValue.ItemClass.GetItemName();
        var storageCount = ContainerUtils.GetItemCount(itemValue);

        if (storageCount > 0)
        {
            LogUtil.DebugLog($"{d_MethodName} | item {itemName}; adding storage count {storageCount} to count {count} and setting the window controller IsDirty = true");
            entry.windowGroup.Controller.IsDirty = true;
        }
        else
        {
            LogUtil.DebugLog($"{d_MethodName} | item {itemName}; initialCount {count}; storageCount {storageCount} but resetting it to 0");
            storageCount = 0;
        }

        return count + storageCount;
    }


    // Used By:
    //      XUiM_PlayerInventory.HasItems
    //          Item Crafting -
    public static int ItemCraft_GetRemainingItemCount(IList<ItemStack> itemStacks, int i, int numLeft)
    {
        const string d_MethodName = nameof(ItemCraft_GetRemainingItemCount);

        if (numLeft <= 0)
        {
            LogUtil.DebugLog($"{d_MethodName} called with numLeft <= 0 ({numLeft}), returning {numLeft}");
            return 0;
        }

        if (itemStacks == null)
        {
            LogUtil.Error($"{d_MethodName} called with null itemStacks");
            return numLeft;
        }

        if (i < 0 || i >= itemStacks.Count)
        {
            LogUtil.Error($"{d_MethodName} called with out-of-bounds index: {i} (Count: {itemStacks.Count})");
            return numLeft;
        }

        var itemStack = itemStacks[i];
        if (itemStack == null)
        {
            LogUtil.Error($"{d_MethodName} called with null itemStack at index {i}");
            return numLeft;
        }

        var itemValue = itemStack.itemValue;
        if (itemValue != null || itemValue.IsEmpty())
        {
            LogUtil.Error($"{d_MethodName} called with null or empty itemValue for itemStack at index {i}");
            return numLeft;
        }

        var itemClass = itemValue.ItemClass;
        if (itemClass == null || string.IsNullOrEmpty(itemClass.Name))
        {
            LogUtil.Error($"{d_MethodName} called with null or empty itemClass for itemValue/itemStack at index {i}");
            return numLeft;
        }

        var itemName = itemClass.GetItemName();
        LogUtil.DebugLog($"{d_MethodName} Before stack {i} which is of {itemName}; numLeft {numLeft}");

        var storageCount = ContainerUtils.GetItemCount(itemValue);
        var result = numLeft - storageCount;

        LogUtil.DebugLog($"{d_MethodName} After | item {itemName}; storageCount {storageCount}; returning {result}");

        if (result < 0)
        {
            LogUtil.DebugLog($"{d_MethodName} | item {itemName}; result is negative, setting it to 0");
            result = 0;
        }

        return result;
    }
}