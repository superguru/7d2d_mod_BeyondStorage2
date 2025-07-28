using System.Collections.Generic;
using System.Reflection;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

public class ItemCraft
{
    // Used By:
    //      XUiC_RecipeCraftCount.calcMaxCraftable
    //          Item Crafting - gets max craftable amount
    public static List<ItemStack> ItemCraft_MaxGetAllStorageStacks(List<ItemStack> items)
    {
        string d_MethodName = MethodBase.GetCurrentMethod().Name;

        if (items != null)
        {
            LogUtil.DebugLog($"{d_MethodName} | itemCount before {items.Count}");

            items.AddRange(ContainerUtils.GetPullableSourceItemStacks());
            LogUtil.DebugLog($"{d_MethodName} | itemCount after {items.Count}");
        }
        else
        {
            // Looks like there can be ghost containers, just like there can be those trees that are visible but not interactable after chopping them down
            LogUtil.Error($"{d_MethodName} called with null items");
        }

        return items;
    }

    // Used By:
    //      XUiC_RecipeList.Update
    //          Item Crafts - shown as available in the list
    public static void ItemCraft_AddPullableSourceStorageStacks(List<ItemStack> items)
    {
        string d_MethodName = MethodBase.GetCurrentMethod().Name;

        if (items != null)
        {
            LogUtil.DebugLog($"{d_MethodName} | items.Count at the start {items.Count}");

            items.AddRange(ContainerUtils.GetPullableSourceItemStacks());
            LogUtil.DebugLog($"{d_MethodName} | items.Count after pulling {items.Count}");

            items = ContainerUtils.StripNullAndEmptyItemStacks(items);
            LogUtil.DebugLog($"{d_MethodName} | items.Count after stripping {items.Count}");
        }
        else
        {
            // Looks like there can be ghost containers, just like there can be those trees that are visible but not interactable after chopping them down
            LogUtil.Error($"{d_MethodName} called with null items");
        }
    }

    public static List<ItemStack> ItemCraft_GetAllAvailableItemStacksFromXui(XUi xui)
    {
        string d_MethodName = MethodBase.GetCurrentMethod().Name;

        var result = new List<ItemStack>(ContainerUtils.DEFAULT_ITEMSTACK_LIST_CAPACITY);
        if (xui != null)
        {
            LogUtil.DebugLog($"{d_MethodName} adding all player items");
            result.AddRange(xui.PlayerInventory.GetAllItemStacks());
            LogUtil.DebugLog($"{d_MethodName} added {result.Count} player items");
        }
        else
        {
            LogUtil.Error($"{d_MethodName} called with null xui");
        }

        ItemCraft.ItemCraft_AddPullableSourceStorageStacks(result);
        LogUtil.DebugLog($"{d_MethodName} returning {result.Count} items");

        return result;
    }

    //  Used By:v
    //      XUiC_IngredientEntry.GetBindingValue
    //          Item Crafting - shows item count available in crafting window(s)
    public static int EntryBinding_AddPullableSourceStorageItemCount(int count, XUiC_IngredientEntry entry)
    {
        string d_MethodName = MethodBase.GetCurrentMethod().Name;

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
        string d_MethodName = MethodBase.GetCurrentMethod().Name;

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
        if (itemStack == null || itemStack.itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} called with null itemStack or itemValue at index {i}");
            return numLeft;
        }

        var itemValue = itemStack.itemValue;
        if (itemValue.ItemClass == null || itemValue.IsEmpty())
        {
            LogUtil.Error($"{d_MethodName} called with null or empty ItemClass for itemValue at index {i}");
            return numLeft;
        }

        var itemName = itemValue.ItemClass.GetItemName();
        if (string.IsNullOrEmpty(itemName))
        {
            LogUtil.Error($"{d_MethodName} called with null or empty itemName for itemValue at index {i}");
            return numLeft;
        }

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