using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Diagnostics;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Data;

internal class StorageSourceItemDataStore
{
    private readonly Dictionary<IStorageSource, List<ItemStack>> _itemStacksBySource = [];
    private readonly Dictionary<ItemStack, IStorageSource> _sourcesByItemStack = new(ItemStackReferenceComparer.Instance);
    private readonly Dictionary<Type, List<IStorageSource>> _sourcesByType = [];
    private readonly FilterStacksStore _collectionStore = new();

    internal AllowedSourcesSnapshot AllowedSources { get; }

    internal StorageSourceItemDataStore(AllowedSourcesSnapshot allowedSources)
    {
        if (allowedSources == null)
        {
            var error = $"{nameof(StorageSourceItemDataStore)}: {nameof(allowedSources)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(allowedSources), error);
        }

        AllowedSources = allowedSources;
    }

    /// <summary>
    /// Gets the list of allowed source types from configuration.
    /// </summary>
    /// <returns>Read-only list of allowed source types</returns>
    internal IReadOnlyList<Type> GetAllowedSourceTypes()
    {
        return AllowedSources.GetAllowedSourceTypes();
    }

    /// <summary>
    /// Checks if a source type is allowed based on current configuration.
    /// </summary>
    /// <param name="sourceType">The source type to check</param>
    /// <returns>True if the source type is allowed</returns>
    internal bool IsAllowedSource(Type sourceType)
    {
        return AllowedSources.IsAllowedSource(sourceType);
    }

    /// <summary>
    /// Clears all relationships from the data store, removing all storage sources and item stacks.
    /// </summary>
    public void Clear()
    {
        _itemStacksBySource.Clear();
        _sourcesByItemStack.Clear();
        _sourcesByType.Clear();
        _collectionStore.Clear();
    }

    /// <summary>
    /// Registers a storage source and its item stacks with the data store.
    /// Always registers ALL valid stacks and prebuilds filter lists for both Unfiltered and specific item types.
    /// </summary>
    /// <param name="source">The storage source to register</param>
    /// <param name="validStacksRegistered">The number of valid item stacks that were successfully registered</param>
    public void RegisterSource(IStorageSource source, out int validStacksRegistered)
    {
        const string d_MethodName = nameof(RegisterSource);
        validStacksRegistered = 0;

        if (source == null)
        {
            ModLogger.DebugLog($"{d_MethodName}(NULL) | Null storage source supplied");
            return;
        }

        var sourceType = source.GetSourceType();
        var sourceTypeAbbrev = NameLookups.GetAbbrev(sourceType);

        var isAllowedSource = IsAllowedSource(sourceType);
        if (!isAllowedSource)
        {
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Source type {sourceType.Name} not allowed, skipping");
            return;
        }

        ItemStack[] stacks = source.GetItemStacks();
        if (stacks == null)
        {
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Null stacks supplied for source {source}");
            return;
        }

        int invalidStacks = 0;

        // Always register ALL valid stacks and prebuild both filter lists
        for (int i = 0; i < stacks.Length; i++)
        {
            var stack = stacks[i];

            var isPopulatedStack = UniqueItemTypes.IsPopulatedStack(stack);
            if (!isPopulatedStack)
            {
                invalidStacks++;
                continue;
            }

            // Register valid stacks and prebuild filter lists
            if (RegisterStack(source, stack))
            {
                validStacksRegistered++;
            }
        }
    }

