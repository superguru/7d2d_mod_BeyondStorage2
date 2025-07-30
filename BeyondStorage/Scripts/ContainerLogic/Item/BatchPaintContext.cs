using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

/// <summary>
/// Context class for batch paint operations that holds a shared BatchRemovalContext
/// and tracks accumulated operations for debugging.
/// </summary>
public sealed class BatchPaintContext
{
    // Multiple caches for different durations - using concurrent dictionary for thread safety
    private static readonly ConcurrentDictionary<double, TimeBasedCache<BatchPaintContext>> s_cachesByDuration = new();

    // Default cache duration for paint operations
    private const double DEFAULT_CACHE_DURATION = 2.0;

    public BatchRemovalContext RemovalContext { get; }
    private readonly Dictionary<int, int> _accumulatedRemovals = new();
    private int _totalOperations = 0;

    public BatchPaintContext(BatchRemovalContext removalContext)
    {
        RemovalContext = removalContext ?? throw new ArgumentNullException(nameof(removalContext));
    }

    /// <summary>
    /// Creates or retrieves a cached BatchPaintContext instance.
    /// </summary>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="forceRefresh">If true, bypasses cache and creates fresh context</param>
    /// <param name="cacheDurationSeconds">Override cache duration in seconds (default: 2.0)</param>
    /// <returns>A BatchPaintContext or null if creation failed</returns>
    public static BatchPaintContext Create(string methodName, double cacheDurationSeconds = DEFAULT_CACHE_DURATION, bool forceRefresh = false)
    {
        // Ensure cache duration is reasonable
        if (cacheDurationSeconds <= 0)
        {
            LogUtil.Warning($"{methodName}: Invalid cache duration {cacheDurationSeconds}, using default {DEFAULT_CACHE_DURATION}");
            cacheDurationSeconds = DEFAULT_CACHE_DURATION;
        }

        // Get or create cache for this duration
        var cache = s_cachesByDuration.GetOrAdd(cacheDurationSeconds, duration =>
            new TimeBasedCache<BatchPaintContext>(duration, $"{nameof(BatchPaintContext)}_{duration:F1}s"));

        return cache.GetOrCreate(() => CreateFresh(methodName), forceRefresh, methodName);
    }

