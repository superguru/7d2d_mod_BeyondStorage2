﻿using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

public class ItemCraft
{
    // Used By:
    //      XUiC_RecipeCraftCount.calcMaxCraftable
    //          Item Crafting - gets max craftable amount
    public static List<ItemStack> ItemCraftMaxGetAllStorageStacks(List<ItemStack> items)
    {
        // Looks like there can be ghost containers, just like there can be those trees that are visible but not interactable after chopping them down
        if (items != null)
        {
            LogUtil.DebugLog($"ItemCraftMaxGetAllStorageStacks | itemCount before {items.Count}");

            items.AddRange(ContainerUtils.GetItemStacks());

            LogUtil.DebugLog($"ItemCraftMaxGetAllStorageStacks | itemCount after {items.Count}");
        }

        return items;
    }

    // Used By:
    //      XUiC_RecipeList.Update
    //          Item Crafts - shown as available in the list
    public static void ItemCraftGetAllStorageStacks(List<ItemStack> items)
    {
        // Looks like there can be ghost containers, just like there can be those trees that are visible but not interactable after chopping them down
        if (items != null)
        {
            LogUtil.DebugLog($"ItemCraftGetAllStorageStacks | items.Count before {items.Count}");

            items.AddRange(ContainerUtils.GetItemStacks());

            LogUtil.DebugLog($"ItemCraftGetAllStorageStacks | items.Count after {items.Count}");
        }
    }

    //  Used By:
    //      XUiC_IngredientEntry.GetBindingValue
    //          Item Crafting - shows item count available in crafting window(s)
    public static int EntryBindingAddAllStorageCount(int count, XUiC_IngredientEntry entry)
    {
        var itemValue = entry.Ingredient.itemValue;
        var storageCount = ContainerUtils.GetItemCount(itemValue);
        LogUtil.DebugLog($"EntryBindingAddAllStorageCount | item {itemValue.ItemClass.GetItemName()}; initialCount {count}; storageCount {storageCount}");

        if (storageCount > 0)
        {
            LogUtil.DebugLog($"EntryBindingAddAllStorageCount | item {itemValue.ItemClass.GetItemName()}; adding storage count {storageCount} to count {count} and setting the window controller IsDirty = true");
            entry.windowGroup.Controller.IsDirty = true;
        }

        return count + storageCount;
    }


    // Used By:
    //      XUiM_PlayerInventory.HasItems
    //          Item Crafting -
    public static int HasItemGetItemCount(IList<ItemStack> itemStacks, int i, int numLeft)
    {
        LogUtil.DebugLog($"HasItemGetItemCount Before {itemStacks}; {i}; {numLeft}");

        if (numLeft <= 0)
        {
            return numLeft;
        }

        var storageCount = ContainerUtils.GetItemCount(itemStacks[i].itemValue);
        LogUtil.DebugLog($"HasItemGetItemCount After | item {itemStacks[i].itemValue.ItemClass.GetItemName()}; storageCount {storageCount}");

        return numLeft - storageCount;
    }
}