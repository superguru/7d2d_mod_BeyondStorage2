using System.Collections.Generic;
using BeyondStorage.Scripts.Utils;
using Platform;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

/// <summary>
/// Encapsulates world and player context information needed for tile entity operations.
/// This class provides a centralized way to access commonly used world/player data.
/// </summary>
public sealed class WorldPlayerContext
{
    private static readonly TimeBasedCache<WorldPlayerContext> s_cache = new(0.5, nameof(WorldPlayerContext)); // 500ms cache

    public World World { get; }
    public EntityPlayerLocal Player { get; }
    public Vector3 PlayerPosition { get; }
    public PlatformUserIdentifierAbs InternalLocalUserIdentifier { get; }
    public int PlayerEntityId { get; }
    public List<Chunk> ChunkCacheCopy { get; }
    public System.DateTime CreatedAt { get; }

    private WorldPlayerContext(World world, EntityPlayerLocal player, List<Chunk> chunkCacheCopy)
    {
        World = world;
        Player = player;
        ChunkCacheCopy = chunkCacheCopy;
        PlayerPosition = player.position;
        InternalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;
        PlayerEntityId = player.entityId;
        CreatedAt = System.DateTime.Now;
    }

    /// <summary>
    /// Creates a new WorldPlayerContext if all required components are available.
    /// Uses caching to avoid expensive operations when called frequently.
    /// Returns null if any component is unavailable.
    /// </summary>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <param name="forceRefresh">If true, bypasses cache and creates fresh context</param>
    /// <returns>A valid WorldPlayerContext or null if creation failed</returns>
    public static WorldPlayerContext TryCreate(string methodName, bool forceRefresh = false)
    {
        return s_cache.GetOrCreate(() => CreateFresh(methodName), forceRefresh, methodName);
    }

    /// <summary>
    /// Forces cache invalidation and creates a fresh context on next call.
    /// </summary>
    public static void InvalidateCache()
    {
        s_cache.InvalidateCache();
    }

    /// <summary>
    /// Gets the age of the current cached context in seconds.
    /// Returns -1 if no cached context exists.
    /// </summary>
    public static double GetCacheAge()
    {
        return s_cache.GetCacheAge();
    }

    /// <summary>
    /// Checks if the cache currently has a valid (non-expired) context.
    /// </summary>
    public static bool HasValidCachedContext()
    {
        return s_cache.HasValidCachedItem();
    }

    /// <summary>
    /// Gets cache statistics for diagnostics.
    /// </summary>
    public static string GetCacheStats()
    {
        return s_cache.GetCacheStats();
    }

    private static WorldPlayerContext CreateFresh(string methodName)
    {
        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.Error($"{methodName}: World is null, aborting.");
            return null;
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.Error($"{methodName}: Player is null, aborting.");
            return null;
        }

        var chunkCacheCopy = world.ChunkCache.GetChunkArrayCopySync();
        if (chunkCacheCopy == null)
        {
            LogUtil.Error($"{methodName}: chunkCacheCopy is null, aborting.");
            return null;
        }

        return new WorldPlayerContext(world, player, chunkCacheCopy);
    }

    /// <summary>
    /// Calculates the distance between the player and a world position.
    /// </summary>
    /// <param name="worldPosition">The world position to measure distance to</param>
    /// <returns>The distance in world units</returns>
    public float DistanceToPlayer(Vector3 worldPosition)
    {
        return Vector3.Distance(PlayerPosition, worldPosition);
    }

    /// <summary>
    /// Checks if a position is within the specified range of the player.
    /// </summary>
    /// <param name="worldPosition">The world position to check</param>
    /// <param name="range">The maximum range (0 or negative means no range limit)</param>
    /// <returns>True if within range or range is unlimited</returns>
    public bool IsWithinRange(Vector3 worldPosition, float range)
    {
        return range <= 0 || DistanceToPlayer(worldPosition) < range;
    }

    /// <summary>
    /// Checks if the player is allowed to access a lockable tile entity.
    /// </summary>
    /// <param name="lockable">The lockable tile entity to check</param>
    /// <returns>True if the player can access the lockable entity</returns>
    public bool CanAccessLockable(ILockable lockable)
    {
        return lockable == null || !lockable.IsLocked() || lockable.IsUserAllowed(InternalLocalUserIdentifier);
    }

    /// <summary>
    /// Gets the age of this context instance in seconds.
    /// </summary>
    public double AgeInSeconds => (System.DateTime.Now - CreatedAt).TotalSeconds;

    /// <summary>
    /// Checks if this context has expired based on the given lifetime.
    /// </summary>
    /// <param name="lifetimeSeconds">Maximum lifetime in seconds</param>
    /// <returns>True if the context has expired</returns>
    public bool HasExpired(double lifetimeSeconds) => AgeInSeconds > lifetimeSeconds;
}