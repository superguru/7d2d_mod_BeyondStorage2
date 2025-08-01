using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.ContainerLogic;
using BeyondStorage.Scripts.Utils;
using static ModEvents;

namespace BeyondStorage.Scripts.Server;

public static class ServerUtils
{
    public static bool HasServerConfig = false;

    public static void PlayerSpawnedInWorld(ref SPlayerSpawnedInWorldData data)
    {
        // Skip if we're not a server
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            return;
        }
        // Skip if single player
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer)
        {
            return;
        }

        Logger.DebugLog($"client {data.ClientInfo}; isLocalPlayer {data.IsLocalPlayer}; entityId {data.EntityId}; respawn type {data.RespawnType}; pos {data.Position}");

        if (data.ClientInfo == null)
        {
            return;
        }
        // Send the current locked dictionary to player logging in
        SendCurrentLockedDict(data.ClientInfo);
        if (ModConfig.ServerSyncConfig())
        {
            data.ClientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageBeyondStorageConfig>());
        }
    }

    private static void SendCurrentLockedDict(ClientInfo client)
    {
        // Skip if we have nothing to send
        if (TileEntityLockManager.LockedTileEntities.IsEmpty)
        {
            return;
        }

        var destinationId = client.entityId;
#if DEBUG
        Logger.DebugLog($"PlayerSpawnedInWorld called with {destinationId}");
        // skip if invalid entity ID or if we are the server just logging in
        if (destinationId == -1)
        {
            Logger.Error("PlayerSpawnedInWorld called without a valid entity id");
            return;
        }

        if (!GameManager.IsDedicatedServer && destinationId == GameManager.Instance.myEntityPlayerLocal.entityId)
        {
            Logger.DebugLog("Skipping local player starting server");
            return;
        }
#else
        // skip if invalid entity ID
        if (destinationId == -1)
        {
            return;
        }
        // skip local entity test if we're a dedicated server
        if (!GameManager.IsDedicatedServer)
        {
            // skip entity is the server-client first starting the server (logging in)
            if (destinationId == GameManager.Instance.myEntityPlayerLocal.entityId)
            {
                return;
            }
        }
#endif 
        // send current locked entities to newly logging in player
        var currentCopy = new Dictionary<Vector3i, int>(TileEntityLockManager.LockedTileEntities);
        client.SendPackage(NetPackageManager.GetPackage<NetPackageLockedTEs>().Setup(currentCopy));
        // SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(new NetPackageLockedTEs().Setup(currentCopy), true, destinationId);
#if DEBUG
        Logger.DebugLog($"SendCurrentLockedDict to {destinationId}");
#endif
    }

    public static void LockedTEsUpdate()
    {
        var newLockedDict = GameManager.Instance.lockedTileEntities;
        var newDictCount = newLockedDict.Count;

        // Skip if it was 0 and still is (before filtering)
        if (TileEntityLockManager.LockedTileEntities.Count == 0 && newDictCount == 0)
        {
            return;
        }

        // TODO: investigate possible performance hit, if large consider moving to update every X delta?
        //          concurrent checking looks to take 1-8 ms for small dictionaries
        Dictionary<Vector3i, int> tempDict = new();
        var currentCopy = new Dictionary<Vector3i, int>(TileEntityLockManager.LockedTileEntities);
        var currentCount = currentCopy.Count;
        var foundChange = false;

        // Remove anything not player storage
        foreach (var kvp in newLockedDict)
        {
            Vector3i tePos;

            if (kvp.Key.TryGetSelfOrFeature(out ITileEntityLootable tileEntityLootable))
            {
                // Handle lootable TEs
                if (!tileEntityLootable.bPlayerStorage)
                {
                    continue;
                }

                tePos = tileEntityLootable.ToWorldPos();
            }
            else if (kvp.Key is TileEntityDewCollector dewCollector)
            {
                // Handle dew collector TEs
                tePos = dewCollector.ToWorldPos();
            }
            else if (kvp.Key is TileEntityWorkstation workstation)
            {
                // Handle workstation TEs
                tePos = workstation.ToWorldPos();
            }
            else
            {
                // Some other TE, which we are not handling
                continue;
            }

            // Add current entry to our new dict for clients
            tempDict.Add(tePos, kvp.Value);

            // Skip if we already know things have changes
            if (foundChange)
            {
                continue;
            }

            if (currentCopy.TryGetValue(tePos, out var currentValue))
            {
                if (currentValue != kvp.Value)
                {
                    foundChange = true; // previous value of key changed
                }
            }
            else
            {
                // new key found mark as changed
                foundChange = true;
            }
        }

        // capture our new count
        var newCount = tempDict.Count;

        // skip if we didn't find any change and the lengths are the same
        if (!foundChange && newCount == currentCount)
        {
            return;
        }
#if DEBUG
        Logger.DebugLog($"Original Count: {newLockedDict.Count}; Filter Count: {newCount}");
#endif
        // Update clients with filtered list
        SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(new NetPackageLockedTEs().Setup(tempDict));

        // Update our own list as well
        TileEntityLockManager.UpdateLockedTEs(tempDict);
    }
}