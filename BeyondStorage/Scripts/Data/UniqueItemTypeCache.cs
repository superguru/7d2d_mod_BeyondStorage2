using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;
internal class UniqueItemTypeCache
{
    private readonly Dictionary<int, UniqueItemTypes> _filterCache = new();

    public void Clear()
    {
        _filterCache.Clear();
    }

    public UniqueItemTypes GetOrCreateFilter(ItemStack stack)
    {
        const string d_MethodName = nameof(GetOrCreateFilter);

        // We do not cache WILDCARD, EMPTY, or any invalid item types
        var itemType = stack?.itemValue?.type ?? 0;
        if (itemType <= UniqueItemTypes.EMPTY)
        {
            var error = $"{d_MethodName}: {nameof(stack)} is null or has an invalid item type.";
            ModLogger.Error(error);
            throw new ArgumentException(error, nameof(stack));
        }

        var filter = GetOrCreateFilter(itemType);
        return filter;
    }

    public UniqueItemTypes GetOrCreateFilter(int itemType)
    {
        const string d_MethodName = nameof(GetOrCreateFilter);

        if (itemType <= 0)
        {
            var error = $"{d_MethodName}: {nameof(itemType)} must be greater than zero, but received {itemType}";
            ModLogger.Error(error);
            throw new ArgumentOutOfRangeException(nameof(itemType), error);
        }

        if (_filterCache.TryGetValue(itemType, out var filter))
        {
            return filter;
        }

        filter = new UniqueItemTypes(itemType);
        _filterCache[itemType] = filter;

        return filter;
    }

    /// <summary>
    /// Determines whether all item types in the specified filter have been previously cached.
    /// This method efficiently checks if all individual item types within a filter are known
    /// to this cache, indicating they have been encountered and processed before.
    /// </summary>
    /// <param name="filter">The filter containing item types to check for cache presence</param>
    /// <returns>
    /// True if ALL item types in the filter are present in the cache; otherwise false.
    /// Returns false for null filters, wildcard filters, or if any item type is not cached.
    /// </returns>
    /// <remarks>
    /// This method is optimized for performance:
    /// - Single-item filters: O(1) dictionary lookup
    /// - Multi-item filters: O(n) where n = number of items in filter (typically small)
    /// - Avoids expensive object allocations and LINQ operations
    /// 
    /// The cache only stores positive item types (> 0). Wildcard filters and invalid
    /// item types are never cached and will always return false.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Assume cache contains item types: [1, 2, 3, 5, 7]
    /// 
    /// // Single item checks
    /// IsFilterKnown(filter[1])           // Returns: true  (item 1 is cached)
    /// IsFilterKnown(filter[4])           // Returns: false (item 4 not cached)
    /// 
    /// // Multi-item checks  
    /// IsFilterKnown(filter[1, 2])        // Returns: true  (both 1 and 2 are cached)
    /// IsFilterKnown(filter[1, 4])        // Returns: false (item 4 not cached)
    /// IsFilterKnown(filter[1, 2, 3])     // Returns: true  (all items cached)
    /// IsFilterKnown(filter[8, 9])        // Returns: false (neither 8 nor 9 cached)
    /// 
    /// // Special cases
    /// IsFilterKnown(filter[WILDCARD])    // Returns: false (wildcards never cached)
    /// IsFilterKnown(null)                // Returns: false (null filters not valid)
    /// IsFilterKnown(filter[])            // Returns: false (empty filters not valid)
    /// </code>
    /// </example>
    public bool IsFilterKnown(UniqueItemTypes filter)
    {
        if (filter == null)
        {
            return false;  // Cannot ever be found
        }

        // Handle unfiltered/wildcard case explicitly
        if (filter.IsUnfiltered)
        {
            return false;  // We never cache wildcard filters
        }

        var filterCount = filter.Count;

        if (filterCount == 0)
        {
            return false;  // This cannot really happen, just being defensive
        }

        // Optimise for the most common case - single item filter
        if (filterCount == 1)
        {
            var itemType = filter.GetSingleType();
            return _filterCache.ContainsKey(itemType);
        }

        // For multi-item filters: check if ALL item types are cached
        // This is O(n) where n = filter.Count (typically small)
        // Much faster than creating new UniqueItemTypes and using CanSatisfy
        foreach (int itemType in filter)
        {
            if (!_filterCache.ContainsKey(itemType))
            {
                return false;  // Found an item type that's not cached
            }
        }

        return true;  // All item types in the filter are cached
    }
}
