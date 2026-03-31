using System;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.TileEntities;

namespace BeyondStorage.Scripts.Storage;

public class SmartSortingFunctions
{
    private static void LogSourceItems(string d_MethodName, ItemStack[] sourceItems)
    {
        if (sourceItems == null || sourceItems.Length == 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: No source items found.");
            return;
        }

        ModLogger.DebugLog($"{d_MethodName}: Source items:");
        int maxItems = sourceItems.Length;
        for (int i = 0; i < maxItems; i++)
        {
            var itemStack = sourceItems[i];
            if (itemStack != null)
            {
                ModLogger.DebugLog($"  Slot {i}/{maxItems}: {itemStack.count}x {ItemX.NameOf(itemStack)}");
            }
            else
            {
                ModLogger.DebugLog($"  Slot {i}/{maxItems}: Empty");
            }
        }
    }

    public static void SmartPlayerInventoryPush()
    {
        const string d_MethodName = nameof(SmartPlayerInventoryPush);

        /* 1. Determine the Source ItemStacks for the smart loot sort based on the active window.
         * 1a. If the active window is the player's inventory, get the ItemStacks for the player's inventory.
         * 1b. Otherwise if the active window is a loot container, get the ItemStacks for the loot container.
         * 1c. Otherwise if the active window is a vehicle storage window. If so, get the ItemStacks for the vehicle.
         * 1d. Otherwise if the active window is a workstation, get the ItemStacks for the workstation.
         * 1e. If no valid window is found, log a warning and exit the function.
         * 2. Determine the Target nearby containers that are valid for smart loot sorting. If no valid containers are found, 
         *    log a warning and exit the function.
         * 3. Perform the smart loot sort on the retrieved ItemStacks.
         */

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Performing smart player inventory push");
#endif

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var sourceItems = LootableItemHandler.GetLootableItems(context.Player);
#if DEBUG
        LogSourceItems(d_MethodName, sourceItems);
#endif

#if DEBUG
        string availableSourcesDescr = context.GetSourceSummary();
        ModLogger.DebugLog($"{d_MethodName}: Available sources: {availableSourcesDescr}");
#endif

        var availableSources = context.GetClosestContainers();

        context.InvalidateCache();
    }
}
