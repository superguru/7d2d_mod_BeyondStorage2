using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Caching;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.TileEntities;

namespace BeyondStorage.Scripts.Storage
{
    /// <summary>
    /// Service responsible for removing items from various storage sources.
    /// Handles the complex logic of item removal across different storage types.
    /// </summary>
    public static class StorageItemRemovalService
    {
        /// <summary>
        /// Removes the specified amount of items from available storage sources.
        /// </summary>
        /// <param name="sources">The storage sources to remove items from</param>
        /// <param name="config">Configuration for which storage types to use</param>
        /// <param name="itemValue">The item type to remove</param>
        /// <param name="stillNeeded">The amount still needed to remove</param>
        /// <param name="ignoreModdedItems">Whether to ignore modded items during removal</param>
        /// <param name="removedItems">Optional list to track removed items</param>
        /// <returns>The actual amount removed</returns>
        public static int RemoveItems(StorageSourceCollection sources, ConfigSnapshot config, ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
        {
            const string d_MethodName = nameof(RemoveItems);

            if (stillNeeded <= 0 || itemValue == null || itemValue.ItemClass == null || itemValue.type <= 0)
            {
                return 0;
            }

            var itemName = itemValue.ItemClass.GetItemName();
            ModLogger.DebugLog($"{d_MethodName} | Trying to remove {stillNeeded} {itemName}");

            int originalNeeded = stillNeeded;

            if (stillNeeded > 0 && config.PullFromDewCollectors)
            {
                RemoveFromStorageType(d_MethodName, "DewCollectors", itemName, sources.DewCollectors, itemValue,
                    ref stillNeeded, ignoreModdedItems, removedItems,
                    dewCollector => dewCollector.items, dewCollector => DewCollectorStateManager.MarkDewCollectorModified(dewCollector));
            }

            if (stillNeeded > 0 && config.PullFromWorkstationOutputs)
            {
                RemoveFromStorageType(d_MethodName, "WorkstationOutputs", itemName, sources.Workstations, itemValue,
                    ref stillNeeded, ignoreModdedItems, removedItems,
                    workstation => workstation.output, workstation => WorkstationStateManager.MarkWorkstationModified(workstation));
            }

            if (stillNeeded > 0)
            {
                RemoveFromStorageType(d_MethodName, "Containers", itemName, sources.Lootables, itemValue,
                    ref stillNeeded, ignoreModdedItems, removedItems,
                    lootable => lootable.items, lootable => lootable.SetModified());
            }

            if (stillNeeded > 0 && config.PullFromVehicleStorage)
            {
                RemoveFromStorageType(d_MethodName, "Vehicles", itemName, sources.Vehicles, itemValue,
                    ref stillNeeded, ignoreModdedItems, removedItems,
                    vehicle => vehicle.bag.items, vehicle => vehicle.SetBagModified());
            }

            return originalNeeded - stillNeeded;
        }

        private static void RemoveFromStorageType<T>(
            string methodName,
            string storageName,
            string itemName,
            List<T> storages,
            ItemValue itemValue,
            ref int stillNeeded,
            bool ignoreModdedItems,
            IList<ItemStack> removedItems,
            Func<T, IEnumerable<ItemStack>> getItems,
            Action<T> markModified)
        {
            int originalNeeded = stillNeeded;

            foreach (var storage in storages)
            {
                if (stillNeeded <= 0)
                {
                    break;
                }

                int newNeeded = RemoveItemsFromItemStacks(getItems(storage), itemValue, stillNeeded, ignoreModdedItems, removedItems);
                if (stillNeeded != newNeeded)
                {
                    markModified(storage);
                    stillNeeded = newNeeded;
                }
            }

            int removed = originalNeeded - stillNeeded;
            ModLogger.DebugLog($"{methodName} | {storageName} | Removed {removed} {itemName}, stillNeeded {stillNeeded}");

#if DEBUG
            if (stillNeeded < 0)
            {
                ModLogger.Error($"{methodName} | stillNeeded after {storageName} should not be negative, but is {stillNeeded}");
                stillNeeded = 0;
            }
#endif
        }

        private static int RemoveItemsFromItemStacks(IEnumerable<ItemStack> items, ItemValue desiredItem, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
        {
            int filterType = desiredItem.type;
            bool itemCanStack = ItemPropertiesCache.GetCanStack(desiredItem);

            foreach (var stack in items)
            {
                if (stillNeeded <= 0)
                {
                    break;
                }

                if (stack?.count <= 0)
                {
                    continue;
                }

                var itemValue = stack.itemValue;
                if (itemValue?.type != filterType)
                {
                    continue;
                }

                if (ItemPropertiesCache.ShouldIgnoreModdedItem(itemValue, ignoreModdedItems))
                {
                    continue;
                }

                if (itemCanStack)
                {
                    var countToRemove = Math.Min(stack.count, stillNeeded);
                    removedItems?.Add(new ItemStack(itemValue.Clone(), countToRemove));
                    stack.count -= countToRemove;
                    stillNeeded -= countToRemove;
                    if (stack.count <= 0)
                    {
                        stack.Clear();
                    }
                }
                else
                {
                    removedItems?.Add(stack.Clone());
                    stack.Clear();
                    --stillNeeded;
                }
            }

            return stillNeeded;
        }
    }
}