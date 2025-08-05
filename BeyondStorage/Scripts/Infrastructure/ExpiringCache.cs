using System;

namespace BeyondStorage.Scripts.Infrastructure;

/// <summary>
/// A generic time-based cache that stores a single item of type T with configurable expiration.
/// Thread-safe and provides methods for cache management and diagnostics.
/// </summary>
/// <typeparam name="T">The type of object to cache</typeparam>
/// <remarks>
/// Initializes a new instance of the ExpiringCache.
/// </remarks>
/// <param name="cacheDurationSeconds">How long items should be cached in seconds</param>
/// <param name="cacheTypeName">Name for logging purposes (optional)</param>
public sealed class ExpiringCache<T>(double cacheDurationSeconds, string cacheTypeName = null) where T : class
{
    private T _cachedItem;
    private DateTime _cacheTimestamp;
    private readonly object _cacheLock = new();
    public bool LogCacheUsage { get; set; } = true;


    /// <summary>
    /// Gets the configured cache duration in seconds.
    /// </summary>
    public double CacheDurationSeconds { get; } = cacheDurationSeconds;

    /// <summary>
    /// Gets the cache type name used for logging.
    /// </summary>
    public string CacheTypeName { get; } = string.IsNullOrEmpty(cacheTypeName) ? typeof(T).Name : cacheTypeName;

    /// <summary>
    /// Gets an item from cache or creates a new one using the provided factory function.
    /// </summary>
    /// <param name="factory">Function to create a new item when cache is empty or expired</param>
    /// <param name="forceRefresh">If true, bypasses cache and creates a fresh item</param>
    /// <param name="methodName">Calling method name for logging</param>
    /// <returns>Cached or newly created item, or null if factory returns null</returns>
    public T GetOrCreate(Func<T> factory, bool forceRefresh = false, string methodName = "Unknown")
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        lock (_cacheLock)
        {
            // Check if we have a valid cached item
            if (!forceRefresh && _cachedItem != null)
            {
                var age = (DateTime.Now - _cacheTimestamp).TotalSeconds;
                if (age < CacheDurationSeconds)
                {
                    if (LogCacheUsage)
                    {
                        ModLogger.DebugLog($"{methodName}: Using cached {CacheTypeName} (age: {age:F3}s)");
                    }

                    return _cachedItem;
                }
            }

            // Create new item
            var newItem = factory();
            if (newItem != null)
            {
                _cachedItem = newItem;
                _cacheTimestamp = DateTime.Now;

                if (LogCacheUsage)
                {
                    ModLogger.DebugLog($"{methodName}: Created fresh {CacheTypeName}");
                }
            }
            else
            {
                // Clear cache if factory returns null
                _cachedItem = null;

                if (LogCacheUsage)
                {
                    ModLogger.DebugLog($"{methodName}: Factory returned null for {CacheTypeName}, cache cleared");
                }
            }

            return newItem;
        }
    }

    /// <summary>
    /// Forces cache invalidation. Next call to GetOrCreate will create a fresh item.
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedItem = null;

            if (LogCacheUsage)
            {
                ModLogger.DebugLog($"{CacheTypeName} cache invalidated");
            }
        }
    }

    /// <summary>
    /// Gets the age of the current cached item in seconds.
    /// Returns -1 if no cached item exists.
    /// </summary>
    /// <returns>Age in seconds or -1 if no cached item</returns>
    public double GetCacheAge()
    {
        lock (_cacheLock)
        {
            if (_cachedItem == null)
            {
                return -1;
            }

            return (DateTime.Now - _cacheTimestamp).TotalSeconds;
        }
    }

    /// <summary>
    /// Checks if the cache currently has a valid (non-expired) item.
    /// </summary>
    /// <returns>True if cache has a valid item</returns>
    public bool HasValidCachedItem()
    {
        lock (_cacheLock)
        {
            if (_cachedItem == null)
            {
                return false;
            }

            var age = (DateTime.Now - _cacheTimestamp).TotalSeconds;
            return age < CacheDurationSeconds;
        }
    }

    /// <summary>
    /// Gets cache statistics for diagnostics.
    /// </summary>
    /// <returns>String containing cache status information</returns>
    public string GetCacheStats()
    {
        lock (_cacheLock)
        {
            if (_cachedItem == null)
            {
                return $"{CacheTypeName} Cache: Empty";
            }

            var age = GetCacheAge();
            var isValid = age < CacheDurationSeconds;
            return $"{CacheTypeName} Cache: Age={age:F3}s, Valid={isValid}, Duration={CacheDurationSeconds}s";
        }
    }
}