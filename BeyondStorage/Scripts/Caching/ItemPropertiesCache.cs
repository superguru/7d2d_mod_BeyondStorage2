using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Caching;

/// <summary>
/// Caches frequently accessed item properties to improve performance in item processing loops.
/// Stores HasModSlots, CanStack, and HasMods values per item type ID and item instance.
/// </summary>
public static class ItemPropertiesCache
{
    /// <summary>
    /// Cache lifetime for item properties in seconds.
    /// Item properties are relatively static, so we can cache them for a longer duration.
    /// </summary>
    private const double CACHE_LIFETIME_SECONDS = 300.0; // 5 minutes

    /// <summary>
    /// Cache lifetime for HasMods results in seconds.
    /// HasMods is instance-specific and changes more frequently, so shorter cache duration.
    /// </summary>
    private const double HASMODS_CACHE_LIFETIME_SECONDS = 30.0; // 30 seconds

    /// <summary>
    /// Cached item properties data structure.
    /// </summary>
    private readonly struct ItemProperties
    {
        public readonly bool HasModSlots;
        public readonly bool CanStack;

        public ItemProperties(bool hasModSlots, bool canStack)
        {
            HasModSlots = hasModSlots;
            CanStack = canStack;
        }
    }

    /// <summary>
    /// Thread-safe time-based cache of item properties indexed by item type ID.
    /// </summary>
    private static readonly ExpiringDictionaryCache<int, ItemProperties> s_propertiesCache =
        new(CACHE_LIFETIME_SECONDS, nameof(ItemPropertiesCache));

    /// <summary>
    /// Thread-safe time-based cache of HasMods results indexed by item instance hash.
    /// Uses shorter cache duration since mod state can change.
    /// </summary>
    private static readonly ExpiringDictionaryCache<string, bool> s_hasModsCache =
        new(HASMODS_CACHE_LIFETIME_SECONDS, "HasModsCache");

    /// <summary>
    /// Gets cached or computes HasModSlots property for the specified ItemValue.
    /// </summary>
    /// <param name="itemValue">The ItemValue to check</param>
    /// <returns>True if the item type has mod slots</returns>
    public static bool GetHasModSlots(ItemValue itemValue)
    {
        if (itemValue == null)
        {
            return false;
        }

        var properties = s_propertiesCache.GetOrCreate(itemValue.type, CreateProperties);
        return properties.HasModSlots;
    }

    /// <summary>
    /// Gets cached or computes CanStack property for the specified ItemValue.
    /// </summary>
    /// <param name="itemValue">The ItemValue to check</param>
    /// <returns>True if the item type can stack</returns>
    public static bool GetCanStack(ItemValue itemValue)
    {
        if (itemValue == null)
        {
            return false;
        }

        var properties = s_propertiesCache.GetOrCreate(itemValue.type, CreateProperties);
        return properties.CanStack;
    }

    /// <summary>
    /// Gets cached or computes HasMods result for the specified ItemValue.
    /// This caches the actual mod state of individual item instances.
    /// </summary>
    /// <param name="itemValue">The ItemValue to check</param>
    /// <returns>True if this specific item instance has mods installed</returns>
    public static bool GetHasMods(ItemValue itemValue)
    {
        if (itemValue == null)
        {
            return false;
        }

        // Quick check: if the item type can't have mods, return false immediately
        if (!GetHasModSlots(itemValue))
        {
            return false;
        }

        // Generate a cache key that represents this specific item instance
        var cacheKey = GenerateItemInstanceKey(itemValue);

        // Get cached result or compute new one
        return s_hasModsCache.GetOrCreate(cacheKey, _ => itemValue.HasMods());
    }

