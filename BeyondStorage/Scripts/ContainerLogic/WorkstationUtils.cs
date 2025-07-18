﻿using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;
using Platform;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class WorkstationUtils
{
    public static IEnumerable<TileEntityWorkstation> GetAvailableWorkstationOutputs()
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();
        var playerPos = player.position;
        var configRange = ModConfig.Range();
        var internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;

        var chunkCacheCopy = GameManager.Instance.World.ChunkCache.GetChunkArrayCopySync();
        if (chunkCacheCopy == null)
        {
            LogUtil.Error("GetAvailableWorkstationOutputs: chunkCacheCopy is null");
            yield break;
        }

        LogUtil.DebugLog("Starting GetAvailableWorkstationOutputs()");
        foreach (var tileEntity in chunkCacheCopy.Where(chunk => chunk != null).SelectMany(chunk => chunk.GetTileEntities().list.Where(item => item is TileEntityWorkstation)))
        {
            if (tileEntity is not TileEntityWorkstation workstation)
            {
                continue;  // Interesting...
            }

            // skip workstations outside of range
            bool isInRange = (configRange <= 0 || Vector3.Distance(playerPos, workstation.ToWorldPos()) < configRange);
            if (!isInRange)
            {
                continue;
            }

            if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
            {
                if (tileLockable.IsLocked() && !tileLockable.IsUserAllowed(internalLocalUserIdentifier))
                {
                    continue;
                }
            }

            if (!workstation.IsPlayerPlaced)
            {
                // Skip non player-placed workstations
                continue;
            }

            if (workstation.output == null || workstation.output.Count() == 0)
            {
                // Skip workstations without output
                continue;
            }

            if (workstation.OutputEmpty())
            {
                // Skip workstations with empty output
                continue;
            }

            if (workstation.IsRemoving)
            {
                // Skip locked workstations for the player
                continue;
            }

            if (ContainerUtils.LockedTileEntities.Count > 0)
            {
                var pos = workstation.ToWorldPos();
                if (ContainerUtils.LockedTileEntities.TryGetValue(pos, out int entityId) && entityId != player.entityId)
                {
                    continue;
                }
            }
#if DEBUG
            // TODO: You might want to comment the following line out while debugging new features
            LogUtil.DebugLog($"TE_WS: {workstation}; Locked Count: {ContainerUtils.LockedTileEntities.Count}; {tileEntity.IsUserAccessing()}");
#endif
            yield return workstation;
        }
    }
}