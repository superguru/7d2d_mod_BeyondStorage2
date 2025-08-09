using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Caching;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Diagnostics;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

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
    /// <param name="gameTrackedRemovedItems">Optional list to track removed items</param>
    /// <returns>The actual amount removed</returns>
    public static int RemoveItems(StorageContext context, ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> gameTrackedRemovedItems = null)
    {
        const string d_MethodName = nameof(RemoveItems);

        if (stillNeeded <= 0)
        {
            return 0;
        }

        var itemName = itemValue?.ItemClass?.GetItemName();
        ModLogger.DebugLog($"{d_MethodName}: trying to remove {stillNeeded} {itemName}");

        int originalNeeded = stillNeeded;
        var itemFilter = UniqueItemTypes.FromItemValue(itemValue);
        bool itemCanStack = ItemPropertiesCache.GetCanStack(itemValue);

        var allowedSourceTypes = context.GetAllowedSourceTypes();
        foreach (var sourceType in allowedSourceTypes)
        {
            if (stillNeeded <= 0)
            {
                break;
            }

            if (sourceType == null)
            {
                continue;
            }

            var nameInfo = NameLookups.GetNameInfo(sourceType);
            var fullSourceTypeName = NameLookups.GetFullName(nameInfo);

            var sourcesByType = context?.Sources?.DataStore?.GetSourcesByType(sourceType);
            var sourceCount = sourcesByType?.Count;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Processing {sourceCount} of {fullSourceTypeName}, stillNeeded {stillNeeded}");
#endif
            for (var iSource = 0; iSource < sourceCount; iSource++)
            {
                if (stillNeeded <= 0)
                {
                    break;
                }

                var source = sourcesByType[iSource];
                if (source == null)
                {
                    continue;
                }

                RemoveFromSource(d_MethodName, source, nameInfo, itemName, itemFilter, itemCanStack, ref stillNeeded, ignoreModdedItems, gameTrackedRemovedItems);
            }
        }

        return originalNeeded - stillNeeded;
    }

    private static void RemoveFromSource(string methodName, IStorageSource source, NameLookups.TypeNameInfo nameInfo, string itemName,
        UniqueItemTypes filter, bool itemCanStack, ref int stillNeeded, bool ignoreModdedItems, IList<ItemStack> gameTrackedRemovedItems)
    {

        int originalNeeded = stillNeeded;

        var itemStacks = source.GetItemStacks();
        var stackLength = itemStacks.Length;

        for (var iStack = 0; iStack < stackLength; iStack++)
        {
            if (stillNeeded <= 0)
            {
                break;
            }

            var stack = itemStacks[iStack];

            if (stack?.count <= 0)
            {
                // This happens a lot, especially after previous removals.
                continue;
            }

            if (!filter.Contains(stack))
            {
                continue;
            }

            var itemValue = stack.itemValue;
            if (ItemPropertiesCache.ShouldIgnoreModdedItem(itemValue, ignoreModdedItems))
            {
                continue;
            }

            if (itemCanStack)
            {
                var countToRemove = Math.Min(stack.count, stillNeeded);

                stack.count -= countToRemove;
                stillNeeded -= countToRemove;

                if (stack.count == 0)
                {
                    stack.Clear();
                }

                // This Clone operation is expensive, but in 7d2d 2.x gameTrackedRemovedItems is always null, so leaving it in for those edge cases
                gameTrackedRemovedItems?.Add(new ItemStack(itemValue.Clone(), countToRemove));
            }
            else
            {
                stack.Clear();
                --stillNeeded;

                // This Clone operation is expensive, but in 7d2d 2.x gameTrackedRemovedItems is always null, so leaving it in for those edge cases
                gameTrackedRemovedItems?.Add(stack.Clone());
            }
        }

        int removed = originalNeeded - stillNeeded;
        //ModLogger.DebugLog($"{methodName}: {nameInfo.Abbrev} | Removed {removed} {itemName}, stillNeeded {stillNeeded}");

        if (removed != 0)
        {
            source.MarkModified();
        }

#if DEBUG
        if (stillNeeded < 0)
        {
            ModLogger.DebugLog($"{methodName}: stillNeeded after {nameInfo.Abbrev} should not be negative, but is {stillNeeded}");
            stillNeeded = 0;
        }
#endif
    }
}