using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

public sealed class StorageContext
{
    internal ConfigSnapshot Config { get; }
    internal WorldPlayerContext WorldPlayerContext { get; }
    internal StorageDataManager Sources { get; }
    internal ItemStackCacheManager CacheManager { get; }

    private DateTime CreatedAt { get; }

    internal StorageContext(ConfigSnapshot config, WorldPlayerContext worldPlayerContext, StorageDataManager sources, ItemStackCacheManager cacheManager)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        WorldPlayerContext = worldPlayerContext ?? throw new ArgumentNullException(nameof(worldPlayerContext));
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        CacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        CreatedAt = DateTime.Now;

        ModLogger.DebugLog($"StorageContext created: {Sources.GetSourceSummary()}");
    }

    #region Cache Management (Enhanced)

    /// <summary>
    /// Ensures cache is valid for the specified filter, refreshing if necessary.
    /// </summary>
    /// <param name="filter">The filter to validate cache for</param>
    /// <param name="methodName">Calling method name for logging</param>
    /// <returns>True if cache was valid (hit), false if refresh was needed (miss)</returns>
    private bool EnsureCacheValid(UniqueItemTypes filter, string methodName)
    {
        filter ??= UniqueItemTypes.Unfiltered;
        var hit = CacheManager.IsCachedForFilter(filter);

        if (!hit)
        {
            try
            {
                ModLogger.DebugLog($"{methodName} | Cache miss, discovering items for filter: {filter}");

                CacheManager.ClearCache();
                Sources.ClearAll();
                Sources.DataStore.CurrentFilter = filter;

                ItemDiscoveryService.DiscoverItems(this);
                CacheManager.MarkCached(filter);

                ModLogger.DebugLog($"{methodName} | Item discovery completed successfully");
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"{methodName} | Failed during item discovery: {ex.Message}", ex);
                return false;
            }
        }

        var cacheStatus = hit ? "HIT" : "MISS";
        ModLogger.DebugLog($"{methodName} | Cache {cacheStatus} for filter: {filter}");
        return hit;
    }

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
    public IReadOnlyCollection<ItemStack> GetAllAvailableItemStacks(UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(GetAllAvailableItemStacks);
        EnsureCacheValid(filterTypes, d_MethodName);
        return StorageQueryService.GetAllAvailableItemStacks(this, filterTypes);
    }

    public int GetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetItemCount);
        var filter = UniqueItemTypes.FromItemValue(itemValue);
        EnsureCacheValid(filter, d_MethodName);
        return StorageQueryService.GetItemCount(this, itemValue);
    }

    public int GetItemCount(UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(GetItemCount);
        EnsureCacheValid(filterTypes, d_MethodName);
        return StorageQueryService.GetItemCount(this, filterTypes);
    }

    public bool HasItem(ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);
        var filter = UniqueItemTypes.FromItemValue(itemValue);
        EnsureCacheValid(filter, d_MethodName);
        return StorageQueryService.HasItem(this, itemValue);
    }

    public bool HasItem(UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(HasItem);
        EnsureCacheValid(filterTypes, d_MethodName);
        return StorageQueryService.HasItem(this, filterTypes);
    }
    #endregion

    #region Removal Operations - Delegate to StorageItemRemovalService
    public int RemoveRemaining(ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(RemoveRemaining);
        var filter = UniqueItemTypes.FromItemValue(itemValue);
        EnsureCacheValid(filter, d_MethodName);
        return StorageItemRemovalService.RemoveItems(this, itemValue, stillNeeded, ignoreModdedItems, removedItems);
    }
    #endregion

    #region Diagnostics and Statistics
    public double AgeInSeconds => (DateTime.Now - CreatedAt).TotalSeconds;

    public string GetSourceSummary()
    {
        return $"{Sources.GetSourceSummary()}, Age: {AgeInSeconds:F1}s";
    }

    internal IReadOnlyCollection<Type> GetAllowedSourceTypes()
    {
        return Sources.DataStore.GetAllowedSourceTypes();
    }
    #endregion
}