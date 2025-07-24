using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;
using Platform;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class DewCollectorUtils
{
    public static List<TileEntityDewCollector> GetAvailableDewCollectorStorages()
    {
        const string d_method_name = "GetAvailableDewCollectorStorages";

        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.DebugLog($"{d_method_name}: World is null, aborting.");
            return new List<TileEntityDewCollector>();
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.DebugLog($"{d_method_name}: Player is null, aborting.");
            return new List<TileEntityDewCollector>();
        }

        LogUtil.DebugLog($"{d_method_name}: Starting");

        var playerPos = player.position;
        var configRange = ModConfig.Range();
        var configOnlyCrates = ModConfig.OnlyStorageCrates();
        var internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;
        var playerEntityId = player.entityId;

        var chunkCacheCopy = world.ChunkCache.GetChunkArrayCopySync();
        if (chunkCacheCopy == null)
        {
            LogUtil.DebugLog($"{d_method_name}: chunkCacheCopy is null, aborting.");
            return new List<TileEntityDewCollector>();
        }

        var result = new List<TileEntityDewCollector>();

        foreach (var chunk in chunkCacheCopy)
        {
            if (chunk == null)
            {
                continue;
            }

            foreach (var tileEntity in chunk.GetTileEntities().list)
            {
                // Only consider dew collectors
                if (tileEntity is not TileEntityDewCollector dewCollector)
                {
                    continue;
                }

                // Skip if out of range
                if (configRange > 0 && Vector3.Distance(playerPos, dewCollector.ToWorldPos()) >= configRange)
                {
                    continue;
                }

                // Skip if being accessed by another user
                if (dewCollector.bUserAccessing)
                {
                    continue;
                }

                // Consider a dew collector empty if all items are empty or null
                bool isEmpty = dewCollector.items.All(item => item?.IsEmpty() ?? true);
                if (isEmpty)
                {
                    continue;
                }

                // Skip if locked by another player
                if (ContainerUtils.LockedTileEntities.Count > 0)
                {
                    var pos = dewCollector.ToWorldPos();
                    if (ContainerUtils.LockedTileEntities.TryGetValue(pos, out int entityId) && entityId != playerEntityId)
                    {
                        continue;
                    }
                }

                result.Add(dewCollector);
            }
        }

        return result;
    }

    /// <summary>
    /// Marks a dew collector as modified after items are removed from it
    /// </summary>
    public static void MarkDewCollectorModified(TileEntityDewCollector dewCollector)
    {
        const string d_method_name = "MarkDewCollectorModified";
        LogUtil.DebugLog($"{d_method_name} | Marking Dew Collector '{dewCollector?.GetType().Name}' as modified");

        if (dewCollector == null)
        {
            LogUtil.Error($"{d_method_name}: dew collector is null");
            return;
        }

        PackDewCollector(dewCollector);

        dewCollector.SetChunkModified();
        dewCollector.SetModified();
    }

    private static void PackDewCollector(TileEntityDewCollector dewCollector)
    {
        const string d_method_name = "MarkDewCollectorModified.PackDewCollector";

        if (dewCollector == null)
        {
            LogUtil.Error($"{d_method_name}: dew collector is null");
            return;
        }

        var s = "";

        s = string.Join(",", dewCollector.fillValuesArr.Select(f => f.ToString()));
        LogUtil.DebugLog($"{d_method_name} | Fill values after item removal: {s}");

        s = string.Join(",", dewCollector.items.Select(stack => stack.count.ToString()));
        LogUtil.DebugLog($"{d_method_name} | Slot counts after item removal: {s}");

        /* Scenario: 
         * - Dew Collector has these items counts in the slots 1, 2, 0; slot 0 is partially filled, slot 1 is full, slot 2 is producing
         * - Why is slot 0 partially filled? 
         *   a) Maybe the player previously removed only 1 out of 2 already produced items out of it.
         *   b) Maybe this mod removed 1 item from it for crafting
         *   --> Either way, that slot is not filled completely, but it is also not producing anything
         *   --> Case a) is already how the game behaves unmodded, so for now not changing that behaviour
         * - In the future, we might want to change this behaviour to always remove full stacks from the dew collector
         * - "Compressing" the slots, where we change the slots counts to be 2,1,0 would not mean slot 1 is producing
         * - Alternatively, making slot 0 start producing at 50% would mean destroying the already produced water in it
         * - We can consolidate all the producted items into the available slots, up to max stack size of the item, but that
         *   seems like too much work with not much gain, and probably not very predictable.
         *   Also this would change the behaviour a lot, meaning dew collectors could produce many more items than
         *   usual, which might not be what players expect, and might be too powerful.
        */
    }
}