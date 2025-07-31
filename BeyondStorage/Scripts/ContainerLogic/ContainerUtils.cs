using System.Collections.Concurrent;
using System.Collections.Generic;
using BeyondStorage.Scripts.Server;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class ContainerUtils
{
    public static ConcurrentDictionary<Vector3i, int> LockedTileEntities { get; private set; }

    private static readonly MethodCallStatistics s_methodStats = new("ContainerUtils");

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
        LogUtil.DebugLog($"UpdateLockedTEs: newCount {lockedTileEntities.Count}");
    }

    public static Dictionary<string, (int callCount, long totalTimeMs, double avgTimeMs)> GetCallStatistics()
    {
        return s_methodStats.GetAllStatistics();
    }

    public static string GetFormattedCallStatistics()
    {
        return s_methodStats.GetFormattedStatistics();
    }

    public static bool HasItem(StorageAccessContext context, ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return false;
        }

        if (context == null)
        {
            context = StorageAccessContext.Create(d_MethodName);
        }

        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create StorageAccessContext");
            return false;
        }

        return context.HasItem(itemValue);
    }
}