using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Source.Diagnostics;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.Storage;

namespace BeyondStorage.Source.Data;

internal class StorageSourceItemDataStore
{
    private readonly Dictionary<IStorageSource, List<ItemStack>> _itemStacksBySource = [];
#pragma warning disable IDE0028 // Simplify collection initialization
    private readonly Dictionary<ItemStack, IStorageSource> _sourcesByItemStack = new(ItemStackReferenceComparer.Instance);
#pragma warning restore IDE0028 // Simplify collection initialization
    private readonly Dictionary<Type, List<IStorageSource>> _sourcesByType = [];
    private readonly FilterStacksStore _collectionStore = new();
    private readonly TargetDistanceStore _distanceStore = new();

    internal AllowedSourcesList AllowedSourcesSnapshot { get; }

    internal StorageSourceItemDataStore(AllowedSourcesList allowedSourcesSnapshot)
    {
        if (allowedSourcesSnapshot == null)
        {
            var error = $"{nameof(StorageSourceItemDataStore)}: {nameof(allowedSourcesSnapshot)} cannot be null.";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(allowedSourcesSnapshot), error);
        }

        AllowedSourcesSnapshot = allowedSourcesSnapshot;
    }

    /// <summary>
    /// Gets the list of allowed source types from configuration.
    /// </summary>
    /// <returns>Read-only list of allowed source types</returns>
    internal IReadOnlyList<Type> GetAllowedSourceTypes()
    {
        return AllowedSourcesSnapshot.GetAllowedSourceTypes();
    }

    /// <summary>
    /// Checks if a source type is allowed based on current configuration.
    /// </summary>
    /// <param name="sourceType">The source type to check</param>
    /// <returns>True if the source type is allowed</returns>
    internal bool IsAllowedSource(Type sourceType)
    {
        return AllowedSourcesSnapshot.IsAllowedSource(sourceType);
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
        _distanceStore.Clear();
    }

    /// <summary>
    /// Registers a storage source with the data store.
    /// Validates the source, classifies its consumable stacks, and if the source also
    /// implements <see cref="IStorageTarget"/>, registers it as a push target.
    /// </summary>
    /// <param name="source">The storage source to register</param>
    /// <param name="distance">Distance from the player, used for push target sorting</param>
    /// <param name="consumableStacksRegistered">Number of non-empty stacks successfully registered for pull operations</param>
    public void RegisterSource(IStorageSource source, float distance, out int consumableStacksRegistered)
    {
        consumableStacksRegistered = 0;

        if (!ValidateSource(source))
        {
            return;
        }

        int countBefore = _sourcesByItemStack.Count;

        if (source is IStorageTarget target)
        {
            // Single read of the underlying storage — single pass classifies consumable stacks
            // and builds both slot maps simultaneously
            var slotData = target.GetSlotData();
            RegisterTargetableSource(source, target, distance, slotData);
        }
        else
        {
            // Pull-only source (e.g. collector, vehicle, drone) — implements IStorageSource but not IStorageTarget.
            // These sources are never push targets, so no slot maps are needed.
            // A separate GetAllItemStacks() call is required here because there is no slotData
            // from which to reuse AllSlots — unlike the IStorageTarget path above which shares
            // a single read between consumable classification and slot map building.
            RegisterConsumableStacks(source, source.GetAllItemStacks());
        }

        consumableStacksRegistered = _sourcesByItemStack.Count - countBefore;
    }

    /// <summary>
    /// Registers a source that is also a push target in a single pass through <see cref="SourceSlotData.AllSlots"/>.
    /// Consumable stack classification, all-items slot map, and pushable slot map are all built
    /// from the same iteration — no array is visited more than once.
    /// Slot maps are cloned per operation at query time, so classification only happens once at registration.
    /// </summary>
    private void RegisterTargetableSource(IStorageSource source, IStorageTarget target, float distance, SourceSlotData slotData)
    {
        var items = slotData.AllSlots;
        var lockedSlots = slotData.LockedSlots;
        var itemsLength = items.Length;
        var lockedLength = lockedSlots?.Length ?? 0;
        var hasLocks = lockedLength > 0;

        // itemsLength is a safe upper bound for distinct item type count
        var allItemsMap = new SlotMaps(itemsLength);
        var pushableMap = new SlotMaps(itemsLength);

        for (int i = 0; i < itemsLength; i++)
        {
            var stack = items[i];

            // 1. Classify consumable (non-empty) and register for pull operations
            if (ItemX.IsPopulated(stack))
            {
                RegisterConsumableStack(source, stack);
            }

            // 2. All-items map — every slot regardless of lock or fill state
            allItemsMap.RegisterSlot(stack);

            // 3. Pushable map — locked slots excluded from push targets
            if (!hasLocks || i >= lockedLength || !lockedSlots[i])
            {
                pushableMap.RegisterSlot(stack);
            }
        }

        _distanceStore.Add(target, distance, allItemsMap, pushableMap);
    }

    /// <summary>
    /// Returns true if the source is non-null and its type is allowed by the current configuration.
    /// </summary>
    private bool ValidateSource(IStorageSource source)
    {
        const string d_MethodName = nameof(RegisterSource);

        if (source == null)
        {
            ModLogger.DebugLog($"{d_MethodName}(NULL): Null storage source supplied");
            return false;
        }

        var sourceType = source.GetSourceType();
        if (!IsAllowedSource(sourceType))
        {
            var sourceTypeAbbrev = TypeNames.GetAbbrev(sourceType);
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}): Source type {sourceType.Name} not allowed, skipping");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Iterates <paramref name="stacks"/>, registering each non-empty slot as a consumable stack.
    /// Classification — non-empty = consumable — is applied here, not in the source.
    /// </summary>
    private void RegisterConsumableStacks(IStorageSource source, ItemStack[] stacks)
    {
        for (int i = 0; i < stacks.Length; i++)
        {
            var stack = stacks[i];
            if (ItemX.IsPopulated(stack))
            {
                RegisterConsumableStack(source, stack);
            }
        }
    }

    /// <summary>
    /// Registers a single validated consumable stack against its source.
    /// Logs and silently skips duplicate registrations.
    /// </summary>
    private void RegisterConsumableStack(IStorageSource source, ItemStack stack)
    {
        const string d_MethodName = nameof(RegisterConsumableStack);

        // All stack validation is done in the caller, so we assume stack is valid here

        // Check if this stack is already in the data store
        if (_sourcesByItemStack.TryGetValue(stack, out var existingStorageSource))
        {
            var sourceTypeName = TypeNames.GetName(source.GetSourceType());
            var itemName = stack?.itemValue?.ItemClass?.Name ?? "Unknown";

#if DEBUG
            // Log the duplicate registration attempt
            if (existingStorageSource.Equals(source))
            {
                ModLogger.DebugLog($"{d_MethodName}: ItemStack '{itemName}' is already associated with this {sourceTypeName} source");
            }
            else
            {
                ModLogger.DebugLog($"{d_MethodName}: ItemStack '{itemName}' is already associated with a different source");
            }
#endif

            return;
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
                var abbrev = TypeNames.GetAbbrev(sourceType);
                var count = kvp.Value.Count;
                return $"{abbrev}:{count}";
            }));
            info = $"{info} [{details}]";
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

    internal IReadOnlyList<StorageTargetAdapter> GetClosestStorageSources(AllowedSourcesList allowedSourcePolicy, ItemScope filter)
    {
        // These are already naturally in the config.range, because of the tile entity discovery process
        var storages = _distanceStore.GetClosestStorageSources(allowedSourcePolicy, filter);
        return storages;
    }

    /// <summary>
    /// Builds slot maps from <paramref name="slotData"/> and registers the target in the distance store.
    /// Slot maps are cloned per operation at query time, so classification only happens once here at registration.
    /// </summary>
    private void RegisterPushTarget(IStorageTarget target, float distance, SourceSlotData slotData)
    {
        var allItemsMap = BuildSlotMap(slotData.AllSlots);
        var pushableMap = BuildPushableSlotMap(slotData.AllSlots, slotData.LockedSlots);
        _distanceStore.Add(target, distance, allItemsMap, pushableMap);
    }

    /// <summary>
    /// Builds a slot map from all slots including empty and locked ones.
    /// Used for the all-items target map which tracks the full slot state of the storage.
    /// </summary>
    private static SlotMaps BuildSlotMap(ItemStack[] items)
    {
        var itemsLength = items.Length;
        var maps = new SlotMaps(Math.Max(ItemX.GetAverageMaxStackSizeOf(items), itemsLength));

        for (int i = 0; i < itemsLength; i++)
        {
            maps.RegisterSlot(items[i]);
        }

        return maps;
    }

    /// <summary>
    /// Builds a slot map containing only unlocked slots.
    /// Locked slot indices are skipped; all other slots including empty ones are registered
    /// so the target adapter can track available space correctly.
    /// </summary>
    private static SlotMaps BuildPushableSlotMap(ItemStack[] items, PackedBoolArray lockedSlots)
    {
        var itemsLength = items.Length;
        var lockedLength = lockedSlots?.Length ?? 0;
        var hasLocks = lockedLength > 0;
        var maps = new SlotMaps(Math.Max(ItemX.GetAverageMaxStackSizeOf(items), itemsLength));

        for (int i = 0; i < itemsLength; i++)
        {
            // Skip locked slots — they are excluded from push targets
            if (hasLocks && i < lockedLength && lockedSlots[i])
            {
                continue;
            }

            maps.RegisterSlot(items[i]);
        }

        return maps;
    }
}
