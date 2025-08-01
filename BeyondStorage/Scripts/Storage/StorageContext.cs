using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Provides access to storage sources and operations within a specific context.
/// This class serves as a facade that coordinates between various storage services.
/// </summary>
public sealed class StorageContext
{
    internal ConfigSnapshot Config { get; }
    internal WorldPlayerContext WorldPlayerContext { get; }
    internal StorageSourceCollection Sources { get; }
    internal ItemStackCacheManager CacheManager { get; }

    private DateTime CreatedAt { get; }

    /// <summary>
    /// Internal constructor used by StorageContextFactory.
    /// </summary>
    internal StorageContext(ConfigSnapshot config, WorldPlayerContext worldPlayerContext, StorageSourceCollection sources, ItemStackCacheManager cacheManager)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        WorldPlayerContext = worldPlayerContext ?? throw new ArgumentNullException(nameof(worldPlayerContext));
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        CacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        CreatedAt = DateTime.Now;

        ModLogger.DebugLog($"StorageContext created: {Sources.GetSourceSummary()}");
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

    public double WorldPlayerContextAgeInSeconds => WorldPlayerContext?.AgeInSeconds ?? -1;

    public bool HasExpired(double lifetimeSeconds) => AgeInSeconds > lifetimeSeconds;

    public string GetSourceSummary()
    {
        return $"{Sources.GetSourceSummary()}, Age: {AgeInSeconds:F1}s";
    }

    public string GetItemStackSummary()
    {
        var cacheInfo = GetItemStackCacheInfo();
        var filterStats = GetFilteringStats();
        var totalItems = GetTotalItemCount();
        return $"{Sources.GetItemStackSummary()}, {totalItems} items | {cacheInfo} | {filterStats}";
    }
    #endregion
}