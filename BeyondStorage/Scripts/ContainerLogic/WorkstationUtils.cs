using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class WorkstationUtils
{
    public static IEnumerable<TileEntityWorkstation> GetAvailableWorkstationOutputs()
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();
        var playerPos = player.position;
        var configRange = ModConfig.Range();
        var chunkCacheCopy = GameManager.Instance.World.ChunkCache.GetChunkArrayCopySync();

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

            // isPlayerPlaced
            // OutputEmpty

            //// verify bag isn't null
            //if (vehicle.bag == null)
            //{
            //    continue;
            //}

            //// skip if empty
            //if (vehicle.bag.IsEmpty())
            //{
            //    continue;
            //}

            //// skip vehicles without storage
            //if (!vehicle.hasStorage())
            //{
            //    continue;
            //}

            //// skip vehicles locked for the player
            //if (vehicle.IsLockedForLocalPlayer(player))
            //{
            //    continue;
            //}

            yield return workstation;
        }
    }
}