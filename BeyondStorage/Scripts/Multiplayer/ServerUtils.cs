using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;

using static ModEvents;

namespace BeyondStorage.Scripts.Multiplayer;

public static class ServerUtils
{
    public static bool HasServerConfig = false;

    public static void PlayerSpawnedInWorld(ref SPlayerSpawnedInWorldData data)
    {
        if (!ShouldProcessPlayerSpawn(data))
        {
            return;
        }

        ModLogger.DebugLog($"client {data.ClientInfo}; isLocalPlayer {data.IsLocalPlayer}; entityId {data.EntityId}; respawn type {data.RespawnType}; pos {data.Position}");

        SendCurrentLockedDict(data.ClientInfo);

        if (ModConfig.ServerSyncConfig())
        {
            data.ClientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageBeyondStorageConfig>());
        }
    }

    private static bool ShouldProcessPlayerSpawn(SPlayerSpawnedInWorldData data)
    {
        var connectionManager = SingletonMonoBehaviour<ConnectionManager>.Instance;

        // Add null check
        if (connectionManager == null)
        {
            return false;
        }

        return connectionManager.IsServer &&
               !connectionManager.IsSinglePlayer &&
               data.ClientInfo != null;
    }

    private static void SendCurrentLockedDict(ClientInfo client)
    {
        if (TileEntityLockManager.LockedTileEntities.IsEmpty || !IsValidDestination(client.entityId))
        {
            return;
        }

        var currentCopy = new Dictionary<Vector3i, int>(TileEntityLockManager.LockedTileEntities);
        client.SendPackage(NetPackageManager.GetPackage<NetPackageLockedTEs>().Setup(currentCopy));

#if DEBUG
        ModLogger.DebugLog($"SendCurrentLockedDict to {client.entityId}");
#endif
    }

    private static bool IsValidDestination(int destinationId)
    {
#if DEBUG
        ModLogger.DebugLog($"PlayerSpawnedInWorld called with {destinationId}");
        if (destinationId == -1)
        {
            ModLogger.Error("PlayerSpawnedInWorld called without a valid entity id");
            return false;
        }

        if (!GameManager.IsDedicatedServer && destinationId == GameManager.Instance.myEntityPlayerLocal.entityId)
        {
            ModLogger.DebugLog("Skipping local player starting server");
            return false;
        }
        return true;
#else
        if (destinationId == -1)
        {
            return false;
        }

        return GameManager.IsDedicatedServer ||
               destinationId != GameManager.Instance.myEntityPlayerLocal.entityId;
#endif
    }

    public static void LockedTEsUpdate()
    {
        var newLockedDict = GameManager.Instance.lockedTileEntities;
        var currentCopy = new Dictionary<Vector3i, int>(TileEntityLockManager.LockedTileEntities);

        if (ShouldSkipUpdate(newLockedDict.Count, currentCopy.Count))
        {
            return;
        }

        var (filteredDict, hasChanges) = ProcessLockedEntities(newLockedDict, currentCopy);

        if (!hasChanges && filteredDict.Count == currentCopy.Count)
        {
            return;
        }

        BroadcastLockedEntitiesUpdate(filteredDict);
    }

    private static bool ShouldSkipUpdate(int newCount, int currentCount)
    {
        return currentCount == 0 && newCount == 0;
    }

    private static (Dictionary<Vector3i, int> filteredDict, bool hasChanges) ProcessLockedEntities(
        IDictionary<ITileEntity, int> newLockedDict,
        Dictionary<Vector3i, int> currentCopy)
    {
        var tempDict = new Dictionary<Vector3i, int>();
        var foundChange = false;

        foreach (var kvp in newLockedDict)
        {
            if (!TryGetTileEntityPosition(kvp.Key, out var tePos))
            {
                continue;
            }

            tempDict.Add(tePos, kvp.Value);

            if (!foundChange)
            {
                foundChange = HasPositionChanged(currentCopy, tePos, kvp.Value);
            }
        }

        return (tempDict, foundChange);
    }

    private static bool TryGetTileEntityPosition(ITileEntity tileEntity, out Vector3i position)
    {
        position = default;

        if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable))
        {
            if (!lootable.bPlayerStorage)
            {
                return false;
            }
            position = lootable.ToWorldPos();
            return true;
        }

        switch (tileEntity)
        {
            case TileEntityDewCollector dewCollector:
                position = dewCollector.ToWorldPos();
                return true;
            case TileEntityWorkstation workstation:
                position = workstation.ToWorldPos();
                return true;
            default:
                return false;
        }
    }

    private static bool HasPositionChanged(Dictionary<Vector3i, int> currentCopy, Vector3i position, int newValue)
    {
        return !currentCopy.TryGetValue(position, out var currentValue) || currentValue != newValue;
    }

    private static void BroadcastLockedEntitiesUpdate(Dictionary<Vector3i, int> filteredDict)
    {
#if DEBUG
        ModLogger.DebugLog($"Original: {GameManager.Instance.lockedTileEntities.Count}; Filter: {filteredDict.Count}");
#endif

        // Use the original direct call pattern with null check
        if (SingletonMonoBehaviour<ConnectionManager>.Instance != null)
        {
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(new NetPackageLockedTEs().Setup(filteredDict));
        }

        TileEntityLockManager.UpdateLockedTEs(filteredDict);
    }
}