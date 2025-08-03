using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Service responsible for querying storage sources for item availability and counts.
/// Provides read-only operations for checking what items are available in storage.
/// Assumes cache validation has already been performed by the calling context.
/// </summary>
public static class StorageQueryService
{
    /// <summary>
    /// Validates common parameters used by all query methods.
    /// </summary>
    /// <returns>True if all parameters are valid</returns>
    private static bool ValidateParameters(string methodName, StorageContext context)
    {
        if (context == null)
        {
            ModLogger.Error($"{methodName} | Context is null");
            return false;
        }

        return true;
    }

    public static int GetItemCount(StorageContext context, ItemValue filterItem)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (!ValidateParameters(d_MethodName, context))
        {
            return 0;
        }

        var filter = UniqueItemTypes.FromItemValue(filterItem);

        return GetItemCount(context, filter);
    }

    public static int GetItemCount(StorageContext context, UniqueItemTypes filter)
    {
        // No cache handling here - just pure query logic
        if (!ValidateParameters(nameof(GetItemCount), context))
        {
            return 0;
        }

        return context.Sources.CountCachedItems(filter);
    }

    public static bool HasItem(StorageContext context, ItemValue filterItem)
    {
        const string d_MethodName = nameof(HasItem);

        if (!ValidateParameters(d_MethodName, context))
        {
            return false;
        }

        var filter = UniqueItemTypes.FromItemValue(filterItem);

        return HasItem(context, filter);
    }

    public static bool HasItem(StorageContext context, UniqueItemTypes filter)
    {
        if (!ValidateParameters(nameof(HasItem), context))
        {
            return false;
        }

        return context.Sources.DataStore.AnyItemsLeft();
    }

    /// <summary>
    /// Gets all available item stacks from storage sources with optional filtering.
    /// </summary>
    public static IReadOnlyCollection<ItemStack> GetAllAvailableItemStacks(StorageContext context, UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(GetAllAvailableItemStacks);

        if (!ValidateParameters(d_MethodName, context))
        {
            ModLogger.DebugLog($"{d_MethodName} | Validation failed, returning empty collection");
#pragma warning disable IDE0301 // Simplify collection initialization
            return System.Array.Empty<ItemStack>(); // Prefer this, as it will not allocate a new array each time
#pragma warning restore IDE0301
        }

        var result = CollectionFactory.CreateItemStackList();
        result.AddRange(context.Sources.DataStore.GetAllItemStacks(filterTypes));

        ModLogger.DebugLog($"{d_MethodName} | Returning {result.Count} item stacks with filter: {filterTypes}");
        return result;
    }

    /// <summary>
    /// Removes items from storage with proper cache validation.
    /// </summary>
    /// <param name="context">The storage context</param>
    /// <param name="itemValue">The item type to remove</param>
    /// <param name="stillNeeded">The amount still needed to remove</param>
    /// <param name="ignoreModdedItems">Whether to ignore modded items during removal</param>
    /// <param name="removedItems">Optional list to track removed items</param>
    /// <returns>The actual amount removed</returns>
    public static int RemoveItems(StorageContext context, ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(RemoveItems);

        if (!ValidateParameters(d_MethodName, context))
        {
            return 0;
        }

        var filter = UniqueItemTypes.FromItemValue(itemValue);

        var itemName = itemValue?.ItemClass?.GetItemName() ?? "Unknown Item";
        ModLogger.DebugLog($"{d_MethodName} | Removing {stillNeeded} {itemName}");

        return StorageItemRemovalService.RemoveItems(context, itemValue, stillNeeded, ignoreModdedItems, removedItems);
    }
}