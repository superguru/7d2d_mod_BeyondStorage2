using System.Collections.Concurrent;
using System.Collections.Generic;
using BeyondStorage.Scripts.Server;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class TileEntityLockManager
{
    public static ConcurrentDictionary<Vector3i, int> LockedTileEntities { get; private set; }

    private static readonly PerformanceProfiler s_methodStats = new("TileEntityLockManager");

    public static void Init()
    {
        ServerUtils.HasServerConfig = false;
        LockedTileEntities = new ConcurrentDictionary<Vector3i, int>();
        s_methodStats.Clear();
    }

    public static void Cleanup()
    {
        ServerUtils.HasServerConfig = false;
        LockedTileEntities?.Clear();
        s_methodStats.Clear();
    }

    public static void UpdateLockedTEs(Dictionary<Vector3i, int> lockedTileEntities)
    {
        LockedTileEntities = new ConcurrentDictionary<Vector3i, int>(lockedTileEntities);
        Logger.DebugLog($"UpdateLockedTEs: newCount {lockedTileEntities.Count}");
    }
}