    private bool RegisterStack(IStorageSource source, ItemStack stack)
    {
        const string d_MethodName = nameof(RegisterStack);

        // All stack validation is done in the caller, so we assume stack is valid here

        // Check if this stack is already in the data store
        if (_sourcesByItemStack.TryGetValue(stack, out var existingStorageSource))
        {
            var sourceTypeName = NameLookups.GetName(source.GetSourceType());
            var itemName = stack?.itemValue?.ItemClass?.Name ?? "Unknown";

            // Log the duplicate registration attempt
            if (existingStorageSource.Equals(source))
            {
                ModLogger.DebugLog($"{d_MethodName} | ItemStack '{itemName}' is already associated with this {sourceTypeName} source");
            }
            else
            {
                ModLogger.DebugLog($"{d_MethodName} | ItemStack '{itemName}' is already associated with a different source");
            }

            return false; // Stack was not added (duplicate)
        }

        // Associate the stack with the source
        if (!_itemStacksBySource.TryGetValue(source, out var itemStacks))
        {
            // Add to source tracking
            itemStacks = CollectionFactory.CreateItemStackList();
            _itemStacksBySource[source] = itemStacks;

            // Add to type tracking
            var sourceType = source.GetSourceType();
            if (!_sourcesByType.TryGetValue(sourceType, out var sourcesOfType))
            {
                sourcesOfType = CollectionFactory.CreateStorageSourceList();
                _sourcesByType[sourceType] = sourcesOfType;
            }

            sourcesOfType.Add(source);
        }

        itemStacks.Add(stack);
        _sourcesByItemStack[stack] = source;

        // Prebuild TWO filter lists for each stack:
        // 1. Add to master unfiltered cache (contains ALL items)
        _collectionStore.AddStackForFilter(UniqueItemTypes.Unfiltered, stack);

        // 2. Add to specific item type filter (contains only items of this type)
        _collectionStore.AddStackForItemType(stack);

        return true; // Stack was successfully added
    }

    public IReadOnlyList<IStorageSource> GetSourcesByType<T>() where T : class, IStorageSource
    {
        return GetSourcesByType(typeof(T));
    }

    public IReadOnlyList<IStorageSource> GetSourcesByType(Type sourceType)
    {
        const string d_MethodName = nameof(GetSourcesByType);

        if (sourceType == null)
        {
            ModLogger.DebugLog($"{d_MethodName}(NULL) | Null source type supplied, returning empty list");
            return [];
        }

        // Use the generic helper to find all matching source lists
        var matchingSourceLists = TypeMatchingHelper.FindAllAssignableMatches(sourceType, _sourcesByType);

        // Flatten all the source lists into a single result
        var result = CollectionFactory.CreateStorageSourceList();
        foreach (var sourceList in matchingSourceLists)
        {
            result.AddRange(sourceList);
        }

        return result.AsReadOnly();
    }

    public List<ItemStack> GetItemStacksBySource(IStorageSource source)
    {
        if (_itemStacksBySource.TryGetValue(source, out List<ItemStack> result))
        {
            return result;
        }

        return [];
    }

    internal bool IsItemsSeenBefore(UniqueItemTypes filter)
    {
        if (filter == null)
        {
            return false; // No filter means no items can be discovered
        }

        // We just test if this filter has been found before
        var result = _collectionStore.IsFilterKnown(filter);
        return result;  // If the actual itemcount is 0, that's fine.
    }

    /// <summary>
    /// Determines if any items matching the specified filter are currently available (count > 0).
    /// This method first checks the cache for efficiency, then examines actual stack counts.
    /// </summary>
    /// <param name="filter">The filter to check for available items</param>
    /// <returns>True if any stacks matching the filter have count > 0; false otherwise</returns>
    internal bool AnyItemsLeft(UniqueItemTypes filter)
    {
        if (filter == null)
        {
            return false; // No filter means no items can be found
        }

        // Fast path: check cache first to avoid expensive list iteration
        // If we've never seen items of this type, we can return false immediately
        if (!IsItemsSeenBefore(filter))
        {
            return false; // No items of this type have been seen before, so none can be left
        }

        // Get the prebuilt list and check if any valid stacks exist
        var itemList = GetItemStacksForFilter(filter);

        // Fast iteration with early return - more efficient than LINQ Any()
        foreach (var stack in itemList)
        {
            if (stack?.count > 0)
            {
                return true; // Found at least one valid stack
            }
        }

        return false; // No valid stacks found
    }

