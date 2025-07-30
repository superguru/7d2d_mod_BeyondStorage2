using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

/// <summary>
/// Context class for batch paint operations that holds a shared StorageAccessContext
/// and tracks accumulated operations for debugging.
/// </summary>
public sealed class BatchPaintContext
{
    // Default cache duration for paint operations
    private const double DEFAULT_CACHE_DURATION = 2.0;

    // Single cache for batch paint operations
    private static readonly TimeBasedCache<BatchPaintContext> s_batchPaintCache = new(DEFAULT_CACHE_DURATION, nameof(BatchPaintContext)); // 2 second cache for paint operations

    public StorageAccessContext StorageContext { get; }
    private readonly Dictionary<int, int> _accumulatedRemovals = new();
    private int _totalOperations = 0;

    public BatchPaintContext(StorageAccessContext storageContext)
    {
        StorageContext = storageContext ?? throw new ArgumentNullException(nameof(storageContext));
    }

    /// <summary>
    /// Creates or retrieves a cached BatchPaintContext instance.
    /// </summary>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="forceRefresh">If true, bypasses cache and creates fresh context</param>
    /// <returns>A BatchPaintContext or null if creation failed</returns>
    public static BatchPaintContext Create(string methodName, bool forceRefresh = false)
    {
        return s_batchPaintCache.GetOrCreate(() => CreateFresh(methodName), forceRefresh, methodName);
    }

    /// <summary>
    /// Creates a new BatchPaintContext with a fresh StorageAccessContext.
    /// </summary>
    /// <param name="methodName">The calling method name for logging</param>
    /// <returns>A new BatchPaintContext or null if creation failed</returns>
    private static BatchPaintContext CreateFresh(string methodName)
    {
        try
        {
            var storageContext = StorageAccessContext.Create(methodName);
            if (storageContext?.WorldPlayerContext == null)
            {
                LogUtil.Error($"{methodName}: Failed to create StorageAccessContext with valid WorldPlayerContext");
                return null;
            }

            LogUtil.DebugLog($"{methodName}: Created batch paint context with {storageContext.GetSourceSummary()}");
            return new BatchPaintContext(storageContext);
        }
        catch (Exception ex)
        {
            LogUtil.Error($"{methodName}: Exception creating BatchPaintContext: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Forces invalidation of the batch paint cache. 
    /// Call this when paint operations are complete or when world state changes significantly.
    /// </summary>
    public static void InvalidateCache()
    {
        s_batchPaintCache.InvalidateCache();
        LogUtil.DebugLog("Batch paint cache invalidated");
    }

    /// <summary>
    /// Gets batch paint cache statistics for debugging.
    /// </summary>
    public static string GetCacheStats()
    {
        return s_batchPaintCache.GetCacheStats();
    }

    /// <summary>
    /// Gets the age of the current cached context in seconds.
    /// Returns -1 if no cached context exists.
    /// </summary>
    public static double GetCacheAge()
    {
        return s_batchPaintCache.GetCacheAge();
    }

    /// <summary>
    /// Checks if the cache currently has a valid (non-expired) context.
    /// </summary>
    public static bool HasValidCachedContext()
    {
        return s_batchPaintCache.HasValidCachedItem();
    }

    /// <summary>
    /// Forces invalidation of all related caches (batch paint and storage access context).
    /// </summary>
    public static void InvalidateAllCaches()
    {
        s_batchPaintCache.InvalidateCache();
        StorageAccessContext.InvalidateCache();
        WorldPlayerContext.InvalidateCache();
        LogUtil.DebugLog("All BatchPaintContext-related caches invalidated");
    }

    /// <summary>
    /// Gets comprehensive cache statistics for all related caches.
    /// </summary>
    public static string GetComprehensiveCacheStats()
    {
        var paintStats = s_batchPaintCache.GetCacheStats();
        var contextStats = StorageAccessContext.GetComprehensiveCacheStats();
        return $"BatchPaint: {paintStats} | {contextStats}";
    }

    /// <summary>
    /// Accumulates the removal count for tracking and debugging purposes.
    /// </summary>
    /// <param name="itemValue">The item that was removed</param>
    /// <param name="removedCount">The number of items removed</param>
    public void AccumulateRemoval(ItemValue itemValue, int removedCount)
    {
        if (itemValue?.ItemClass != null && removedCount > 0)
        {
            var itemType = itemValue.type;
            _accumulatedRemovals.TryGetValue(itemType, out var currentCount);
            _accumulatedRemovals[itemType] = currentCount + removedCount;
            _totalOperations++;

            LogUtil.DebugLog($"BatchPaintContext: Accumulated {removedCount} of {itemValue.ItemClass.Name} (total: {_accumulatedRemovals[itemType]}, operations: {_totalOperations})");
        }
    }

    /// <summary>
    /// Gets a summary of all operations performed in this batch context.
    /// </summary>
    /// <returns>A string containing operation statistics and context ages</returns>
    public string GetOperationsSummary()
    {
        var contextAge = StorageContext?.AgeInSeconds ?? -1;
        var worldContextAge = StorageContext?.WorldPlayerContext?.AgeInSeconds ?? -1;
        return $"BatchPaintContext: {_totalOperations} operations, {_accumulatedRemovals.Count} different item types, Context age: {contextAge:F1}s, WorldPlayerContext age: {worldContextAge:F1}s";
    }

    /// <summary>
    /// Gets the total number of operations performed in this batch context.
    /// </summary>
    public int TotalOperations => _totalOperations;

    /// <summary>
    /// Gets the number of different item types that have been processed.
    /// </summary>
    public int UniqueItemTypes => _accumulatedRemovals.Count;

    /// <summary>
    /// Gets the total count of a specific item type that has been removed.
    /// </summary>
    /// <param name="itemType">The item type to check</param>
    /// <returns>The total count removed, or 0 if not found</returns>
    public int GetTotalRemovedCount(int itemType)
    {
        return _accumulatedRemovals.TryGetValue(itemType, out var count) ? count : 0;
    }

    /// <summary>
    /// Gets the total count of a specific item that has been removed.
    /// </summary>
    /// <param name="itemValue">The item to check</param>
    /// <returns>The total count removed, or 0 if not found</returns>
    public int GetTotalRemovedCount(ItemValue itemValue)
    {
        if (itemValue?.ItemClass == null)
        {
            return 0;
        }

        return GetTotalRemovedCount(itemValue.type);
    }
}