using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Caching;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Provides access to storage sources and operations within a specific context.
/// This class serves as a facade that coordinates between various storage services.
/// </summary>
public sealed class StorageAccessContext
{
    internal ConfigSnapshot Config { get; }
    internal WorldPlayerContext WorldPlayerContext { get; }
    internal StorageSourceCollection Sources { get; }
    internal ItemStackCacheManager CacheManager { get; }

    private DateTime CreatedAt { get; }

    // Legacy properties for backward compatibility - delegate to Sources
    private List<TileEntityDewCollector> DewCollectors => Sources.DewCollectors;
    private List<ITileEntityLootable> Lootables => Sources.Lootables;
    private List<EntityVehicle> Vehicles => Sources.Vehicles;
    private List<TileEntityWorkstation> Workstations => Sources.Workstations;

    private List<ItemStack> DewCollectorItems => Sources.DewCollectorItems;
    private List<ItemStack> WorkstationItems => Sources.WorkstationItems;
    private List<ItemStack> ContainerItems => Sources.ContainerItems;
    private List<ItemStack> VehicleItems => Sources.VehicleItems;

    // Legacy properties for backward compatibility - delegate to CacheManager
    public bool IsFiltered => CacheManager.IsFiltered;
    public UniqueItemTypes CurrentFilterTypes => CacheManager.CurrentFilterTypes;

    // Factory method delegation
    public static StorageAccessContext Create(string methodName = "Unknown", bool forceRefresh = false)
    {
        return StorageContextFactory.Create(methodName, forceRefresh);
    }

    /// <summary>
    /// Internal constructor used by StorageContextFactory.
    /// </summary>
    internal StorageAccessContext(ConfigSnapshot config, WorldPlayerContext worldPlayerContext, StorageSourceCollection sources, ItemStackCacheManager cacheManager)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        WorldPlayerContext = worldPlayerContext ?? throw new ArgumentNullException(nameof(worldPlayerContext));
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        CacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        CreatedAt = DateTime.Now;

        ModLogger.DebugLog($"StorageAccessContext created: {Sources.GetSourceSummary()}");
    }

    #region Cache Management
    public bool IsCachedForFilter(UniqueItemTypes filterTypes)
    {
        return CacheManager.IsCachedForFilter(filterTypes);
    }

    public bool IsCachedForFilter(ItemValue filterItem)
    {
        return CacheManager.IsCachedForFilter(filterItem);
    }

    public string GetItemStackCacheInfo()
    {
        return CacheManager.GetCacheInfo();
    }

    public static void InvalidateCache()
    {
        StorageContextFactory.InvalidateCache();
    }

    public static double GetCacheAge()
    {
        return StorageContextFactory.GetCacheAge();
    }

    public static bool HasValidCachedContext()
    {
        return StorageContextFactory.HasValidCachedContext();
    }

    public static string GetCacheStats()
    {
        return StorageContextFactory.GetCacheStats();
    }
    #endregion

    #region Query Operations - Delegate to StorageQueryService
    public int GetTotalItemCount()
    {
        return StorageQueryService.GetTotalItemCount(Sources, Config, CacheManager);
    }

    public int GetTotalStackCount()
    {
        return StorageQueryService.GetTotalStackCount(Sources, Config, CacheManager);
    }

    public List<ItemStack> GetAllAvailableItemStacks(UniqueItemTypes filterTypes)
    {
        return StorageQueryService.GetAllAvailableItemStacks(Sources, Config, CacheManager, filterTypes);
    }

    // Fixed: Consistent delegation pattern
    public string GetFilteringStats()
    {
        // Ensure ItemStacks are pulled through consistent service call
        GetTotalItemCount(); // This ensures extraction happens
        return ItemStackExtractionService.GetExtractionStats(Sources, CacheManager);
    }

    public int GetItemCount(ItemValue itemValue)
    {
        return StorageQueryService.GetItemCount(Sources, Config, CacheManager, itemValue);
    }

    public int GetItemCount(UniqueItemTypes filterTypes)
    {
        return StorageQueryService.GetItemCount(Sources, Config, CacheManager, filterTypes);
    }

    public bool HasItem(ItemValue itemValue)
    {
        return StorageQueryService.HasItem(Sources, Config, CacheManager, itemValue);
    }

    public bool HasItem(UniqueItemTypes filterTypes)
    {
        return StorageQueryService.HasItem(Sources, Config, CacheManager, filterTypes);
    }
    #endregion

    #region Removal Operations - Delegate to StorageItemRemovalService
    public int RemoveRemaining(ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        return StorageItemRemovalService.RemoveItems(Sources, Config, itemValue, stillNeeded, ignoreModdedItems, removedItems);
    }
    #endregion

    #region Diagnostics and Statistics
    public double AgeInSeconds => (DateTime.Now - CreatedAt).TotalSeconds;

    // Fixed: Changed return type from object to double for consistency
    public double WorldPlayerContextAgeInSeconds => WorldPlayerContext?.AgeInSeconds ?? -1;

    public bool HasExpired(double lifetimeSeconds) => AgeInSeconds > lifetimeSeconds;

    public string GetSourceSummary()
    {
        return $"{Sources.GetSourceSummary()}, Age: {AgeInSeconds:F1}s";
    }

    // Fixed: Consistent facade pattern - use facade methods instead of direct service calls
    public string GetItemStackSummary()
    {
        var cacheInfo = GetItemStackCacheInfo();
        var filterStats = GetFilteringStats();
        var totalItems = GetTotalItemCount(); // Use facade method instead of direct service call
        return $"{Sources.GetItemStackSummary()}, {totalItems} items | {cacheInfo} | {filterStats}";
    }

    public static string GetComprehensiveCacheStats()
    {
        var contextStats = StorageContextFactory.GetCacheStats();
        var worldPlayerStats = WorldPlayerContext.GetCacheStats();
        var itemPropsStats = ItemPropertiesCache.GetCacheStats();
        var globalInvalidations = ItemStackCacheManager.GetGlobalInvalidationCounter();
        return $"StorageAccessContext: {contextStats} | WorldPlayerContext: {worldPlayerStats} | {itemPropsStats} | Global invalidations: {globalInvalidations}";
    }

    // Option 1: Remove this redundant method entirely and use StorageContextFactory.IsValidContext directly
    // Option 2: Add specific validation logic here if needed
    internal static bool IsValidContext(StorageAccessContext context)
    {
        return StorageContextFactory.IsValidContext(context);
    }
    #endregion
}