    /// <summary>
    /// Gets all item stacks for the specified filter.
    /// Since we prebuild all filter lists during registration, this returns the prebuilt lists.
    /// </summary>
    /// <param name="filter">The filter to apply (null means unfiltered)</param>
    /// <returns>Prebuilt list of item stacks for the specified filter</returns>
    internal IList<ItemStack> GetItemStacksForFilter(UniqueItemTypes filter)
    {
        filter ??= UniqueItemTypes.Unfiltered;

        // Since we prebuild all filters during registration, the list should exist
        if (_collectionStore.ContainsStacksForFilter(filter, out var itemList))
        {
            return itemList;
        }

        // If we reach here, it means the filter wasn't prebuilt (no items of this type exist)
        // It's not an error, just return an empty list, because there are no items matching this filter
        return CollectionFactory.EmptyItemStackList;
    }

    /// <summary>
    /// Counts the total number of items matching the specified filter across all cached stacks.
    /// Only counts items with stack.count > 0.
    /// </summary>
    /// <param name="filter">The filter to apply for counting items</param>
    /// <returns>Total count of items matching the filter; 0 if filter is null or no items found</returns>
    internal int GetFilteredItemCount(UniqueItemTypes filter)
    {
        if (filter == null)
        {
            return 0;
        }

        // Fast path: check cache first
        if (!IsItemsSeenBefore(filter))
        {
            return 0;
        }

        var itemList = GetItemStacksForFilter(filter);

        // Direct iteration without intermediate variables for maximum performance
        int result = 0;
        for (int i = 0; i < itemList.Count; i++)
        {
            var count = itemList[i]?.count ?? 0;
            if (count > 0)
            {
                result += count;
            }
        }

        return result;
    }

    internal bool GetSourceByItemStack(ItemStack stack, out IStorageSource source)
    {
        return _sourcesByItemStack.TryGetValue(stack, out source);
    }

    internal IReadOnlyCollection<IStorageSource> GetAllSources()
    {
        return _itemStacksBySource.Keys;
    }

    internal IReadOnlyCollection<Type> GetAllSourceTypes()
    {
        return _sourcesByType.Keys;
    }

    /// <summary>
    /// Gets diagnostic information about the current state of the data store.
    /// </summary>
    public string GetDiagnosticInfo()
    {
        var totalSources = _itemStacksBySource.Count;
        var totalStacks = _sourcesByItemStack.Count;
        var totalTypes = _sourcesByType.Count;
        var storedFilters = _collectionStore.StoredFiltersCount;

        var info = $"[DataStore] Sources: {totalSources}, Stacks: {totalStacks} (master), Types: {totalTypes}, Filters: {storedFilters}";

        if (_sourcesByType.Count > 0)
        {
            var details = string.Join(", ", _sourcesByType.Select(kvp =>
            {
                var sourceType = kvp.Key;
                var abbrev = NameLookups.GetAbbrev(sourceType);
                var count = kvp.Value.Count;
                return $"{abbrev}:{count}";
            }));
            info += " [" + details + "]";
        }

        return info;
    }

    /// <summary>
    /// Gets comprehensive diagnostic information including filter store state.
    /// </summary>
    public string GetComprehensiveDiagnosticInfo()
    {
        var dataStoreInfo = GetDiagnosticInfo();
        var filterStoreInfo = _collectionStore.GetDiagnosticInfo();

        return $"{dataStoreInfo} | FilterStore: {filterStoreInfo}";
    }

    /// <summary>
    /// Invalidates all cached filter lists except the master unfiltered cache.
    /// Used when filter logic changes but the master data is still valid.
    /// </summary>
    internal void InvalidateFilterCaches()
    {
        // Clear all filter caches except the master unfiltered one
        var allFilters = _collectionStore.GetAllFilters().ToList();
        foreach (var filter in allFilters)
        {
            if (!filter.IsUnfiltered)
            {
                _collectionStore.ClearStacksForFilter(filter);
            }
        }
    }

    /// <summary>
    /// Checks if the data store has any cached stacks for the specified filter.
    /// </summary>
    /// <param name="filter">The filter to check</param>
    /// <returns>True if cached data exists for the filter</returns>
    internal bool HasCachedStacksForFilter(UniqueItemTypes filter)
    {
        if (filter == null)
        {
            return false;
        }

        return _collectionStore.ContainsStacksForFilter(filter);
    }
}
