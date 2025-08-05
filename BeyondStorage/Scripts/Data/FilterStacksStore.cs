using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

internal class FilterStacksStore
{
    private const int FILTERS_DISPLAY_MAX = 5;

    private readonly Dictionary<UniqueItemTypes, List<ItemStack>> _itemLists = [];
    private readonly UniqueItemTypeCache _uniqueItemTypeCache = new();

    /// <summary>
    /// Initializes a new instance of the FilterStacksStore class.
    /// Ensures the Unfiltered key always exists with an empty list.
    /// </summary>
    public FilterStacksStore()
    {
        // Ensure Unfiltered exists from initialization
        EnsureUnfilteredStacksExist();
    }

    public List<ItemStack> AddStackRangeForFilter(UniqueItemTypes filter, List<ItemStack> stacks)
    {
        const string d_MethodName = nameof(AddStackRangeForFilter);

        if (filter == null)
        {
            var error = $"{d_MethodName}: {nameof(filter)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(filter), error);
        }

        if (stacks == null)
        {
            var error = $"{d_MethodName}: {nameof(stacks)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(stacks), error);
        }

        var current = GetStacksForFilter(filter);
        var currentCount = current.Count;

        current.AddRange(stacks);
        var newCount = current.Count;
        var addedCount = newCount - currentCount;

        return current;
    }

    public List<ItemStack> AddStackForFilter(UniqueItemTypes filter, ItemStack stack)
    {
        const string d_MethodName = nameof(AddStackForFilter);

        if (stack == null)
        {
            var error = $"{d_MethodName}: {nameof(stack)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(stack), error);
        }

        if (filter == null)
        {
            var error = $"{d_MethodName}: {nameof(filter)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(filter), error);
        }

        var current = GetStacksForFilter(filter);
        current.Add(stack);

        return current;
    }

    public List<ItemStack> AddStackForItemType(ItemStack stack)
    {
        const string d_MethodName = nameof(AddStackForItemType);

        if (stack == null)
        {
            var error = $"{d_MethodName}: {nameof(stack)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(stack), error);
        }

        try
        {
            // Create or get the specific item type filter for this stack
            var filter = _uniqueItemTypeCache.GetOrCreateFilter(stack);

            var current = AddStackForFilter(filter, stack);
            return current;
        }
        catch (Exception ex)
        {
            ModLogger.Error($"{d_MethodName}: Failed to create filter for stack: {ex.Message}", ex);
            throw;
        }
    }

    public List<ItemStack> GetStacksForFilter(UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(GetStacksForFilter);

        if (filter == null)
        {
            var error = $"{d_MethodName}: {nameof(filter)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(filter), error);
        }

        // Always ensure the requested filter has a value
        EnsureStacksForFilterExist(filter);

        // This should always succeed now
        var itemList = _itemLists[filter];
        return itemList;
    }

    /// <summary>
    /// Determines whether all item types in the specified filter are known to this store's cache.
    /// This method checks if the item types have been encountered before, not whether
    /// the store currently contains items of those types.
    /// </summary>
    /// <param name="filter">The filter to check</param>
    /// <returns>
    /// True if all item types in the filter have been cached (encountered before);
    /// false for null, wildcard, or unknown filters
    /// </returns>
    /// <remarks>
    /// This method checks the internal cache, not the actual store contents.
    /// A filter can be "known" even if its corresponding stacks were cleared.
    /// Use ContainsStacksForFilter() to check actual store contents.
    /// </remarks>
    public bool IsFilterKnown(UniqueItemTypes filter)
    {
        if (filter == null)
        {
            return false;
        }

        var result = _uniqueItemTypeCache.IsFilterKnown(filter);
        return result;
    }

    public List<ItemStack> SetStacksForFilter(UniqueItemTypes filter, List<ItemStack> stacks)
    {
        const string d_MethodName = nameof(SetStacksForFilter);

        if (filter == null)
        {
            var error = $"{d_MethodName}: {nameof(filter)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(filter), error);
        }

        if (stacks == null)
        {
            var error = $"{d_MethodName}: {nameof(stacks)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(stacks), error);
        }

        var existed = _itemLists.ContainsKey(filter);
        _itemLists[filter] = stacks;

        return stacks;
    }

