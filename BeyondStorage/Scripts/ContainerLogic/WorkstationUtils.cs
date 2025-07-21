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
            //LogUtil.DebugLog($"TE_WS: {workstation}; Locked Count: {ContainerUtils.LockedTileEntities.Count}; {tileEntity.IsUserAccessing()}");
#endif
            yield return workstation;
        }
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