    /// <summary>
    /// Creates a new BatchPaintContext with a fresh BatchRemovalContext.
    /// </summary>
    /// <param name="methodName">The calling method name for logging</param>
    /// <returns>A new BatchPaintContext or null if creation failed</returns>
    private static BatchPaintContext CreateFresh(string methodName)
    {
        try
        {
            var removalContext = BatchRemovalContext.Create(methodName, cacheDurationSeconds: DEFAULT_CACHE_DURATION);
            if (removalContext?.WorldPlayerContext == null)
            {
                LogUtil.Error($"{methodName}: Failed to create BatchRemovalContext with valid WorldPlayerContext");
                return null;
            }

            LogUtil.DebugLog($"{methodName}: Created batch paint context with {removalContext.GetSourceSummary()}");
            return new BatchPaintContext(removalContext);
        }
        catch (Exception ex)
        {
            LogUtil.Error($"{methodName}: Exception creating BatchPaintContext: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Forces invalidation of the batch paint cache for the specified duration.
    /// </summary>
    /// <param name="cacheDurationSeconds">Cache duration to invalidate (default: all caches)</param>
    public static void InvalidateCache(double? cacheDurationSeconds = null)
    {
        if (cacheDurationSeconds.HasValue)
        {
            // Invalidate specific cache duration
            if (s_cachesByDuration.TryGetValue(cacheDurationSeconds.Value, out var cache))
            {
                cache.InvalidateCache();
                LogUtil.DebugLog($"BatchPaintContext cache invalidated for duration {cacheDurationSeconds.Value:F1}s");
            }
        }
        else
        {
            // Invalidate all caches
            foreach (var kvp in s_cachesByDuration)
            {
                kvp.Value.InvalidateCache();
            }
            LogUtil.DebugLog("All BatchPaintContext caches invalidated");
        }
    }

    /// <summary>
    /// Gets batch paint cache statistics for debugging for the specified cache duration.
    /// </summary>
    /// <param name="cacheDurationSeconds">Cache duration to check (default: 2.0)</param>
    public static string GetCacheStats(double cacheDurationSeconds = DEFAULT_CACHE_DURATION)
    {
        if (s_cachesByDuration.TryGetValue(cacheDurationSeconds, out var cache))
        {
            return cache.GetCacheStats();
        }
        return $"{nameof(BatchPaintContext)}_{cacheDurationSeconds:F1}s: No cache found";
    }

    /// <summary>
    /// Gets cache statistics for all cache durations.
    /// </summary>
    public static string GetAllCacheStats()
    {
        if (s_cachesByDuration.IsEmpty)
        {
            return "BatchPaintContext: No caches created";
        }

        var stats = new List<string>();
        foreach (var kvp in s_cachesByDuration)
        {
            stats.Add($"{kvp.Key:F1}s: {kvp.Value.GetCacheStats()}");
        }
        return $"BatchPaintContext caches: [{string.Join(", ", stats)}]";
    }

    /// <summary>
    /// Gets the age of the current cached context in seconds for the specified cache duration.
    /// Returns -1 if no cached context exists.
    /// </summary>
    /// <param name="cacheDurationSeconds">Cache duration to check (default: 2.0)</param>
    public static double GetCacheAge(double cacheDurationSeconds = DEFAULT_CACHE_DURATION)
    {
        if (s_cachesByDuration.TryGetValue(cacheDurationSeconds, out var cache))
        {
            return cache.GetCacheAge();
        }
        return -1;
    }

    /// <summary>
    /// Checks if the cache currently has a valid (non-expired) context for the specified duration.
    /// </summary>
    /// <param name="cacheDurationSeconds">Cache duration to check (default: 2.0)</param>
    public static bool HasValidCachedContext(double cacheDurationSeconds = DEFAULT_CACHE_DURATION)
    {
        if (s_cachesByDuration.TryGetValue(cacheDurationSeconds, out var cache))
        {
            return cache.HasValidCachedItem();
        }
        return false;
    }

    /// <summary>
    /// Forces invalidation of all related caches (batch paint and batch removal context).
    /// </summary>
    /// <param name="paintCacheDurationSeconds">Paint cache duration to invalidate (default: all paint caches)</param>
    /// <param name="removalCacheDurationSeconds">Removal cache duration to invalidate (default: all removal caches)</param>
    public static void InvalidateAllCaches(double? paintCacheDurationSeconds = null, double? removalCacheDurationSeconds = null)
    {
        InvalidateCache(paintCacheDurationSeconds);
        BatchRemovalContext.InvalidateCache(removalCacheDurationSeconds);
        WorldPlayerContext.InvalidateCache();
        //LogUtil.DebugLog("All BatchPaintContext-related caches invalidated");
    }

    /// <summary>
    /// Gets comprehensive cache statistics for all related caches.
    /// </summary>
    /// <param name="paintCacheDurationSeconds">Paint cache duration to check (default: 2.0)</param>
    /// <param name="removalCacheDurationSeconds">Removal cache duration to check (default: 1.0)</param>
    public static string GetComprehensiveCacheStats(double paintCacheDurationSeconds = DEFAULT_CACHE_DURATION, double removalCacheDurationSeconds = 1.0)
    {
        var paintStats = GetCacheStats(paintCacheDurationSeconds);
        var contextStats = BatchRemovalContext.GetComprehensiveCacheStats(removalCacheDurationSeconds);
        return $"BatchPaint: {paintStats} | {contextStats}";
    }

    /// <summary>
    /// Cleans up unused caches (those without valid cached items).
    /// Call this periodically to prevent memory leaks from many different cache durations.
    /// </summary>
    public static void CleanupUnusedCaches()
    {
        var keysToRemove = new List<double>();

        foreach (var kvp in s_cachesByDuration)
        {
            if (!kvp.Value.HasValidCachedItem())
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            s_cachesByDuration.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            //LogUtil.DebugLog($"BatchPaintContext: Cleaned up {keysToRemove.Count} unused caches");
        }
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
        var contextAge = RemovalContext?.AgeInSeconds ?? -1;
        var worldContextAge = RemovalContext?.WorldPlayerContext?.AgeInSeconds ?? -1;
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