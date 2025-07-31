using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public sealed class StorageAccessContext
{
    private const double DEFAULT_CACHE_DURATION = 0.5;

    private static readonly TimeBasedCache<StorageAccessContext> s_contextCache = new(DEFAULT_CACHE_DURATION, nameof(StorageAccessContext));

    private static long s_globalInvalidationCounter = 0;

    public ConfigSnapshot Config { get; }
    public WorldPlayerContext WorldPlayerContext { get; }

    private List<TileEntityDewCollector> DewCollectors { get; set; }
    private List<ITileEntityLootable> Lootables { get; set; }
    private List<EntityVehicle> Vehicles { get; set; }
    private List<TileEntityWorkstation> Workstations { get; set; }

    private List<ItemStack> DewCollectorItems { get; set; }
    private List<ItemStack> WorkstationItems { get; set; }
    private List<ItemStack> ContainerItems { get; set; }
    private List<ItemStack> VehicleItems { get; set; }

    private UniqueItemTypes _lastFilterTypes = UniqueItemTypes.Unfiltered;
    private bool _itemStacksCached = false;
    private DateTime _itemStacksCacheTime = DateTime.MinValue;
    private long _itemStacksInvalidationCounter = 0;
    private const double ITEMSTACK_CACHE_DURATION = 0.8;

    public DateTime CreatedAt { get; }

    public static StorageAccessContext Create(string methodName = "Unknown", bool forceRefresh = false)
    {
        return s_contextCache.GetOrCreate(() => CreateFresh(methodName), forceRefresh, methodName);
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

    private StorageAccessContext()
    {
        Config = ConfigSnapshot.Current;

        WorldPlayerContext = WorldPlayerContext.TryCreate(nameof(StorageAccessContext));
        if (WorldPlayerContext == null)
        {
            LogUtil.Error($"{nameof(StorageAccessContext)}: Failed to create WorldPlayerContext, aborting context creation.");
            DewCollectors = new List<TileEntityDewCollector>(0);
            Workstations = new List<TileEntityWorkstation>(0);
            Lootables = new List<ITileEntityLootable>(0);
            Vehicles = new List<EntityVehicle>(0);

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
        DewCollectorItems = new List<ItemStack>();
        WorkstationItems = new List<ItemStack>();
        ContainerItems = new List<ItemStack>();
        VehicleItems = new List<ItemStack>();
    }

    public bool IsFiltered => _lastFilterTypes.IsFiltered;

    public UniqueItemTypes CurrentFilterTypes => _lastFilterTypes;

    private bool HasGlobalInvalidationOccurred()
    {
        return s_globalInvalidationCounter != _itemStacksInvalidationCounter;
    }

    public bool IsCachedForFilter(UniqueItemTypes filterTypes)
    {
        if (!_itemStacksCached)
        {
            return false;
        }

        if (HasGlobalInvalidationOccurred())
        {
            InvalidateItemStacksCache();
            return false;
        }

        var cacheAge = (DateTime.Now - _itemStacksCacheTime).TotalSeconds;
        if (cacheAge > ITEMSTACK_CACHE_DURATION)
        {
            _itemStacksCached = false;
            return false;
        }

        if (ReferenceEquals(_lastFilterTypes, filterTypes))
        {
            return true;
        }

        return AreFilterTypesEquivalent(_lastFilterTypes, filterTypes);
    }

    public bool IsCachedForFilter(ItemValue filterItem)
    {
        var filterTypes = filterItem != null
            ? UniqueItemTypes.FromItemType(filterItem.type)
            : UniqueItemTypes.Unfiltered;

        return IsCachedForFilter(filterTypes);
    }

    private bool AreFilterTypesEquivalent(UniqueItemTypes cached, UniqueItemTypes requested)
    {
        if (cached == null || requested == null)
        {
            return cached == requested;
        }

        if (cached.IsUnfiltered && requested.IsUnfiltered)
        {
            return true;
        }

        if (cached.IsUnfiltered != requested.IsUnfiltered)
        {
            return false;
        }

        if (cached.Count != requested.Count)
        {
            return false;
        }

        foreach (int type in requested)
        {
            if (!cached.Contains(type))
            {
                return false;
            }
        }

        return true;
    }

    public void MarkItemStacksCached(UniqueItemTypes filterTypes)
    {
        _lastFilterTypes = filterTypes ?? UniqueItemTypes.Unfiltered;
        _itemStacksCached = true;
        _itemStacksCacheTime = DateTime.Now;
        _itemStacksInvalidationCounter = s_globalInvalidationCounter;
    }

    public void MarkItemStacksCached(ItemValue filterItem)
    {
        var filterTypes = filterItem != null
            ? UniqueItemTypes.FromItemType(filterItem.type)
            : UniqueItemTypes.Unfiltered;

        MarkItemStacksCached(filterTypes);
    }

    public void ClearItemStacks()
    {
        DewCollectorItems.Clear();
        WorkstationItems.Clear();
        ContainerItems.Clear();
        VehicleItems.Clear();

        _itemStacksCached = false;
        _lastFilterTypes = UniqueItemTypes.Unfiltered;
        _itemStacksCacheTime = DateTime.MinValue;
        _itemStacksInvalidationCounter = s_globalInvalidationCounter;
    }

    public void InvalidateItemStacksCache()
    {
        _itemStacksCached = false;
        _lastFilterTypes = UniqueItemTypes.Unfiltered;
        _itemStacksCacheTime = DateTime.MinValue;
        _itemStacksInvalidationCounter = s_globalInvalidationCounter;
    }

    public string GetItemStackCacheInfo()
    {
        if (!_itemStacksCached)
        {
            return "ItemStacks: Not cached";
        }

        var cacheAge = (DateTime.Now - _itemStacksCacheTime).TotalSeconds;
        var isValid = cacheAge <= ITEMSTACK_CACHE_DURATION && !HasGlobalInvalidationOccurred();

        var filterInfo = _lastFilterTypes.IsFiltered
            ? $"filtered for {_lastFilterTypes.Count} types"
            : "unfiltered (all items)";

        var globalInvalidationInfo = HasGlobalInvalidationOccurred() ? ", globally invalidated" : "";

        return $"ItemStacks: Cached {cacheAge:F2}s ago, {filterInfo}, valid:{isValid}{globalInvalidationInfo}";
    }

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

    public int GetTotalStackCount()
    {
        return DewCollectorItems.Count + WorkstationItems.Count + ContainerItems.Count + VehicleItems.Count;
    }

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

    public bool IsFilterCompatible(UniqueItemTypes requestedFilterTypes)
    {
        if (!_itemStacksCached)
        {
            return false;
        }

        if (_lastFilterTypes.IsUnfiltered)
        {
            return true;
        }

        if (requestedFilterTypes == null || requestedFilterTypes.IsUnfiltered)
        {
            return false;
        }

        foreach (int type in requestedFilterTypes)
        {
            if (!_lastFilterTypes.Contains(type))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsFilterCompatible(ItemValue requestedFilterItem)
    {
        var requestedFilterTypes = requestedFilterItem != null
            ? UniqueItemTypes.FromItemType(requestedFilterItem.type)
            : UniqueItemTypes.Unfiltered;

        return IsFilterCompatible(requestedFilterTypes);
    }

    public string GetFilteringStats()
    {
        var cacheInfo = _itemStacksCached ? "cached" : "not cached";
        var filterStatus = _lastFilterTypes.IsFiltered
            ? $"filtered ({_lastFilterTypes.Count} types)"
            : "unfiltered";
        var itemCount = GetTotalItemCount();
        var stackCount = GetTotalStackCount();

        return $"Filter stats: {cacheInfo}, {filterStatus}, {itemCount} items in {stackCount} stacks";
    }

    public int GetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return 0;
        }

        PullSourceItemStacks(out var totalItemCountAdded, filterItem: itemValue);

        LogUtil.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} of '{itemValue.ItemClass?.Name}'");

        return totalItemCountAdded;
    }

    public int GetItemCount(UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (filterTypes == null)
        {
            filterTypes = UniqueItemTypes.Unfiltered;
        }

        PullSourceItemStacks(out var totalItemCountAdded, filterTypes);

        LogUtil.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} items with filter: {filterTypes}");

        return totalItemCountAdded;
    }

    public bool HasItem(ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return false;
        }

        var totalItemCount = GetItemCount(itemValue);
        var result = totalItemCount > 0;

        LogUtil.DebugLog($"{d_MethodName} for '{itemValue?.ItemClass?.Name}' is {result}");

        return result;
    }

    public bool HasItem(UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(HasItem);

        if (filterTypes == null)
        {
            filterTypes = UniqueItemTypes.Unfiltered;
        }

        var totalItemCount = GetItemCount(filterTypes);
        var result = totalItemCount > 0;

        LogUtil.DebugLog($"{d_MethodName} with filter: {filterTypes} is {result}");

        return result;
    }

    public int RemoveRemaining(ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(RemoveRemaining);

        if (stillNeeded <= 0 || itemValue == null || itemValue.ItemClass == null || itemValue.type <= 0)
        {
#if DEBUG
            LogUtil.Error($"{d_MethodName} | stillNeeded {stillNeeded}; item null is {itemValue == null}");
#endif
            return 0;
        }

        var itemName = itemValue.ItemClass.GetItemName();
        LogUtil.DebugLog($"{d_MethodName} | Trying to remove {stillNeeded} {itemName}");

        int originalNeeded = stillNeeded;

        var config = Config;

        if (stillNeeded > 0 && config.PullFromDewCollectors)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "DewCollectors", itemName, DewCollectors, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                dewCollector => dewCollector.items, dewCollector => DewCollectorUtils.MarkDewCollectorModified(dewCollector));
        }

        if (stillNeeded > 0 && config.PullFromWorkstationOutputs)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "WorkstationOutputs", itemName, Workstations, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                workstation => workstation.output, workstation => WorkstationUtils.MarkWorkstationModified(workstation));
        }

        if (stillNeeded > 0)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "Containers", itemName, Lootables, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                lootable => lootable.items, lootable => lootable.SetModified());
        }

        if (stillNeeded > 0 && config.PullFromVehicleStorage)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "Vehicles", itemName, Vehicles, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                vehicle => vehicle.bag.items, vehicle => vehicle.SetBagModified());
        }

        return originalNeeded - stillNeeded;
    }

    private void RemoveItemsFromStorageInternal<T>(
        string d_method_name,
        string storageName,
        string itemName,
        List<T> storages,
        ItemValue itemValue,
        ref int stillNeeded,
        bool ignoreModdedItems,
        IList<ItemStack> removedItems,
        Func<T, IEnumerable<ItemStack>> getItems,
        Action<T> markModified)
    {
        int originalNeeded = stillNeeded;

        foreach (var storage in storages)
        {
            if (stillNeeded <= 0)
            {
                break;
            }

            int newNeeded = RemoveItemsInternal(getItems(storage), itemValue, stillNeeded, ignoreModdedItems, removedItems);
            if (stillNeeded != newNeeded)
            {
                markModified(storage);
                stillNeeded = newNeeded;
            }
        }

        int removed = originalNeeded - stillNeeded;
        LogUtil.DebugLog($"{d_method_name} | {storageName} | Removed {removed} {itemName}, stillNeeded {stillNeeded}");

#if DEBUG
        if (stillNeeded < 0)
        {
            LogUtil.Error($"{d_method_name} | stillNeeded after {storageName} should not be negative, but is {stillNeeded}");
            stillNeeded = 0;
        }
#endif
    }

    private int RemoveItemsInternal(IEnumerable<ItemStack> items, ItemValue desiredItem, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        int filterType = desiredItem.type;
        bool itemCanStack = ItemClass.GetForId(filterType).CanStack();

        foreach (var stack in items)
        {
            if (stillNeeded <= 0)
            {
                break;
            }

            if (stack?.count <= 0)
            {
                continue;
            }

            var itemValue = stack.itemValue;
            if (itemValue?.type != filterType)
            {
                continue;
            }

            if (ignoreModdedItems && itemValue.HasModSlots && itemValue.HasMods())
            {
                continue;
            }

            if (itemCanStack)
            {
                var countToRemove = Math.Min(stack.count, stillNeeded);
                removedItems?.Add(new ItemStack(itemValue.Clone(), countToRemove));
                stack.count -= countToRemove;
                stillNeeded -= countToRemove;
                if (stack.count <= 0)
                {
                    stack.Clear();
                }
            }
            else
            {
                removedItems?.Add(stack.Clone());
                stack.Clear();
                --stillNeeded;
            }
        }

        return stillNeeded;
    }

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

                if (tileEntity.IsRemoving)
                {
                    continue;
                }

                bool isLootable = tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable);
                bool hasStorageFeature = config.OnlyStorageCrates ? tileEntity.TryGetSelfOrFeature(out TEFeatureStorage _) : true;

                if (!(tileEntity is TileEntityDewCollector ||
                      tileEntity is TileEntityWorkstation ||
                      isLootable))
                {
                    continue;
                }

                var tileEntityWorldPos = tileEntity.ToWorldPos();

                if (ContainerUtils.LockedTileEntities.Count > 0)
                {
                    if (ContainerUtils.LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) && entityId != worldPlayerContext.PlayerEntityId)
                    {
                        continue;
                    }
                }

                if (!worldPlayerContext.IsWithinRange(tileEntityWorldPos, config.Range))
                {
                    continue;
                }

                if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                {
                    if (!worldPlayerContext.CanAccessLockable(tileLockable))
                    {
                        continue;
                    }
                }

                if (config.PullFromDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
                {
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

                if (config.PullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                {
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

                if (lootable != null)
                {
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
            if (vehicle.bag == null || vehicle.bag.IsEmpty() || !vehicle.hasStorage())
            {
                continue;
            }

            if (!WorldPlayerContext.IsWithinRange(vehicle.position, configRange))
            {
                continue;
            }

            if (vehicle.IsLockedForLocalPlayer(WorldPlayerContext.Player))
            {
                continue;
            }

            Vehicles.Add(vehicle);
        }
    }

    public void PullSourceItemStacks(out int totalItemsAddedCount, ItemValue filterItem = null)
    {
        var filterTypes = filterItem != null
            ? UniqueItemTypes.FromItemType(filterItem.type)
            : UniqueItemTypes.Unfiltered;

        PullSourceItemStacks(out totalItemsAddedCount, filterTypes);
    }

    public void PullSourceItemStacks(out int totalItemsAddedCount, UniqueItemTypes filterTypes = null)
    {
        const string d_MethodName = nameof(PullSourceItemStacks);

        totalItemsAddedCount = 0;
        filterTypes ??= UniqueItemTypes.Unfiltered;

        if (IsCachedForFilter(filterTypes))
        {
            totalItemsAddedCount = GetTotalItemCount();
            var cachedResult = GetAllItemStacks();

            LogUtil.DebugLog($"{d_MethodName}: Using cached ItemStacks, found {totalItemsAddedCount} items from {cachedResult.Count} stacks - DC:{DewCollectorItems.Count}, WS:{WorkstationItems.Count}, CT:{ContainerItems.Count}, VH:{VehicleItems.Count} | {GetItemStackCacheInfo()}");
            return;
        }

        ClearItemStacks();

        if (Config.PullFromDewCollectors)
        {
            AddValidItemStacksFromSources(d_MethodName, DewCollectorItems, DewCollectors, dc => dc.items,
                "Dew Collector Storage", out int dewCollectorItemsAddedCount, filterTypes);

            totalItemsAddedCount += dewCollectorItemsAddedCount;
        }

        if (Config.PullFromWorkstationOutputs)
        {
            AddValidItemStacksFromSources(d_MethodName, WorkstationItems, Workstations, workstation => workstation.output,
                "Workstation Output", out int workstationItemsAddedCount, filterTypes);

            totalItemsAddedCount += workstationItemsAddedCount;
        }

        AddValidItemStacksFromSources(d_MethodName, ContainerItems, Lootables, l => l.items,
            "Container Storage", out int containerItemsAddedCount, filterTypes);

        totalItemsAddedCount += containerItemsAddedCount;

        if (Config.PullFromVehicleStorage)
        {
            AddValidItemStacksFromSources(d_MethodName, VehicleItems, Vehicles, v => v.bag?.GetSlots(),
                "Vehicle Storage", out int vehicleItemsAddedCount, filterTypes);

            totalItemsAddedCount += vehicleItemsAddedCount;
        }

        MarkItemStacksCached(filterTypes);
        LogUtil.DebugLog($"{d_MethodName}: Found {totalItemsAddedCount} items from {GetTotalStackCount()} stacks - DC:{DewCollectorItems.Count}, WS:{WorkstationItems.Count}, CT:{ContainerItems.Count}, VH:{VehicleItems.Count} | {GetItemStackCacheInfo()}");
    }

    private void AddValidItemStacksFromSources<T>(
            string d_MethodName,
            List<ItemStack> output,
            IEnumerable<T> sources,
            Func<T, ItemStack[]> getStacks,
            string sourceName,
            out int itemsAddedCount,
            UniqueItemTypes filterTypes = null) where T : class
    {
        itemsAddedCount = 0;
        filterTypes ??= UniqueItemTypes.Unfiltered;

        if (sources == null)
        {
            LogUtil.Error($"{d_MethodName}: {sourceName} pulled in 0 stacks (null source)");
            return;
        }

        foreach (var source in sources)
        {
            if (source == null)
            {
                continue;
            }

            var stacks = getStacks(source);
            if (stacks == null)
            {
                continue;
            }

            for (int i = 0; i < stacks.Length; i++)
            {
                var stack = stacks[i];
                int stackCount = stack?.count ?? 0;

                if (stackCount <= 0)
                {
                    continue;
                }

                if (filterTypes.IsFiltered && !filterTypes.Contains(stack.itemValue?.type ?? 0))
                {
                    continue;
                }

                output.Add(stack);
                itemsAddedCount += stackCount;
            }
        }
    }

    private void DiscoverTileEntitySources()
    {
        if (WorldPlayerContext == null)
        {
            LogUtil.Error($"{nameof(DiscoverTileEntitySources)}: WorldPlayerContext is null, aborting.");
            return;
        }

        AddPullableTileEntities();
    }

    public static void InvalidateCache()
    {
        s_contextCache.InvalidateCache();

        s_globalInvalidationCounter++;

        LogUtil.DebugLog($"StorageAccessContext cache invalidated (global invalidation counter: {s_globalInvalidationCounter})");
    }

    public static double GetCacheAge()
    {
        return s_contextCache.GetCacheAge();
    }

    public static bool HasValidCachedContext()
    {
        return s_contextCache.HasValidCachedItem();
    }

    public static string GetCacheStats()
    {
        return s_contextCache.GetCacheStats();
    }

    public double AgeInSeconds => (DateTime.Now - CreatedAt).TotalSeconds;

    public bool HasExpired(double lifetimeSeconds) => AgeInSeconds > lifetimeSeconds;

    public string GetSourceSummary()
    {
        return $"Lootables: {Lootables.Count}, DewCollectors: {DewCollectors.Count}, Workstations: {Workstations.Count}, Vehicles: {Vehicles.Count}, Age: {AgeInSeconds:F1}s";
    }

    public string GetItemStackSummary()
    {
        var cacheInfo = GetItemStackCacheInfo();
        var filterStats = GetFilteringStats();
        return $"ItemStacks - DC:{DewCollectorItems.Count}, WS:{WorkstationItems.Count}, CT:{ContainerItems.Count}, VH:{VehicleItems.Count}, Total:{GetTotalStackCount()} stacks, {GetTotalItemCount()} items | {cacheInfo} | {filterStats}";
    }

    public static string GetComprehensiveCacheStats()
    {
        var contextStats = s_contextCache.GetCacheStats();
        var worldPlayerStats = WorldPlayerContext.GetCacheStats();
        return $"StorageAccessContext: {contextStats} | WorldPlayerContext: {worldPlayerStats} | Global invalidations: {s_globalInvalidationCounter}";
    }
}