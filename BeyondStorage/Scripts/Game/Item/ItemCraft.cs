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
            ModLogger.Warning($"{d_MethodName}: called with null items");
            return stacks;  // We're not fixing the caller's mistakes
        }

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: stacks before {stacks.Count}");
#endif
        ItemStackAnalyzer.PurgeInvalidItemStacks(stacks);

        var context = StorageContextFactory.Create(d_MethodName);
        if (context != null)
        {
            var storageStacks = context.GetAllAvailableItemStacks(UniqueItemTypes.Unfiltered);
            stacks.AddRange(storageStacks);
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: stacks after {stacks.Count}, storageStacksAdded {storageStacks.Count}");
#endif
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
            ModLogger.Warning($"{d_MethodName}: called with null items");
            return;
        }

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: stacks at the start {stacks.Count} (before stripping)");
#endif
        ItemStackAnalyzer.PurgeInvalidItemStacks(stacks);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: stacks at the start {stacks.Count} (after stripping)");
#endif
        var context = StorageContextFactory.Create(d_MethodName);
        if (context != null)
        {
            var storageStacks = context.GetAllAvailableItemStacks(UniqueItemTypes.Unfiltered);
            stacks.AddRange(storageStacks);
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: stacks after pulling {stacks.Count}, storageStacksAdded {storageStacks.Count}");
#endif
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

        if (entry == null)
        {
            ModLogger.Warning($"{d_MethodName}: ingredient entry is null, returning 0");
            return 0;
        }

        var itemValue = entry.Ingredient.itemValue;
        var itemName = itemValue.ItemClass.GetItemName();

        var context = StorageContextFactory.Create(d_MethodName);
        var storageCount = context.GetItemCount(itemValue);

        if (storageCount > 0)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: item {itemName}; adding storage count {storageCount} to entityAvailableCount {entityAvailableCount} and setting the window controller IsDirty = true");
#endif
            entry.windowGroup.Controller.IsDirty = true;
        }
        else
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: item {itemName}; initialCount {entityAvailableCount}; storageCount {storageCount} but resetting it to 0");
#endif
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
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Start: item {itemName}; stillNeeded {stillNeeded}");
#endif
        // Get storage count and return result
        var context = StorageContextFactory.Create(d_MethodName);
        var storageCount = context.GetItemCount(itemStack.itemValue);
        var result = stillNeeded - storageCount;

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: End: item {itemName}; stillNeeded {stillNeeded}; storageCount {storageCount}; result {result}; context {context != null}");
#endif
        return result;
    }
}