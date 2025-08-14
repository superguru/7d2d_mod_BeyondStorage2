using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Caching;

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
    /// Static lookup set for method names that should suppress cache usage logging.
    /// Using HashSet for O(1) lookup performance.
    /// </summary>
    private static readonly HashSet<string> s_suppressLoggingMethodNames = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object s_suppressLoggingLock = new();

    /// <summary>
    /// Gets the configured cache duration in seconds.
    /// </summary>
    public double CacheDurationSeconds { get; } = cacheDurationSeconds;

    /// <summary>
    /// Gets the cache type name used for logging.
    /// </summary>
    public string CacheTypeName { get; } = string.IsNullOrEmpty(cacheTypeName) ? typeof(T).Name : cacheTypeName;

    /// <summary>
    /// Adds a method name to the suppress logging lookup.
    /// Method names are compared case-insensitively.
    /// </summary>
    /// <param name="methodName">The method name to suppress logging for</param>
    public static void AddSuppressLoggingMethodName(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return;
        }

        lock (s_suppressLoggingLock)
        {
            s_suppressLoggingMethodNames.Add(methodName);
        }
    }

    /// <summary>
    /// Adds multiple method names to the suppress logging lookup.
    /// Method names are compared case-insensitively.
    /// </summary>
    /// <param name="methodNames">The method names to suppress logging for</param>
    public static void AddSuppressLoggingMethodNames(params string[] methodNames)
    {
        if (methodNames == null || methodNames.Length == 0)
        {
            return;
        }

        lock (s_suppressLoggingLock)
        {
            foreach (var methodName in methodNames)
            {
                if (!string.IsNullOrEmpty(methodName))
                {
                    _ = s_suppressLoggingMethodNames.Add(methodName);
                }
            }
        }
    }

    /// <summary>
    /// Removes a method name from the suppress logging lookup.
    /// </summary>
    /// <param name="methodName">The method name to remove</param>
    /// <returns>True if the method name was removed, false if it wasn't found</returns>
    public static bool RemoveSuppressLoggingMethodName(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return false;
        }

        lock (s_suppressLoggingLock)
        {
            return s_suppressLoggingMethodNames.Remove(methodName);
        }
    }

    /// <summary>
    /// Clears all method names from the suppress logging lookup.
    /// </summary>
    public static void ClearSuppressLoggingMethodNames()
    {
        lock (s_suppressLoggingLock)
        {
            s_suppressLoggingMethodNames.Clear();
        }
    }

    /// <summary>
    /// Checks if logging should be suppressed for the given method name.
    /// </summary>
    /// <param name="methodName">The method name to check</param>
    /// <returns>True if logging should be suppressed</returns>
    private static bool ShouldSuppressLogging(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return false;
        }

        lock (s_suppressLoggingLock)
        {
            return s_suppressLoggingMethodNames.Contains(methodName);
        }
    }

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
            // Determine if we should log based on LogCacheUsage setting and method name suppression
            bool shouldLog = LogCacheUsage && !ShouldSuppressLogging(methodName);

            // Check if we have a valid cached item
            if (!forceRefresh && _cachedItem != null)
            {
                var age = (DateTime.Now - _cacheTimestamp).TotalSeconds;
                if (age < CacheDurationSeconds)
                {
                    if (shouldLog)
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

                if (shouldLog)
                {
                    ModLogger.DebugLog($"{methodName}: Created fresh {CacheTypeName}");
                }
            }
            else
            {
                // ClearMove cache if factory returns null
                _cachedItem = null;

                if (shouldLog)
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

    /// <summary>
    /// Gets diagnostic information about the suppress logging configuration.
    /// </summary>
    /// <returns>String containing suppress logging statistics</returns>
    public static string GetSuppressLoggingStats()
    {
        lock (s_suppressLoggingLock)
        {
            return $"Suppress logging for {s_suppressLoggingMethodNames.Count} method name(s): [{string.Join(", ", s_suppressLoggingMethodNames)}]";
        }
    }
}