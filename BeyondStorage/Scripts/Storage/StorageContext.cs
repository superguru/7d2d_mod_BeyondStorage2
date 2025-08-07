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
        const string d_MethodName = nameof(StorageContext);

        if (config == null)
        {
            var error = $"{d_MethodName}: {nameof(config)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(config), error);
        }

        if (worldPlayerContext == null)
        {
            var error = $"{d_MethodName}: {nameof(worldPlayerContext)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(worldPlayerContext), error);
        }

        if (sources == null)
        {
            var error = $"{d_MethodName}: {nameof(sources)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(sources), error);
        }

        if (cacheManager == null)
        {
            var error = $"{d_MethodName}: {nameof(cacheManager)} cannot be null.";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(cacheManager), error);
        }

        Config = config;
        WorldPlayerContext = worldPlayerContext;
        Sources = sources;
        CacheManager = cacheManager;
        CreatedAt = DateTime.Now;

        ModLogger.DebugLog($"StorageContext created: {Sources.GetSourceSummary()}");
    }

    #region Cache Management

    /// <summary>
    /// Ensures cache is valid for the specified filter, refreshing if necessary.
    /// </summary>
    /// <param name="filter">The filter to validate cache for</param>
    /// <param name="methodName">Calling method name for logging</param>
    /// <returns>True if cache was valid (hit), false if refresh was needed (miss)</returns>
    private bool EnsureValidCache(string methodName)
    {
        var hit = CacheManager.IsMasterCacheValid();

        if (!hit)
        {
            try
            {
                // Clear data first, then invalidate cache atomically
                Sources.Clear();
                CacheManager.InvalidateCache();

                // Always discover everything for master cache
                ItemDiscoveryService.DiscoverItems(this);
                CacheManager.MarkCached();

                hit = true; // Cache refresh succeeded
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"{methodName}: Failed during item discovery: {ex.Message}", ex);

                // Ensure cache is invalidated on failure and data is cleared
                Sources.Clear();
                CacheManager.InvalidateCache();

                return false;
            }
        }

        //var cacheStatus = hit ? "HIT" : "MISS";
        //ModLogger.DebugLog($"{methodName}: CACHE_CHECK_{cacheStatus}");
        return hit;
    }

    /// <summary>
    /// Gets information about the current cache state.
    /// </summary>
    /// <returns>String containing cache information</returns>
    public string GetItemStackCacheInfo()
    {
        return CacheManager.GetCacheInfo();
    }

    /// <summary>
    /// Gets comprehensive diagnostic information including both cache and data store state.
    /// </summary>
    /// <returns>String containing comprehensive diagnostic information</returns>
    public string GetComprehensiveDiagnosticInfo()
    {
        var cacheInfo = CacheManager.GetCacheInfo();
        var dataStoreInfo = Sources.DataStore.GetComprehensiveDiagnosticInfo();
        return $"{cacheInfo} | {dataStoreInfo}";
    }
    #endregion

    #region Query Operations - Delegate to StorageQueryService
    public IList<ItemStack> GetAllAvailableItemStacks(UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(GetAllAvailableItemStacks);

        if (!EnsureValidCache(d_MethodName))
        {
            ModLogger.Error($"{d_MethodName}: Cache validation failed, returning empty collection");
            return CollectionFactory.EmptyItemStackList;
        }

        return StorageQueryService.GetAllAvailableItemStacks(this, filter);
    }

    public int GetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetItemCount);
        var filter = UniqueItemTypes.FromItemValue(itemValue);

        if (!EnsureValidCache(d_MethodName))
        {
            ModLogger.Error($"{d_MethodName}: Cache validation failed, returning 0");
            return 0;
        }

        return StorageQueryService.GetItemCount(this, itemValue);
    }

    public int GetItemCount(UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (!EnsureValidCache(d_MethodName))
        {
            ModLogger.Error($"{d_MethodName}: Cache validation failed, returning 0");
            return 0;
        }

        return StorageQueryService.GetItemCount(this, filter);
    }

    public bool HasItem(ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);
        var filter = UniqueItemTypes.FromItemValue(itemValue);

        if (!EnsureValidCache(d_MethodName))
        {
            ModLogger.Error($"{d_MethodName}: Cache validation failed, returning false");
            return false;
        }

        return StorageQueryService.HasItem(this, itemValue);
    }

    public bool HasItem(UniqueItemTypes filter)
    {
        const string d_MethodName = nameof(HasItem);

        if (!EnsureValidCache(d_MethodName))
        {
            ModLogger.Error($"{d_MethodName}: Cache validation failed, returning false");
            return false;
        }

        return StorageQueryService.HasItem(this, filter);
    }
    #endregion

    #region Removal Operations - Delegate to StorageItemRemovalService
    public int RemoveRemaining(ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> gameTrackedRemovedItems = null)
    {
        const string d_MethodName = nameof(RemoveRemaining);
        var filter = UniqueItemTypes.FromItemValue(itemValue);

        if (!EnsureValidCache(d_MethodName))
        {
            ModLogger.Error($"{d_MethodName}: Cache validation failed, returning 0");
            return 0;
        }

        return StorageItemRemovalService.RemoveItems(this, itemValue, stillNeeded, ignoreModdedItems, gameTrackedRemovedItems);
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