    /// <summary>
    /// Checks if an item should be ignored due to mod restrictions using cached properties.
    /// This is an optimized version of the common pattern: ignoreModdedItems && itemValue.HasModSlots && itemValue.HasMods()
    /// </summary>
    /// <param name="itemValue">The item value to check</param>
    /// <param name="ignoreModdedItems">Whether to ignore modded items</param>
    /// <returns>True if the item should be ignored</returns>
    public static bool ShouldIgnoreModdedItem(ItemValue itemValue, bool ignoreModdedItems)
    {
        if (!ignoreModdedItems || itemValue == null)
        {
            return false;
        }

        // Use cached HasModSlots check first (fastest)
        if (!GetHasModSlots(itemValue))
        {
            return false;
        }

        // Use cached HasMods check (slower but still cached)
        return GetHasMods(itemValue);
    }

    /// <summary>
    /// Generates a cache key for a specific item instance based on its type and mod state.
    /// This key should be unique for items with different mod configurations.
    /// </summary>
    /// <param name="itemValue">The ItemValue to generate a key for</param>
    /// <returns>A string key representing this specific item instance</returns>
    private static string GenerateItemInstanceKey(ItemValue itemValue)
    {
        // Start with item type as base
        var keyBuilder = new System.Text.StringBuilder();
        keyBuilder.Append(itemValue.type);

        // Add quality if it affects mod slots
        if (itemValue.HasQuality)
        {
            keyBuilder.Append('_');
            keyBuilder.Append(itemValue.Quality);
        }

        // Add a hash of the mods array to detect changes
        if (itemValue.Modifications != null && itemValue.Modifications.Length > 0)
        {
            keyBuilder.Append('_');

            // Create a simple hash of the modifications array
            int modHash = 0;
            for (int i = 0; i < itemValue.Modifications.Length; i++)
            {
                var mod = itemValue.Modifications[i];
                if (mod != null && !mod.IsEmpty())
                {
                    modHash = modHash * 31 + mod.type;
                    modHash = modHash * 31 + i; // Include slot position
                }
            }
            keyBuilder.Append(modHash);
        }
        else
        {
            // No mods installed
            keyBuilder.Append("_nomods");
        }

        return keyBuilder.ToString();
    }

    /// <summary>
    /// Factory method to create ItemProperties for a given item type.
    /// </summary>
    /// <param name="itemType">The item type ID</param>
    /// <returns>Newly computed item properties</returns>
    private static ItemProperties CreateProperties(int itemType)
    {
        // Create a temporary ItemValue to access properties
        var tempItemValue = new ItemValue(itemType);

        var hasModSlots = tempItemValue.HasModSlots;
        var canStack = tempItemValue.ItemClass?.CanStack() ?? false;

        return new ItemProperties(hasModSlots, canStack);
    }

    /// <summary>
    /// Clears all caches. Useful for testing or when item definitions change.
    /// </summary>
    public static void ClearCache()
    {
        s_propertiesCache.InvalidateCache();
        s_hasModsCache.InvalidateCache();
    }

    /// <summary>
    /// Removes expired entries from both caches to prevent memory bloat.
    /// </summary>
    public static void CleanupExpiredEntries()
    {
        s_propertiesCache.CleanupExpiredEntries();
        s_hasModsCache.CleanupExpiredEntries();
    }

    /// <summary>
    /// Gets cache statistics for monitoring and debugging.
    /// </summary>
    /// <returns>String containing cache statistics</returns>
    public static string GetCacheStats()
    {
        var propertiesStats = s_propertiesCache.GetCacheStats();
        var hasModsStats = s_hasModsCache.GetCacheStats();
        return $"{propertiesStats} | {hasModsStats}";
    }

    /// <summary>
    /// Forces invalidation of HasMods cache when items might have changed.
    /// Call this when you know item modifications have been altered.
    /// </summary>
    public static void InvalidateHasModsCache()
    {
        s_hasModsCache.InvalidateCache();
        ModLogger.DebugLog("ItemPropertiesCache: HasMods cache invalidated");
    }

    /// <summary>
    /// Forces invalidation of a specific item's HasMods cache entry.
    /// </summary>
    /// <param name="itemValue">The item whose cache entry should be invalidated</param>
    public static void InvalidateHasModsForItem(ItemValue itemValue)
    {
        if (itemValue != null)
        {
            var cacheKey = GenerateItemInstanceKey(itemValue);
            s_hasModsCache.InvalidateKey(cacheKey);
        }
    }
}