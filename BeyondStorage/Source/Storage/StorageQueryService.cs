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
    private static bool ValidateParameters(string methodName, StorageContext context, UniqueItemTypes filter)
    {
        if (!StorageContextFactory.EnsureValidContext(context, methodName))
        {
            ModLogger.DebugLog($"{methodName}: Context is null");
            return false;
        }

        if (filter == null)
        {
            ModLogger.DebugLog($"{methodName}: Filter is null");
            return false;
        }

        return true;
    }

    public static int GetItemCount(StorageContext context, ItemValue filterItem)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (filterItem == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: filterItem is null");
            return 0;
        }

        var filter = UniqueItemTypes.FromItemValue(filterItem);

        if (!ValidateParameters(d_MethodName, context, filter))
        {
            return 0;
        }

        return context.Sources.CountCachedItems(filter);
    }

    public static int GetItemCount(StorageContext context, UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (!ValidateParameters(d_MethodName, context, filter))
        {
            return 0;
        }

        return context.Sources.CountCachedItems(filter);
    }

    public static bool HasItem(StorageContext context, ItemValue filterItem)
    {
        const string d_MethodName = nameof(HasItem);

        if (filterItem == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: filterItem is null");
            return false;
        }

        var filter = UniqueItemTypes.FromItemValue(filterItem);

        if (!ValidateParameters(d_MethodName, context, filter))
        {
            return false;
        }

        return context.Sources.DataStore.AnyItemsLeft(filter);
    }

    public static bool HasItem(StorageContext context, UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(HasItem);

        if (!ValidateParameters(d_MethodName, context, filter))
        {
            return false;
        }

        return context.Sources.DataStore.AnyItemsLeft(filter);
    }

    /// <summary>
    /// Gets all available item stacks from storage sources
    /// </summary>
    public static IList<ItemStack> GetAllAvailableItemStacks(StorageContext context, UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(GetAllAvailableItemStacks);

        if (!ValidateParameters(d_MethodName, context, filter))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning empty collection");
            return CollectionFactory.EmptyItemStackList;
        }

        var result = context.Sources.DataStore.GetItemStacksForFilter(filter);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Returning {result.Count} item stacks with filter: {filter}");
#endif
        return result;
    }
}