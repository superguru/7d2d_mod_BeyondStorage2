using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public sealed class StorageAccessContext
{
    // Default cache duration
    private const double DEFAULT_CACHE_DURATION = 1.0;

    // Single cache for batch removal operations
    private static readonly TimeBasedCache<StorageAccessContext> s_contextCache = new(DEFAULT_CACHE_DURATION, nameof(StorageAccessContext));

    // Static invalidation tracking - incremented each time static cache is invalidated
    private static long s_globalInvalidationCounter = 0;

    public ConfigSnapshot Config { get; }
    public WorldPlayerContext WorldPlayerContext { get; }

    public List<TileEntityDewCollector> DewCollectors { get; private set; }
    public List<ITileEntityLootable> Lootables { get; private set; }
    public List<EntityVehicle> Vehicles { get; private set; }
    public List<TileEntityWorkstation> Workstations { get; private set; }

    // ItemStack lists for filtered results - populated on-demand and cached
    public List<ItemStack> DewCollectorItems { get; private set; }
    public List<ItemStack> WorkstationItems { get; private set; }
    public List<ItemStack> ContainerItems { get; private set; }
    public List<ItemStack> VehicleItems { get; private set; }

    // Cache tracking for ItemStack lists - using -1 for unfiltered instead of nullable
    private int _lastFilterItemType = -1; // -1 means unfiltered/all items
    private bool _itemStacksCached = false;
    private DateTime _itemStacksCacheTime = DateTime.MinValue;
    private long _itemStacksInvalidationCounter = 0; // Tracks invalidation at time of caching
    private const double ITEMSTACK_CACHE_DURATION = 0.8; // 800ms cache duration for ItemStack lists

    public DateTime CreatedAt { get; }

    private StorageAccessContext()
    {
        Config = ConfigSnapshot.Current;

        // Create WorldPlayerContext first - if this fails, the entire context creation should fail
        WorldPlayerContext = WorldPlayerContext.TryCreate(nameof(StorageAccessContext));
        if (WorldPlayerContext == null)
        {
            LogUtil.Error($"{nameof(StorageAccessContext)}: Failed to create WorldPlayerContext, aborting context creation.");
            // In this case, we'll continue but the collections will remain empty
            DewCollectors = new List<TileEntityDewCollector>(0);
            Workstations = new List<TileEntityWorkstation>(0);
            Lootables = new List<ITileEntityLootable>(0);
            Vehicles = new List<EntityVehicle>(0);

            // Initialize ItemStack lists as empty
            DewCollectorItems = new List<ItemStack>(0);
            WorkstationItems = new List<ItemStack>(0);
            ContainerItems = new List<ItemStack>(0);
            VehicleItems = new List<ItemStack>(0);

            CreatedAt = DateTime.Now;
            return;
        }

        InitSourceCollections();
        DiscoverSources();

        InitItemStackLists();

        CreatedAt = DateTime.Now;
        LogUtil.DebugLog($"StorageAccessContext created: {Lootables.Count} lootables, {DewCollectors.Count} dew collectors, {Workstations.Count} workstations, {Vehicles.Count} vehicles");
    }

    private void InitSourceCollections()
    {
        // Initialize collections with appropriate capacity - will be populated when discovery methods are called
        DewCollectors = ListProvider.GetEmptyDewCollectorList();
        Workstations = ListProvider.GetEmptyWorkstationList();
        Lootables = ListProvider.GetEmptyLootableList();
        Vehicles = ListProvider.GetEmptyVehicleList();
    }

    private void DiscoverSources()
    {
        DiscoverTileEntitySources();
        DiscoverVehicleStorages();
    }

    private void InitItemStackLists()
    {
        // Initialize ItemStack lists - will be populated when GetPullableSourceItemStacks is called
        DewCollectorItems = new List<ItemStack>();
        WorkstationItems = new List<ItemStack>();
        ContainerItems = new List<ItemStack>();
        VehicleItems = new List<ItemStack>();
    }

    public bool IsFiltered => _lastFilterItemType >= 0;

    public int CurrentFilterType => _lastFilterItemType;

    /// <summary>
    /// Checks if the global invalidation has occurred since ItemStacks were cached.
    /// </summary>
    private bool HasGlobalInvalidationOccurred()
    {
        return s_globalInvalidationCounter != _itemStacksInvalidationCounter;
    }

    /// <summary>
    /// Checks if the ItemStack lists are cached and valid for the given filter.
    /// </summary>
    /// <param name="filterItem">The filter item to check against cached results</param>
    /// <returns>True if cached results are valid for this filter</returns>
    public bool IsCachedForFilter(ItemValue filterItem)
    {
        if (!_itemStacksCached)
        {
            return false;
        }

        // Check if global invalidation has occurred since we cached our ItemStacks
        if (HasGlobalInvalidationOccurred())
        {
            InvalidateItemStacksCache();
            return false;
        }

        // Check if cache has expired
        var cacheAge = (DateTime.Now - _itemStacksCacheTime).TotalSeconds;
        if (cacheAge > ITEMSTACK_CACHE_DURATION)
        {
            _itemStacksCached = false;
            return false;
        }

        // Check if filter matches: -1 means unfiltered/all items
        var filterType = filterItem?.type ?? -1;
        return _lastFilterItemType == filterType;
    }

    /// <summary>
    /// Marks the ItemStack lists as cached for the given filter.
    /// </summary>
    /// <param name="filterItem">The filter item used to populate the lists, or null for unfiltered</param>
    public void MarkItemStacksCached(ItemValue filterItem)
    {
        _lastFilterItemType = filterItem?.type ?? -1; // -1 means unfiltered/all items
        _itemStacksCached = true;
        _itemStacksCacheTime = DateTime.Now;
        _itemStacksInvalidationCounter = s_globalInvalidationCounter; // Capture current invalidation state
    }

    /// <summary>
    /// Clears all ItemStack lists and invalidates cache. Should be called before repopulating with new filter criteria.
    /// </summary>
    public void ClearItemStacks()
    {
        DewCollectorItems.Clear();
        WorkstationItems.Clear();
        ContainerItems.Clear();
        VehicleItems.Clear();

        // Invalidate cache
        _itemStacksCached = false;
        _lastFilterItemType = -1; // Reset to unfiltered
        _itemStacksCacheTime = DateTime.MinValue;
        _itemStacksInvalidationCounter = s_globalInvalidationCounter; // Reset to current state
    }

    /// <summary>
    /// Forces invalidation of the ItemStack cache without clearing the lists.
    /// Use when you know the underlying data has changed but want to keep current lists for reference.
    /// </summary>
    public void InvalidateItemStacksCache()
    {
        _itemStacksCached = false;
        _lastFilterItemType = -1; // Reset to unfiltered
        _itemStacksCacheTime = DateTime.MinValue;
        _itemStacksInvalidationCounter = s_globalInvalidationCounter; // Reset to current state
    }

    /// <summary>
    /// Gets enhanced cache information including filter awareness.
    /// </summary>
    public string GetItemStackCacheInfo()
    {
        if (!_itemStacksCached)
        {
            return "ItemStacks: Not cached";
        }

        var cacheAge = (DateTime.Now - _itemStacksCacheTime).TotalSeconds;
        var isValid = cacheAge <= ITEMSTACK_CACHE_DURATION && !HasGlobalInvalidationOccurred();

        var filterInfo = IsFiltered
            ? $"filtered for type:{_lastFilterItemType}"
            : "unfiltered (all items)";

        var globalInvalidationInfo = HasGlobalInvalidationOccurred() ? ", globally invalidated" : "";

        return $"ItemStacks: Cached {cacheAge:F2}s ago, {filterInfo}, valid:{isValid}{globalInvalidationInfo}";
    }

    /// <summary>
    /// Gets the total count of items across all ItemStack lists.
    /// </summary>
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var stack in DewCollectorItems)
        {
            total += stack.count;
        }

        foreach (var stack in WorkstationItems)
        {
            total += stack.count;
        }

        foreach (var stack in ContainerItems)
        {
            total += stack.count;
        }

        foreach (var stack in VehicleItems)
        {
            total += stack.count;
        }

        return total;
    }

    /// <summary>
    /// Gets the total number of stacks across all ItemStack lists.
    /// </summary>
    public int GetTotalStackCount()
    {
        return DewCollectorItems.Count + WorkstationItems.Count + ContainerItems.Count + VehicleItems.Count;
    }

    /// <summary>
    /// Creates a concatenated list of all ItemStacks in order: DewCollector → Workstation → Container → Vehicle.
    /// </summary>
    public List<ItemStack> GetAllItemStacks()
    {
        var totalStacks = GetTotalStackCount();
        var result = new List<ItemStack>(totalStacks);

        result.AddRange(DewCollectorItems);
        result.AddRange(WorkstationItems);
        result.AddRange(ContainerItems);
        result.AddRange(VehicleItems);

        return result;
    }

    /// <summary>
    /// Checks if the current cached data is compatible with the specified filter.
    /// Returns true if data can be reused (either exact match or cached data is more general).
    /// </summary>
    /// <param name="requestedFilterItem">The filter being requested</param>
    /// <returns>True if cached data is compatible</returns>
    public bool IsFilterCompatible(ItemValue requestedFilterItem)
    {
        if (!_itemStacksCached)
        {
            return false;
        }

        var requestedFilterType = requestedFilterItem?.type ?? -1;

        // If we have unfiltered data (-1), it's compatible with any filter request
        if (_lastFilterItemType == -1)
        {
            return true;
        }

        // If we have filtered data, it's only compatible with the exact same filter
        return _lastFilterItemType == requestedFilterType;
    }

    /// <summary>
    /// Gets filtering statistics for performance analysis.
    /// </summary>
    /// <returns>String with filtering statistics</returns>
    public string GetFilteringStats()
    {
        var cacheInfo = _itemStacksCached ? "cached" : "not cached";
        var filterStatus = IsFiltered ? $"filtered (type {_lastFilterItemType})" : "unfiltered";
        var itemCount = GetTotalItemCount();
        var stackCount = GetTotalStackCount();

        return $"Filter stats: {cacheInfo}, {filterStatus}, {itemCount} items in {stackCount} stacks";
    }

    /// <summary>
    /// Discovers and populates tile entity sources (containers, dew collectors, workstations).
    /// </summary>
    private void DiscoverTileEntitySources()
    {
        if (WorldPlayerContext == null)
        {
            LogUtil.Error($"{nameof(DiscoverTileEntitySources)}: WorldPlayerContext is null, aborting.");
            return;
        }

        AddPullableTileEntities();
    }

    /// <summary>
    /// Adds pullable tile entities to the appropriate collections based on configuration settings.
    /// </summary>
    private void AddPullableTileEntities()
    {
        const string d_MethodName = nameof(AddPullableTileEntities);

        if (WorldPlayerContext == null)
        {
            LogUtil.Error($"{d_MethodName}: WorldPlayerContext is null, aborting.");
            return;
        }

        var config = Config;
        var worldPlayerContext = WorldPlayerContext;
        var dewCollectors = DewCollectors;
        var workstations = Workstations;
        var lootables = Lootables;

        int chunksProcessed = 0;
        int nullChunks = 0;
        int tileEntitiesProcessed = 0;

        foreach (var chunk in worldPlayerContext.ChunkCacheCopy)
        {
            if (chunk == null)
            {
                nullChunks++;
                continue;
            }

            chunksProcessed++;

            var tileEntityList = chunk.tileEntities?.list;
            if (tileEntityList == null)
            {
                continue;
            }

            foreach (var tileEntity in tileEntityList)
            {
                tileEntitiesProcessed++;

                // Skip if being removed
                if (tileEntity.IsRemoving)
                {
                    continue;
                }

                // 1. Type checks first (cheapest) - cache both interface queries
                bool isLootable = tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable);
                bool hasStorageFeature = config.OnlyStorageCrates ? tileEntity.TryGetSelfOrFeature(out TEFeatureStorage _) : true;

                if (!(tileEntity is TileEntityDewCollector ||
                      tileEntity is TileEntityWorkstation ||
                      isLootable))
                {
                    continue;
                }

                // 2. Then positional checks
                var tileEntityWorldPos = tileEntity.ToWorldPos();

                // Locked check (skip if locked by another player)
                if (ContainerUtils.LockedTileEntities.Count > 0)
                {
                    if (ContainerUtils.LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) && entityId != worldPlayerContext.PlayerEntityId)
                    {
                        continue;
                    }
                }

                // Range check first for early exit
                if (!worldPlayerContext.IsWithinRange(tileEntityWorldPos, config.Range))
                {
                    continue;
                }

                // Lockable check (skip if locked and not allowed)
                if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                {
                    if (!worldPlayerContext.CanAccessLockable(tileLockable))
                    {
                        continue;
                    }
                }

                // DEW COLLECTOR check
                if (config.PullFromDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
                {
                    // Skip if any player is currently accessing the dew collector
                    if (dewCollector.bUserAccessing)
                    {
                        continue;
                    }

                    if (dewCollector.items?.Length <= 0 || !dewCollector.items.Any(item => item?.count > 0))
                    {
                        continue;
                    }

                    dewCollectors.Add(dewCollector);
                    continue;
                }

                // WORKSTATION check  
                if (config.PullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                {
                    // Only player-placed workstations
                    if (!workstation.IsPlayerPlaced)
                    {
                        continue;
                    }

                    if (workstation.output?.Length <= 0 || !workstation.output.Any(item => item?.count > 0))
                    {
                        continue;
                    }

                    workstations.Add(workstation);
                    continue;
                }

                // LOOTABLE (Containers) check
                if (lootable != null)
                {
                    // Must be player storage
                    if (!lootable.bPlayerStorage)
                    {
                        continue;
                    }

                    if (config.OnlyStorageCrates && !hasStorageFeature)
                    {
                        continue;
                    }

                    if (lootable.items?.Length <= 0 || !lootable.items.Any(item => item?.count > 0))
                    {
                        continue;
                    }

                    lootables.Add(lootable);
                    continue;
                }
            }
        }

        LogUtil.DebugLog($"{d_MethodName}: Processed {chunksProcessed} chunks, {nullChunks} null chunks, {tileEntitiesProcessed} tile entities");
    }

    /// <summary>
    /// Discovers and populates available vehicle storages.
    /// </summary>
    private void DiscoverVehicleStorages()
    {
        const string d_MethodName = nameof(DiscoverVehicleStorages);

        if (WorldPlayerContext == null)
        {
            LogUtil.Error($"{d_MethodName}: WorldPlayerContext is null, aborting.");
            return;
        }

        var configRange = Config.Range;

        var vehicles = VehicleManager.Instance?.vehiclesActive;
        if (vehicles == null)
        {
            LogUtil.Error($"{d_MethodName}: VehicleManager returned null list, aborting.");
            return;
        }

        foreach (var vehicle in vehicles)
        {
            // Must have storage and a non-empty bag
            if (vehicle.bag == null || vehicle.bag.IsEmpty() || !vehicle.hasStorage())
            {
                continue;
            }

            // Range check using WorldPlayerContext
            if (!WorldPlayerContext.IsWithinRange(vehicle.position, configRange))
            {
                continue;
            }

            // Locked for player check
            if (vehicle.IsLockedForLocalPlayer(WorldPlayerContext.Player))
            {
                continue;
            }

            Vehicles.Add(vehicle);
        }
    }

    /// <summary>
    /// Creates or retrieves a cached StorageAccessContext instance.
    /// Uses TimeBasedCache to avoid expensive context creation operations.
    /// </summary>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <param name="forceRefresh">If true, bypasses cache and creates fresh context</param>
    /// <returns>A valid StorageAccessContext or null if creation failed</returns>
    public static StorageAccessContext Create(string methodName = "Unknown", bool forceRefresh = false)
    {
        return s_contextCache.GetOrCreate(() => CreateFresh(methodName), forceRefresh, methodName);
    }

    /// <summary>
    /// Forces cache invalidation for StorageAccessContext instances.
    /// Call this when world state changes significantly.
    /// Also invalidates ItemStack caches in all existing instances.
    /// </summary>
    public static void InvalidateCache()
    {
        s_contextCache.InvalidateCache();

        // Increment global invalidation counter to invalidate ItemStack caches in existing instances
        s_globalInvalidationCounter++;

        LogUtil.DebugLog($"StorageAccessContext cache invalidated (global invalidation counter: {s_globalInvalidationCounter})");
    }

    /// <summary>
    /// Gets the age of the current cached context in seconds.
    /// Returns -1 if no cached context exists.
    /// </summary>
    public static double GetCacheAge()
    {
        return s_contextCache.GetCacheAge();
    }

    /// <summary>
    /// Checks if the cache currently has a valid (non-expired) context.
    /// </summary>
    public static bool HasValidCachedContext()
    {
        return s_contextCache.HasValidCachedItem();
    }

    /// <summary>
    /// Gets cache statistics for diagnostics.
    /// </summary>
    public static string GetCacheStats()
    {
        return s_contextCache.GetCacheStats();
    }

    private static StorageAccessContext CreateFresh(string methodName)
    {
        try
        {
            var context = new StorageAccessContext();

            if (context.WorldPlayerContext == null)
            {
                LogUtil.Error($"{methodName}: Created StorageAccessContext with null WorldPlayerContext");
                return null;
            }

            LogUtil.DebugLog($"{methodName}: Created fresh StorageAccessContext with {context.GetSourceSummary()}");
            return context;
        }
        catch (Exception ex)
        {
            LogUtil.Error($"{methodName}: Exception creating StorageAccessContext: {ex.Message}");
            return null;
        }
    }

    public double AgeInSeconds => (DateTime.Now - CreatedAt).TotalSeconds;

    public bool HasExpired(double lifetimeSeconds) => AgeInSeconds > lifetimeSeconds;

    public string GetSourceSummary()
    {
        return $"Lootables: {Lootables.Count}, DewCollectors: {DewCollectors.Count}, Workstations: {Workstations.Count}, Vehicles: {Vehicles.Count}, Age: {AgeInSeconds:F1}s";
    }

    /// <summary>
    /// Gets a summary including ItemStack counts and enhanced cache info.
    /// </summary>
    public string GetItemStackSummary()
    {
        var cacheInfo = GetItemStackCacheInfo();
        var filterStats = GetFilteringStats();
        return $"ItemStacks - DC:{DewCollectorItems.Count}, WS:{WorkstationItems.Count}, CT:{ContainerItems.Count}, VH:{VehicleItems.Count}, Total:{GetTotalStackCount()} stacks, {GetTotalItemCount()} items | {cacheInfo} | {filterStats}";
    }

    /// <summary>
    /// Gets comprehensive cache information including nested WorldPlayerContext cache stats.
    /// </summary>
    public static string GetComprehensiveCacheStats()
    {
        var contextStats = s_contextCache.GetCacheStats();
        var worldPlayerStats = WorldPlayerContext.GetCacheStats();
        return $"StorageAccessContext: {contextStats} | WorldPlayerContext: {worldPlayerStats} | Global invalidations: {s_globalInvalidationCounter}";
    }
}