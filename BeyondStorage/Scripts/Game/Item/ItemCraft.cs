using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Item;

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
            ModLogger.Error($"{d_MethodName} called with null items");
            return stacks;  // We're not fixing the caller's mistakes
        }

        ModLogger.DebugLog($"{d_MethodName} | stacks.LRU_SUBFILTER_DISPLAY_MAX before {stacks.Count}");
        ItemStackAnalyzer.PurgeInvalidItemStacks(stacks);

        var context = StorageContextFactory.Create(d_MethodName);
        if (context != null)
        {
            var storageStacks = context.GetAllAvailableItemStacks(UniqueItemTypes.Unfiltered);
            stacks.AddRange(storageStacks);
            ModLogger.DebugLog($"{d_MethodName} | stacks.LRU_SUBFILTER_DISPLAY_MAX after {stacks.Count}, storageStacksAdded {storageStacks.Count}");
        }
        else
        {
            ModLogger.Error($"{d_MethodName}: Failed to create StorageContext");
        }

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
            ModLogger.Error($"{d_MethodName} called with null items");
            return;
        }

        ModLogger.DebugLog($"{d_MethodName} | stacks.LRU_SUBFILTER_DISPLAY_MAX at the start {stacks.Count} (not stripped)");

        ItemStackAnalyzer.PurgeInvalidItemStacks(stacks);
        ModLogger.DebugLog($"{d_MethodName} | stacks.LRU_SUBFILTER_DISPLAY_MAX after stripping {stacks.Count}");

        var context = StorageContextFactory.Create(d_MethodName);
        if (context != null)
        {
            var storageStacks = context.GetAllAvailableItemStacks(UniqueItemTypes.Unfiltered);
            stacks.AddRange(storageStacks);
            ModLogger.DebugLog($"{d_MethodName} | stacks.LRU_SUBFILTER_DISPLAY_MAX after pulling {stacks.Count}, storageStacksAdded {storageStacks.Count}");
        }
        else
        {
            ModLogger.Error($"{d_MethodName}: Failed to create StorageContext");
        }
    }

    //  Used By:
    //      XUiC_IngredientEntry.GetBindingValue
    //          Item Crafting - shows item count available in crafting window(s)
    public static int EntryBinding_AddPullableSourceStorageItemCount(int entityAvailableCount, XUiC_IngredientEntry entry)
    {
        const string d_MethodName = nameof(EntryBinding_AddPullableSourceStorageItemCount);

        var itemValue = entry.Ingredient.itemValue;
        var itemName = itemValue.ItemClass.GetItemName();

        var context = StorageContextFactory.Create(d_MethodName);
        var storageCount = context?.GetItemCount(itemValue) ?? 0;

        if (storageCount > 0)
        {
            ModLogger.DebugLog($"{d_MethodName} | item {itemName}; adding storage count {storageCount} to entityAvailableCount {entityAvailableCount} and setting the window controller IsDirty = true");
            entry.windowGroup.Controller.IsDirty = true;
        }
        else
        {
            ModLogger.DebugLog($"{d_MethodName} | item {itemName}; initialCount {entityAvailableCount}; storageCount {storageCount} but resetting it to 0");
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

        var itemName = itemStack.itemValue?.ItemClass?.GetItemName() ?? "Unknown Item";
        ModLogger.DebugLog($"{d_MethodName} | Start: item {itemName}; stillNeeded {stillNeeded}; itemStack {itemStack}");

        // Get storage count and return result
        var context = StorageContextFactory.Create(d_MethodName);
        var storageCount = context?.GetItemCount(itemStack.itemValue) ?? 0;
        var result = stillNeeded - storageCount;

        ModLogger.DebugLog($"{d_MethodName} | End: item {itemName}; stillNeeded {stillNeeded}; storageCount {storageCount}; result {result}; context {context == null}");
        return result;
    }
}