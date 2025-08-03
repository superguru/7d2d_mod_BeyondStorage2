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

    public IReadOnlyList<Type> GetAllowedSourceTypes()
    {
        return AllowedSources.GetAllowedSourceTypes();
    }

    public bool IsAllowedSource(Type sourceType)
    {
        return AllowedSources.IsAllowedSource(sourceType);
    }

    private UniqueItemTypes _currentFilter;
    public UniqueItemTypes CurrentFilter
    {
        get => _currentFilter;
        set
        {
            var newFilter = value ?? UniqueItemTypes.Unfiltered;
            if (!UniqueItemTypes.CanSatisfy(_currentFilter, newFilter))
            {
                Clear();
                _currentFilter = newFilter;
            }
        }
    }

    internal StorageSourceItemDataStore(AllowedSourcesSnapshot allowedSources, UniqueItemTypes filterTypes)
    {
        if (allowedSources == null)
        {
            var error = $"{nameof(StorageSourceItemDataStore)}: {nameof(allowedSources)} cannot be null.";
            ModLogger.Error(error);

            throw new ArgumentNullException(nameof(allowedSources), error);
        }

        AllowedSources = allowedSources;
        _currentFilter = filterTypes ?? UniqueItemTypes.Unfiltered;
    }

    /// <summary>
    /// Clears all relationships from the data store, removing all storage sources and item stacks.
    /// </summary>
    public void Clear()
    {
        _itemStacksBySource.Clear();
        _sourcesByItemStack.Clear();
        _sourcesByType.Clear();
    }

    public void ClearAll()
    {
        Clear();
        _currentFilter = UniqueItemTypes.Unfiltered;
    }

    /// <summary>
    /// Registers a storage source and its item stacks with the data store.
    /// Skips null or invalid item stacks to avoid invalid relationships.
    /// </summary>
    /// <param name="source">The storage source to register</param>
    /// <param name="validStacksRegistered">The number of valid item stacks that were successfully registered</param>
    public void RegisterSource(IStorageSource source, out int validStacksRegistered)
    {
        const string d_MethodName = nameof(RegisterSource);
        validStacksRegistered = 0;

        if (source == null)
        {
            ModLogger.DebugLog($"{d_MethodName}(NULL) null storage supplied");
            return;
        }

        ModLogger.DebugLog($"{d_MethodName} | Registering source: {source}");

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
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) null stacks supplied for source {source}");
            return;
        }

        ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Source has {stacks.Length} item stacks to process");

        var filter = CurrentFilter;
        int filteredOut = 0;
        int invalidStacks = 0;

        for (int i = 0; i < stacks.Length; i++)
        {
            var stack = stacks[i];

            var isPopulatedStack = UniqueItemTypes.IsPopulatedStack(stack);
            if (!isPopulatedStack)
            {
                invalidStacks++;
                continue;
            }

            if (filter.IsFiltered && !filter.Contains(stack))
            {
                filteredOut++;
                ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Stack {i} ({stack?.itemValue?.ItemClass?.Name}) filtered out by {filter}");
                continue;
            }

            //Disable for now: ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Registering stack {i}: {stack?.itemValue?.ItemClass?.Name} (count: {stack?.count})");

            // Only increment validStacksRegistered when the stack is actually added
            if (RegisterStack(source, stack))
            {
                validStacksRegistered++;
            }
        }

        ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) - Valid: {validStacksRegistered}, Filtered: {filteredOut}, Invalid: {invalidStacks}, Total: {stacks.Length}");
    }

    private bool RegisterStack(IStorageSource source, ItemStack stack)
    {
        const string d_MethodName = nameof(RegisterStack);

        // All of the stack validation is done in the caller, so we assume stack is valid here.

        // Is this stack already in the data store
        if (_sourcesByItemStack.TryGetValue(stack, out var existingStorageSource))
        {
            var sourceTypeName = NameLookups.GetName(source.GetSourceType());
            var itemName = stack?.itemValue?.ItemClass?.Name;

            // It's not good either way
            if (existingStorageSource.Equals(source))
            {
                ModLogger.DebugLog($"{d_MethodName}: ItemStack '{itemName}' is already associated with a {sourceTypeName} source.");
            }
            else
            {
                ModLogger.DebugLog($"{d_MethodName}: ItemStack '{itemName}' is already associated with another source {source?.GetType().ToString()}.");
            }

            return false; // Stack was not added (duplicate)
        }

        // Now we associate the stack with the source
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

        // Use the generic helper to find all matching source lists
        var matchingSourceLists = TypeMatchingHelper.FindAllAssignableMatches(sourceType, _sourcesByType);

        // Flatten all the source lists into a single result
        var result = new List<IStorageSource>();
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

    internal IEnumerable<ItemStack> GetAllItemStacks(UniqueItemTypes filter)
    {
        foreach (var stack in _sourcesByItemStack.Keys)
        {
            if (filter.Contains(stack))
            {
                yield return stack;
            }
        }
    }

    internal int CountCachedItems(UniqueItemTypes filter)
    {
        //TODO: remove debug logging in production code
        ModLogger.DebugLog($"DataStore_CountCachedItems called with filter: {filter}");
        int result = 0;
        foreach (var stack in GetAllItemStacks(filter))
        {
            result += stack?.count ?? 0;
        }
        ModLogger.DebugLog($"DataStore_CountCachedItems result count: {result}");
        return result;
    }

    // Make these internal for better encapsulation
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

        var info = $"[DataSource] Sources: {totalSources}, Stacks: {totalStacks}, {CurrentFilter}, Types: {totalTypes}";
        var details = string.Join(", ", _sourcesByType.Select(kvp =>
        {
            var sourceType = kvp.Key;
            var abbrev = NameLookups.GetAbbrev(sourceType);
            var count = kvp.Value.Count;
            return $"{abbrev}:{count}";
        }));

        return info + " [" + details + "]";
    }
}
