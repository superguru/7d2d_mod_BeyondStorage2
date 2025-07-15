using System.Collections.Generic;
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

        foreach (var tileEntity in chunkCacheCopy.Where(chunk => chunk != null).SelectMany(chunk => chunk.GetTileEntities().list))
        {
            if (tileEntity is not TileEntityWorkstation workstation)
            {
                continue;
            }

#if DEBUG
            if (LogUtil.IsDebug())
            {
                LogUtil.DebugLog($"Found Workstation of type {workstation.GetType().Name}");
            }
#endif

            // skip workstations outside of range
            bool isInRange = (configRange <= 0 || Vector3.Distance(playerPos, workstation.ToWorldPos()) < configRange);
            if (!isInRange)
            {
                continue;
            }

#if DEBUG
            if (LogUtil.IsDebug())
            {
                LogUtil.DebugLog($"Found Workstation of type {workstation.GetType().Name} in range");
            }
#endif

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

            yield return workstation;
        }
    }
}