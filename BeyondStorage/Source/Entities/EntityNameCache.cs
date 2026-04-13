using System;
using System.Collections.Generic;

namespace BeyondStorage.Source.Entities;

/// <summary>
/// Caches resolved names for entities to avoid repeated localization lookups.
/// Uses LRU (Least Recently Used) eviction when the maximum cache size is reached.
/// Thread-safe for concurrent access.
/// </summary>
/// <remarks>
/// Implementation uses a LinkedList for LRU ordering and a Dictionary for O(1) lookups:
/// - Head of LinkedList = Most Recently Used
/// - Tail of LinkedList = Least Recently Used
/// - Both reads (TryGetName) and writes (CacheName) refresh an entry's position
/// - All operations are O(1) except eviction scanning
/// </remarks>
public static class EntityNameCache
{
    private const int MAX_CACHE_SIZE = 1024;

    private static readonly object s_lock = new();

    private class CacheEntry
    {
        public object Key { get; }
        public string Value { get; set; }

        public CacheEntry(object key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// Dictionary maps the entity key to the LinkedList node for O(1) access.
    /// </summary>
    private static readonly Dictionary<object, LinkedListNode<CacheEntry>> s_cacheMap = [];

    /// <summary>
    /// LinkedList maintains the LRU order.
    /// Head = Most Recently Used (First)
    /// Tail = Least Recently Used (Last)
    /// </summary>
    private static readonly LinkedList<CacheEntry> s_lruList = new();

    /// <summary>
    /// Adds or updates a name for a given entity. 
    /// If the entity is already cached, updates the name and moves it to the most-recent position (head).
    /// If the cache is full, evicts the least-recently-used entry before adding.
    /// </summary>
    /// <param name="entity">The entity (tile entity, collector, etc.) to cache</param>
    /// <param name="name">The resolved name to cache</param>
    /// <exception cref="ArgumentNullException">Thrown if entity is null</exception>
    public static void CacheName(object entity, string name)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        lock (s_lock)
        {
            if (s_cacheMap.TryGetValue(entity, out var node))
            {
                // Update existing entry and move to head (most recent)
                node.Value.Value = name;
                s_lruList.Remove(node);
                s_lruList.AddFirst(node);
            }
            else
            {
                // New entry - evict LRU if at capacity
                if (s_cacheMap.Count >= MAX_CACHE_SIZE)
                {
                    EvictLRU();
                }

                // Add new entry to head (most recent)
                var newEntry = new CacheEntry(entity, name);
                var newNode = s_lruList.AddFirst(newEntry);
                s_cacheMap.Add(entity, newNode);
            }
        }
    }

    /// <summary>
    /// Attempts to retrieve the name for an entity. 
    /// If found, returns true and moves the entry to the most-recent position (LRU refresh).
    /// </summary>
    /// <param name="entity">The entity to look up</param>
    /// <param name="name">The cached name if found, null otherwise</param>
    /// <returns>True if the entity was found in cache, false otherwise</returns>
    public static bool TryGetName(object entity, out string name)
    {
        if (entity == null)
        {
            name = null;
            return false;
        }

        lock (s_lock)
        {
            if (s_cacheMap.TryGetValue(entity, out var node))
            {
                // Move accessed node to head (refreshing its life)
                s_lruList.Remove(node);
                s_lruList.AddFirst(node);

                name = node.Value.Value;
                return true;
            }
        }

        name = null;
        return false;
    }

    /// <summary>
    /// Explicitly removes a name from the cache.
    /// Forces re-resolution on next access.
    /// Useful when entity names change (e.g., sign text updated).
    /// </summary>
    /// <param name="entity">The entity to remove from cache</param>
    /// <returns>True if the entity was found and removed, false if it wasn't cached</returns>
    public static bool RemoveName(object entity)
    {
        if (entity == null) return false;

        lock (s_lock)
        {
            if (s_cacheMap.TryGetValue(entity, out var node))
            {
                s_lruList.Remove(node);
                s_cacheMap.Remove(entity);

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Evicts the least-recently-used entry from the cache (tail of LinkedList).
    /// Called automatically when cache reaches MAX_CACHE_SIZE.
    /// </summary>
    private static void EvictLRU()
    {
        var oldestNode = s_lruList.Last;
        if (oldestNode != null)
        {
            s_cacheMap.Remove(oldestNode.Value.Key);
            s_lruList.RemoveLast();
        }
    }

    /// <summary>
    /// Clears all cached names. 
    /// Useful when reloading the game, changing worlds, or when localization is reloaded.
    /// </summary>
    public static void ClearCache()
    {
        lock (s_lock)
        {
            s_cacheMap.Clear();
            s_lruList.Clear();
        }
    }

    /// <summary>
    /// Gets the current number of cached entries.
    /// Useful for debugging and monitoring cache utilization.
    /// </summary>
    /// <returns>The number of entries currently in the cache</returns>
    public static int GetCacheSize()
    {
        lock (s_lock)
        {
            return s_cacheMap.Count;
        }
    }
}