    public void ClearStacksForFilter(UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(ClearStacksForFilter);

        if (filter == null)
        {
            var error = $"{d_MethodName}: {nameof(filter)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(filter), error);
        }

        var wasRemoved = _itemLists.Remove(filter);

        if (wasRemoved)
        {
            // Note: We don't remove from _uniqueItemTypeCache because:
            // 1. Other filters might reference the same item types
            // 2. Cache indicates "seen before" not "currently exists"

            // If Unfiltered was removed, immediately recreate it
            if (filter.IsUnfiltered)
            {
                EnsureUnfilteredStacksExist();
            }
        }
    }

    public bool ContainsStacksForFilter(UniqueItemTypes filter, out List<ItemStack> stacks)
    {
        const string d_MethodName = nameof(ContainsStacksForFilter);

        if (filter == null)
        {
            var error = $"{d_MethodName}: {nameof(filter)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(filter), error);
        }

        var exists = _itemLists.TryGetValue(filter, out stacks);
        return exists;
    }

    /// <summary>
    /// Checks if the specified filter type exists in the store.
    /// This is a query operation that returns false for invalid input.
    /// </summary>
    /// <param name="filter">The filter type to check</param>
    /// <returns>True if the filter exists in the store; false for null or non-existent filters</returns>
    public bool ContainsStacksForFilter(UniqueItemTypes filter)
    {
        if (filter == null)
        {
            return false; // Query method returns false for null
        }

        return _itemLists.ContainsKey(filter);
    }

    /// <summary>
    /// Clears all item lists from the store.
    /// Always ensures Unfiltered exists with an empty list after clearing.
    /// </summary>
    public void Clear()
    {
        var filterCount = _itemLists.Count;
        _itemLists.Clear();
        _uniqueItemTypeCache.Clear();

        // Always ensure Unfiltered exists after clearing
        EnsureUnfilteredStacksExist();
    }

    private List<ItemStack> EnsureUnfilteredStacksExist()
    {
        return EnsureStacksForFilterExist(UniqueItemTypes.Unfiltered);
    }

    private List<ItemStack> EnsureStacksForFilterExist(UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(EnsureStacksForFilterExist);

        if (filter == null)
        {
            var error = $"{d_MethodName}: {nameof(filter)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(filter), error);
        }

        if (!_itemLists.ContainsKey(filter))
        {
            _itemLists[filter] = CollectionFactory.CreateItemStackList();
        }

        return _itemLists[filter];
    }

    /// <summary>
    /// Gets the number of stored filter entries.
    /// Always at least 1 due to Unfiltered key.
    /// </summary>
    public int StoredFiltersCount => _itemLists.Count;

    /// <summary>
    /// Gets all stored filter types.
    /// Always includes Unfiltered.
    /// </summary>
    /// <returns>Collection of all filter types</returns>
    public IReadOnlyCollection<UniqueItemTypes> GetAllFilters()
    {
        return _itemLists.Keys;
    }

    /// <summary>
    /// Gets diagnostic information about the current state of the item list store.
    /// </summary>
    /// <returns>String containing diagnostic information</returns>
    public string GetDiagnosticInfo()
    {
        var totalFilters = _itemLists.Count;
        var totalItems = _itemLists.Values.Sum(list => list.Count);

        var info = $"[FilterStacksStore] Filters: {totalFilters}, Total Items: {totalItems}";

        if (totalFilters > 0)
        {
            var filterDetails = _itemLists
                .Take(FILTERS_DISPLAY_MAX)
                .Select(kvp => $"{kvp.Key}({kvp.Value.Count})")
                .ToList();

            var moreInfo = totalFilters > FILTERS_DISPLAY_MAX ? $", +{totalFilters - FILTERS_DISPLAY_MAX} more" : "";
            info += $" [{string.Join(", ", filterDetails)}{moreInfo}]";
        }

        return info;
    }
}
