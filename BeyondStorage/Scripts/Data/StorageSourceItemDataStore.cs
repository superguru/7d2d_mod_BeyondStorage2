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

    internal AllowedSourcesSnapshot AllowedSources { get; }
    private readonly ItemStackCacheManager _cacheManager;

    internal StorageSourceItemDataStore(AllowedSourcesSnapshot allowedSources, ItemStackCacheManager cacheManager)
    {
        if (allowedSources == null)
        {
            var error = $"{nameof(StorageSourceItemDataStore)}: {nameof(allowedSources)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(allowedSources), error);
        }

        if (cacheManager == null)
        {
            var error = $"{nameof(StorageSourceItemDataStore)}: {nameof(cacheManager)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(cacheManager), error);
        }

        AllowedSources = allowedSources;
        _cacheManager = cacheManager;
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

        // We've cleared the data store, so we need to invalidate the cache
        _cacheManager.InvalidateCache();
    }

    /// <summary>
    /// Registers a storage source and its item stacks with the data store.
    /// Always registers ALL valid stacks - filtering is handled at query time.
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

        ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Processing source: {sourceType.Name}");

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

        ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Source has {stacks.Length} item stacks to process");

        int invalidStacks = 0;

        // Always register ALL valid stacks - no filtering at registration time
        for (int i = 0; i < stacks.Length; i++)
        {
            var stack = stacks[i];

            var isPopulatedStack = UniqueItemTypes.IsPopulatedStack(stack);
            if (!isPopulatedStack)
            {
                invalidStacks++;
                continue;
            }

            // No filtering here - always register valid stacks
            if (RegisterStack(source, stack))
            {
                validStacksRegistered++;
            }
        }

        ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Valid: {validStacksRegistered}, Invalid: {invalidStacks}, Total: {stacks.Length}");
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
        ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Finding sources for type: {sourceType.Name}");

        // Use the generic helper to find all matching source lists
        var matchingSourceLists = TypeMatchingHelper.FindAllAssignableMatches(sourceType, _sourcesByType);

        // Flatten all the source lists into a single result
        var result = new List<IStorageSource>();
        foreach (var sourceList in matchingSourceLists)
        {
            result.AddRange(sourceList);
        }

        ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Found {result.Count} sources");
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

    internal int ItemStackCount => _sourcesByItemStack.Count;

    internal bool AnyItemsLeft()
    {
        foreach (var stack in _sourcesByItemStack.Keys)
        {
            if (stack?.count > 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets all item stacks, optionally filtered by the specified filter.
    /// Uses the cache manager to apply filtering on-demand.
    /// </summary>
    /// <param name="filter">The filter to apply (null means unfiltered)</param>
    /// <returns>Filtered enumerable of item stacks</returns>
    internal IEnumerable<ItemStack> GetAllItemStacks(UniqueItemTypes filter)
    {
        var allStacks = _sourcesByItemStack.Keys; // Master data (always unfiltered)
        return _cacheManager.GetFilteredView(filter, allStacks);
    }

    internal int CountCachedItems(UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(CountCachedItems);

        ModLogger.DebugLog($"{d_MethodName} | Called with filter: {filter}");

        int result = 0;
        foreach (var stack in GetAllItemStacks(filter))
        {
            result += stack?.count ?? 0;
        }

        ModLogger.DebugLog($"{d_MethodName} | Result count: {result}");
        return result;
    }

    // Internal methods for better encapsulation
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

        var info = $"[DataStore] Sources: {totalSources}, Stacks: {totalStacks} (unfiltered), Types: {totalTypes}";

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
    /// Gets comprehensive diagnostic information including cache state.
    /// </summary>
    public string GetComprehensiveDiagnosticInfo()
    {
        var dataStoreInfo = GetDiagnosticInfo();
        var cacheInfo = _cacheManager.GetCacheInfo();
        var viewsInfo = _cacheManager.GetFilteredViewsInfo();

        return $"{dataStoreInfo} | Cache: {cacheInfo} | Views: {viewsInfo}";
    }

    internal bool SameCacheManager(ItemStackCacheManager cacheManager)
    {
        return ReferenceEquals(_cacheManager, cacheManager);
    }
}
