using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;
using Platform;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class WorkstationUtils
{
    public static List<TileEntityWorkstation> GetAvailableWorkstationOutputs()
    {
        const string d_method_name = "GetAvailableWorkstationOutputs";

        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.Error($"{d_method_name}: World is null");
            return new List<TileEntityWorkstation>();
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.Error($"{d_method_name}: Player is null");
            return new List<TileEntityWorkstation>();
        }

        var playerPos = player.position;
        var configRange = ModConfig.Range();
        var internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;
        var playerEntityId = player.entityId;

        var chunkCacheCopy = world.ChunkCache.GetChunkArrayCopySync();
        if (chunkCacheCopy == null)
        {
            LogUtil.Error($"{d_method_name}: chunkCacheCopy is null");
            return new List<TileEntityWorkstation>();
        }

        LogUtil.DebugLog($"{d_method_name}: Starting");

        var result = new List<TileEntityWorkstation>();

        foreach (var chunk in chunkCacheCopy)
        {
            if (chunk == null)
            {
                continue;
            }

            foreach (var tileEntity in chunk.GetTileEntities().list)
            {
                if (tileEntity is not TileEntityWorkstation workstation)
                {
                    continue;
                }

                // Range check
                if (configRange > 0 && Vector3.Distance(playerPos, workstation.ToWorldPos()) >= configRange)
                {
                    continue;
                }

                // Lockable check
                if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                {
                    if (tileLockable.IsLocked() && !tileLockable.IsUserAllowed(internalLocalUserIdentifier))
                    {
                        continue;
                    }
                }

                // Only player-placed workstations
                if (!workstation.IsPlayerPlaced)
                {
                    continue;
                }

                // Must have output and not be empty
                if (workstation.output == null || !workstation.output.Any())
                {
                    continue;
                }

                if (workstation.OutputEmpty())
                {
                    continue;
                }

                // Skip if being removed
                if (workstation.IsRemoving)
                {
                    continue;
                }

                // Locked tile entities check
                if (ContainerUtils.LockedTileEntities.Count > 0)
                {
                    var pos = workstation.ToWorldPos();
                    if (ContainerUtils.LockedTileEntities.TryGetValue(pos, out int entityId) && entityId != playerEntityId)
                    {
                        continue;
                    }
                }

                result.Add(workstation);
            }
        }

        return result;
    }

    /// <summary>
    /// Marks a workstation as modified when items are removed from its output, such as when pulling items from the workstation.
    /// </summary>
    public static void MarkWorkstationModified(TileEntityWorkstation workstation)
    {
        const string d_method_name = "MarkWorkstationModified";
        LogUtil.DebugLog($"{d_method_name} | Marking Workstation '{workstation?.GetType().Name}' as modified");

        if (workstation == null)
        {
            LogUtil.Error($"{d_method_name}: workstation is null");
            return;
        }

        workstation.SetChunkModified();
        workstation.SetModified();

        string blockName = GameManager.Instance.World.GetBlock(workstation.ToWorldPos()).Block.GetBlockName();
        var workstationData = CraftingManager.GetWorkstationData(blockName);
        if (workstationData == null)
        {
            LogUtil.Error($"{d_method_name}: No WorkstationData found for block '{blockName}'");
            return;
        }

        string windowName = !string.IsNullOrEmpty(workstationData.WorkstationWindow)
            ? workstationData.WorkstationWindow
            : $"workstation_{blockName}";

        LogUtil.DebugLog($"{d_method_name}: blockName '{blockName}', windowName '{windowName}'");

        var player = GameManager.Instance.World.GetPrimaryPlayer();

        var windowGroup = player.windowManager.GetWindow(windowName) as XUiWindowGroup;
        if (windowGroup == null)
        {
            LogUtil.DebugLog($"{d_method_name}: windowGroup is null for '{windowName}'");
            return;
        }

        if (!windowGroup.isShowing)
        {
            return;
        }

        var workstationWindowGroup = windowGroup.Controller as XUiC_WorkstationWindowGroup;
        if (workstationWindowGroup == null)
        {
            LogUtil.DebugLog($"{d_method_name}: WorkstationWindowGroup is null for '{windowName}'");
            return;
        }

        if (workstationWindowGroup.WorkstationData == null)
        {
            LogUtil.Error($"{d_method_name}: WorkstationData is null for '{windowName}'");
            return;
        }

        LogUtil.DebugLog($"{d_method_name}: Syncing UI from TE for '{windowName}'");
        workstationWindowGroup.syncUIfromTE();
    }
}