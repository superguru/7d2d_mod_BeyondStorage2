using System;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage
{
    /// <summary>
    /// Factory responsible for creating and caching StorageAccessContext instances.
    /// Handles the complex logic of context creation, validation, and caching.
    /// </summary>
    public static class StorageContextFactory
    {
        private const double DEFAULT_CACHE_DURATION = 0.5;
        private static readonly ExpiringCache<StorageAccessContext> s_contextCache = new(DEFAULT_CACHE_DURATION, nameof(StorageAccessContext));

        /// <summary>
        /// Creates or retrieves a cached StorageAccessContext instance.
        /// </summary>
        /// <param name="methodName">The calling method name for logging</param>
        /// <param name="forceRefresh">Whether to force creation of a fresh context</param>
        /// <returns>A valid StorageAccessContext or null if creation failed</returns>
        public static StorageAccessContext Create(string methodName = "Unknown", bool forceRefresh = false)
        {
            return s_contextCache.GetOrCreate(() => CreateFresh(methodName), forceRefresh, methodName);
        }

        /// <summary>
        /// Creates a fresh StorageAccessContext instance.
        /// </summary>
        /// <param name="methodName">The calling method name for logging</param>
        /// <returns>A new StorageAccessContext or null if creation failed</returns>
        private static StorageAccessContext CreateFresh(string methodName)
        {
            try
            {
                var worldPlayerContext = WorldPlayerContext.TryCreate(nameof(StorageAccessContext));
                if (worldPlayerContext == null)
                {
                    ModLogger.Error($"{methodName}: Failed to create WorldPlayerContext, aborting context creation.");
                    return null;
                }

                var config = ConfigSnapshot.Current;
                var sources = new StorageSourceCollection();
                var cacheManager = new ItemStackCacheManager();

                // Discover storage sources
                StorageDiscoveryService.DiscoverStorageSources(sources, worldPlayerContext, config);

                var context = new StorageAccessContext(config, worldPlayerContext, sources, cacheManager);

                ModLogger.DebugLog($"{methodName}: Created fresh StorageAccessContext with {context.GetSourceSummary()}");
                return context;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"{methodName}: Exception creating StorageAccessContext: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates that a context is usable.
        /// </summary>
        /// <param name="context">The context to validate</param>
        /// <returns>True if the context is valid</returns>
        public static bool IsValidContext(StorageAccessContext context)
        {
            return context != null && context.WorldPlayerContext != null;
        }

        /// <summary>
        /// Invalidates the context cache, forcing fresh creation on next request.
        /// </summary>
        public static void InvalidateCache()
        {
            s_contextCache.InvalidateCache();
            ItemStackCacheManager.InvalidateGlobalCache();
            ModLogger.DebugLog($"StorageAccessContext cache invalidated");
        }

        /// <summary>
        /// Gets the age of the current cached context in seconds.
        /// </summary>
        /// <returns>Age in seconds or -1 if no cached context</returns>
        public static double GetCacheAge()
        {
            return s_contextCache.GetCacheAge();
        }

        /// <summary>
        /// Checks if there is a valid cached context available.
        /// </summary>
        /// <returns>True if a valid cached context exists</returns>
        public static bool HasValidCachedContext()
        {
            return s_contextCache.HasValidCachedItem();
        }

        /// <summary>
        /// Gets cache statistics for diagnostics.
        /// </summary>
        /// <returns>String containing cache statistics</returns>
        public static string GetCacheStats()
        {
            return s_contextCache.GetCacheStats();
        }
    }
}