using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Caching;

/// <summary>
/// A generic time-based dictionary cache that stores multiple items of type TValue keyed by TKey with configurable expiration.
/// Thread-safe and provides methods for cache management and diagnostics.
/// </summary>
/// <typeparam name="TKey">The type of the cache key</typeparam>
/// <typeparam name="TValue">The type of the cached value</typeparam>
public sealed class ExpiringDictionaryCache<TKey, TValue>
{
    private readonly struct CacheEntry
    {
        public readonly TValue Value;
        public readonly DateTime CachedAt;

        public CacheEntry(TValue value)
        {
            Value = value;
            CachedAt = DateTime.Now;
        }

        public bool IsExpired(double cacheDurationSeconds) =>
            (DateTime.Now - CachedAt).TotalSeconds > cacheDurationSeconds;
    }

    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();
    private readonly double _cacheDurationSeconds;
    private readonly string _cacheTypeName;

    /// <summary>
    /// Initializes a new instance of the ExpiringDictionaryCache.
    /// </summary>
    /// <param name="cacheDurationSeconds">How long items should be cached in seconds</param>
    /// <param name="cacheTypeName">Name for logging purposes (optional)</param>
    public ExpiringDictionaryCache(double cacheDurationSeconds, string cacheTypeName = null)
    {
        _cacheDurationSeconds = cacheDurationSeconds;
        _cacheTypeName = cacheTypeName ?? $"{typeof(TKey).Name}->{typeof(TValue).Name}";
    }

    /// <summary>
    /// Gets an item from cache or creates a new one using the provided factory function.
    /// </summary>
    /// <param name="key">The key to look up</param>
    /// <param name="factory">Function to create a new item when cache is empty or expired</param>
    /// <param name="forceRefresh">If true, bypasses cache and creates a fresh item</param>
    /// <returns>Cached or newly created item</returns>
    public TValue GetOrCreate(TKey key, Func<TKey, TValue> factory, bool forceRefresh = false)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        // Fast path: check if we have valid cached data
        if (!forceRefresh && _cache.TryGetValue(key, out var cachedEntry) && !cachedEntry.IsExpired(_cacheDurationSeconds))
        {
            return cachedEntry.Value;
        }

        // Slow path: create new item
        var newValue = factory(key);
        var newEntry = new CacheEntry(newValue);

        // Use AddOrUpdate to handle race conditions
        _cache.AddOrUpdate(key, newEntry, (k, oldEntry) => newEntry);

        return newValue;
    }

    /// <summary>
    /// Forces cache invalidation for all items.
    /// </summary>
    public void InvalidateCache()
    {
        _cache.Clear();
        ModLogger.DebugLog($"{_cacheTypeName}: cache invalidated");
    }

    /// <summary>
    /// Forces cache invalidation for a specific key.
    /// </summary>
    /// <param name="key">The key to invalidate</param>
    public void InvalidateKey(TKey key)
    {
        _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Removes expired entries from the cache to prevent memory bloat.
    /// </summary>
    public void CleanupExpiredEntries()
    {
        var keysToRemove = new List<TKey>();

        foreach (var kvp in _cache)
        {
            if (kvp.Value.IsExpired(_cacheDurationSeconds))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            ModLogger.DebugLog($"{_cacheTypeName}: Cleaned up {keysToRemove.Count} expired entries");
        }
    }

    /// <summary>
    /// Gets cache statistics for diagnostics.
    /// </summary>
    /// <returns>String containing cache status information</returns>
    public string GetCacheStats()
    {
        var totalEntries = _cache.Count;
        var expiredEntries = 0;

        foreach (var kvp in _cache)
        {
            if (kvp.Value.IsExpired(_cacheDurationSeconds))
            {
                expiredEntries++;
            }
        }

        var validEntries = totalEntries - expiredEntries;
        return $"{_cacheTypeName}: {validEntries} valid, {expiredEntries} expired, {totalEntries} total entries (cache lifetime: {_cacheDurationSeconds}s)";
    }

    /// <summary>
    /// Gets the configured cache duration in seconds.
    /// </summary>
    public double CacheDurationSeconds => _cacheDurationSeconds;

    /// <summary>
    /// Gets the cache type name used for logging.
    /// </summary>
    public string CacheTypeName => _cacheTypeName;
}