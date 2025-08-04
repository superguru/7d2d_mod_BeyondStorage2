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

        var sourceTypeAbbrev = NameLookups.GetAbbrev(sourceType);

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

    internal bool AnyItemsLeft(UniqueItemTypes filter)
    {
        filter ??= UniqueItemTypes.Unfiltered;
        var itemList = GetItemStacksForFilter(filter);

        return itemList.Any(stack => stack != null && stack.count > 0);
    }

    /// <summary>
    /// Gets all item stacks for the specified filter.
    /// Since we prebuild all filter lists during registration, this returns the prebuilt lists.
    /// </summary>
    /// <param name="filter">The filter to apply (null means unfiltered)</param>
    /// <returns>Prebuilt list of item stacks for the specified filter</returns>
    internal IList<ItemStack> GetItemStacksForFilter(UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(GetItemStacksForFilter);

        filter ??= UniqueItemTypes.Unfiltered;

        // Since we prebuild all filters during registration, the list should exist
        if (_collectionStore.ContainsStacksForFilter(filter, out var itemList))
        {
            return itemList;
        }

        // If we reach here, it means the filter wasn't prebuilt (no items of this type exist)
        ModLogger.Error($"{d_MethodName} | Filter '{filter}' not found - no items of this type were discovered");
        return CollectionFactory.EmptyItemStackList;
    }

    internal int CountCachedItems(UniqueItemTypes filter)
    {
        int result = 0;
        foreach (var stack in GetItemStacksForFilter(filter))
        {
            if (stack != null)
            {
                var stackCount = stack.count;
                if (stackCount > 0)
                {
                    result += stackCount;
                }
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
