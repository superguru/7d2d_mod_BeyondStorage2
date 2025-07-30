using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public sealed class BatchRemovalContext
{
    // Multiple caches for different durations - using concurrent dictionary for thread safety
    private static readonly ConcurrentDictionary<double, TimeBasedCache<BatchRemovalContext>> s_cachesByDuration = new();

    // Default cache duration
    private const double DEFAULT_CACHE_DURATION = 1.0;

    public ConfigSnapshot Config { get; }
    public WorldPlayerContext WorldPlayerContext { get; }

    public List<TileEntityDewCollector> DewCollectors { get; private set; }
    public List<ITileEntityLootable> Lootables { get; private set; }
    public List<EntityVehicle> Vehicles { get; private set; }
    public List<TileEntityWorkstation> Workstations { get; private set; }
    public DateTime CreatedAt { get; }

    private BatchRemovalContext()
    {
        Config = ConfigSnapshot.Current;

        // Create WorldPlayerContext first - if this fails, the entire context creation should fail
        WorldPlayerContext = WorldPlayerContext.TryCreate(nameof(BatchRemovalContext));
        if (WorldPlayerContext == null)
        {
            LogUtil.Error($"{nameof(BatchRemovalContext)}: Failed to create WorldPlayerContext, aborting context creation.");
            // In this case, we'll continue but the collections will remain empty
            DewCollectors = new List<TileEntityDewCollector>(0);
            Workstations = new List<TileEntityWorkstation>(0);
            Lootables = new List<ITileEntityLootable>(0);
            Vehicles = new List<EntityVehicle>(0);
            CreatedAt = DateTime.Now;
            return;
        }

        // Initialize collections with appropriate capacity
        DewCollectors = new List<TileEntityDewCollector>(ContainerUtils.DEFAULT_DEW_COLLECTOR_LIST_CAPACITY);
        Workstations = new List<TileEntityWorkstation>(ContainerUtils.DEFAULT_WORKSTATION_LIST_CAPACITY);
        Lootables = new List<ITileEntityLootable>(ContainerUtils.DEFAULT_LOOTBLE_LIST_CAPACITY);
        Vehicles = new List<EntityVehicle>(VehicleUtils.DEFAULT_VEHICLE_LIST_CAPACITY);

        // Let utility classes populate the collections
        ContainerUtils.DiscoverTileEntitySources(this);
        VehicleUtils.GetAvailableVehicleStorages(this);

        CreatedAt = DateTime.Now;

        //LogUtil.DebugLog($"BatchRemovalContext created: {Lootables.Count} lootables, {DewCollectors.Count} dew collectors, {Workstations.Count} workstations, {Vehicles.Count} vehicles");
    }

    /// <summary>
    /// Creates or retrieves a cached BatchRemovalContext instance.
    /// Uses TimeBasedCache to avoid expensive context creation operations.
    /// </summary>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <param name="forceRefresh">If true, bypasses cache and creates fresh context</param>
    /// <param name="cacheDurationSeconds">Override cache duration in seconds (default: 1.0)</param>
    /// <returns>A valid BatchRemovalContext or null if creation failed</returns>
    public static BatchRemovalContext Create(string methodName = "Unknown", double cacheDurationSeconds = DEFAULT_CACHE_DURATION, bool forceRefresh = false)
    {
        // Ensure cache duration is reasonable
        if (cacheDurationSeconds <= 0)
        {
            LogUtil.Warning($"{methodName}: Invalid cache duration {cacheDurationSeconds}, using default {DEFAULT_CACHE_DURATION}");
            cacheDurationSeconds = DEFAULT_CACHE_DURATION;
        }

        // Get or create cache for this duration
        var cache = s_cachesByDuration.GetOrAdd(cacheDurationSeconds, duration =>
            new TimeBasedCache<BatchRemovalContext>(duration, $"{nameof(BatchRemovalContext)}_{duration:F1}s"));

        return cache.GetOrCreate(() => CreateFresh(methodName), forceRefresh, methodName);
    }

    /// <summary>
    /// Forces cache invalidation for BatchRemovalContext instances with the specified duration.
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
                //LogUtil.DebugLog($"BatchRemovalContext cache invalidated for duration {cacheDurationSeconds.Value:F1}s");
            }
        }
        else
        {
            // Invalidate all caches
            foreach (var kvp in s_cachesByDuration)
            {
                kvp.Value.InvalidateCache();
            }
            //LogUtil.DebugLog("All BatchRemovalContext caches invalidated");
        }
    }

    /// <summary>
    /// Gets the age of the current cached context in seconds for the specified cache duration.
    /// Returns -1 if no cached context exists.
    /// </summary>
    /// <param name="cacheDurationSeconds">Cache duration to check (default: 1.0)</param>
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
    /// <param name="cacheDurationSeconds">Cache duration to check (default: 1.0)</param>
    public static bool HasValidCachedContext(double cacheDurationSeconds = DEFAULT_CACHE_DURATION)
    {
        if (s_cachesByDuration.TryGetValue(cacheDurationSeconds, out var cache))
        {
            return cache.HasValidCachedItem();
        }
        return false;
    }

    /// <summary>
    /// Gets cache statistics for diagnostics for the specified cache duration.
    /// </summary>
    /// <param name="cacheDurationSeconds">Cache duration to check (default: 1.0)</param>
    public static string GetCacheStats(double cacheDurationSeconds = DEFAULT_CACHE_DURATION)
    {
        if (s_cachesByDuration.TryGetValue(cacheDurationSeconds, out var cache))
        {
            return cache.GetCacheStats();
        }
        return $"{nameof(BatchRemovalContext)}_{cacheDurationSeconds:F1}s: No cache found";
    }

    /// <summary>
    /// Gets cache statistics for all cache durations.
    /// </summary>
    public static string GetAllCacheStats()
    {
        if (s_cachesByDuration.IsEmpty)
        {
            return "BatchRemovalContext: No caches created";
        }

        var stats = new List<string>();
        foreach (var kvp in s_cachesByDuration)
        {
            stats.Add($"{kvp.Key:F1}s: {kvp.Value.GetCacheStats()}");
        }
        return $"BatchRemovalContext caches: [{string.Join(", ", stats)}]";
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
            //LogUtil.DebugLog($"BatchRemovalContext: Cleaned up {keysToRemove.Count} unused caches");
        }
    }

    private static BatchRemovalContext CreateFresh(string methodName)
    {
        try
        {
            var context = new BatchRemovalContext();

            if (context.WorldPlayerContext == null)
            {
                LogUtil.Error($"{methodName}: Created BatchRemovalContext with null WorldPlayerContext");
                return null;
            }

            LogUtil.DebugLog($"{methodName}: Created fresh BatchRemovalContext with {context.GetSourceSummary()}");
            return context;
        }
        catch (Exception ex)
        {
            LogUtil.Error($"{methodName}: Exception creating BatchRemovalContext: {ex.Message}");
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
    /// Gets comprehensive cache information including nested WorldPlayerContext cache stats.
    /// </summary>
    /// <param name="cacheDurationSeconds">Cache duration to check (default: 1.0)</param>
    public static string GetComprehensiveCacheStats(double cacheDurationSeconds = DEFAULT_CACHE_DURATION)
    {
        var batchStats = GetCacheStats(cacheDurationSeconds);
        var worldPlayerStats = WorldPlayerContext.GetCacheStats();
        return $"BatchRemovalContext: {batchStats} | WorldPlayerContext: {worldPlayerStats}";
    }
}