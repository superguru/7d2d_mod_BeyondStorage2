using System.Collections;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;
using UnityEngine;
using static ItemActionRanged;
using static ItemActionTextureBlock;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(ItemActionTextureBlock))]
public class ItemActionTextureBlockFireShotLaterPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionTextureBlock.fireShotLater))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static bool ItemActionTextureBlock_fireShotLater_Prefix(
        ItemActionTextureBlock __instance,
        int _shotIdx,
        ItemActionDataRanged _actionData,
        ref IEnumerator __result)
    {
        const string d_MethodName = nameof(ItemActionTextureBlock_fireShotLater_Prefix);

        try
        {
            var itemActionTextureBlockData = (ItemActionTextureBlockData)_actionData;

            // Only intercept paint modes that benefit from batching
            if (itemActionTextureBlockData.paintMode == EnumPaintMode.Fill ||
                itemActionTextureBlockData.paintMode == EnumPaintMode.Multiple ||
                itemActionTextureBlockData.paintMode == EnumPaintMode.Spray)
            {
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Intercepting {itemActionTextureBlockData.paintMode} mode for batched painting");
#endif
                var exposedWrapper = new ItemActionTextureBlockExposed(__instance);

                __result = SmartFireShotLater(__instance, _shotIdx, _actionData);
                return false; // Skip original method
            }
        }
        catch (System.Exception ex)
        {
            ModLogger.Error($"{d_MethodName}: Error in SmartFireShotLater: {ex}");
        }

        // Let original method handle Single mode and any errors
        return true;
    }

    private static IEnumerator SmartFireShotLater(ItemActionTextureBlock instance, int _shotIdx, ItemActionDataRanged _actionData)
    {
        yield return new WaitForSeconds(instance.rayCastDelay);

        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        PersistentPlayerData playerDataFromEntityID = GameManager.Instance.GetPersistentPlayerList().GetPlayerDataFromEntityID(holdingEntity.entityId);
        holdingEntity.GetLookVector((_actionData.muzzle != null) ? _actionData.muzzle.forward : Vector3.zero);

        // Get hit block face using reflection to access private method
        var getHitBlockFaceMethod = AccessTools.Method(typeof(ItemActionTextureBlock), "getHitBlockFace");
        var parameters = new object[] { _actionData, null, null, null, null };
        var result = (int)getHitBlockFaceMethod.Invoke(instance, parameters);

        if (result == -1 || parameters[4] == null || !((WorldRayHitInfo)parameters[4]).bHitValid)
        {
            yield break;
        }

        var blockPos = (Vector3i)parameters[1];
        var bv = (BlockValue)parameters[2];
        var blockFace = (BlockFace)parameters[3];
        var hitInfo = (WorldRayHitInfo)parameters[4];

        ItemActionTextureBlockData itemActionTextureBlockData = (ItemActionTextureBlockData)_actionData;

        if (instance.bRemoveTexture)
        {
            itemActionTextureBlockData.idx = 0;
        }

        World world = GameManager.Instance.World;
        ChunkCluster chunkCluster = world.ChunkClusters[hitInfo.hit.clrIdx];
        if (chunkCluster == null)
        {
            yield break;
        }

        // Create PaintOperationContext early and pass it down
        // This will create an exposed wrapper that preserves all original instance data
        var paintContext = new PaintOperationContext(instance, itemActionTextureBlockData, instance.currentMagazineItem);

        BlockToolSelection.Instance.BeginUndo(chunkCluster.ClusterIdx);

        // Handle different paint modes with smart batching
        switch (itemActionTextureBlockData.paintMode)
        {
            case EnumPaintMode.Fill:
                yield return HandleSmartFloodFill(paintContext, world, chunkCluster, holdingEntity.entityId, playerDataFromEntityID, blockPos, blockFace, bv, hitInfo);
                break;

            case EnumPaintMode.Multiple:
                yield return HandleSmartMultiplePaint(paintContext, world, chunkCluster, holdingEntity.entityId, playerDataFromEntityID, blockPos, blockFace, bv, hitInfo, 1.25f);
                break;

            case EnumPaintMode.Spray:
                yield return HandleSmartSprayPaint(paintContext, world, chunkCluster, holdingEntity.entityId, playerDataFromEntityID, blockPos, blockFace, bv, hitInfo, 7.5f);
                break;
        }

        BlockToolSelection.Instance.EndUndo(chunkCluster.ClusterIdx);
    }

    private static IEnumerator HandleSmartFloodFill(PaintOperationContext paintContext, World world, ChunkCluster chunkCluster, int entityId, PersistentPlayerData playerData, Vector3i blockPos, BlockFace blockFace, BlockValue bv, WorldRayHitInfo hitInfo)
    {
        // Calculate flood fill vectors
        Vector3 normalized = GameUtils.GetNormalFromHitInfo(blockPos, hitInfo.hitCollider, hitInfo.hitTriangleIdx, out var _).normalized;
        Vector3 vector1, vector2;

        if (Utils.FastAbs(normalized.x) >= Utils.FastAbs(normalized.y) && Utils.FastAbs(normalized.x) >= Utils.FastAbs(normalized.z))
        {
            vector1 = Vector3.up;
            vector2 = Vector3.forward;
        }
        else if (Utils.FastAbs(normalized.y) >= Utils.FastAbs(normalized.x) && Utils.FastAbs(normalized.y) >= Utils.FastAbs(normalized.z))
        {
            vector1 = Vector3.right;
            vector2 = Vector3.forward;
        }
        else
        {
            vector1 = Vector3.right;
            vector2 = Vector3.up;
        }

        vector1 = ItemActionTextureBlock.ProjectVectorOnPlane(normalized, vector1).normalized * 0.3f;
        vector2 = ItemActionTextureBlock.ProjectVectorOnPlane(normalized, vector2).normalized * 0.3f;

        for (int channel = 0; channel < 1; channel++)
        {
            if (!paintContext.ActionData.channelMask.IncludesChannel(channel))
            {
                continue;
            }

            int sourcePaint = chunkCluster.GetBlockFaceTexture(blockPos, blockFace, channel);
            if (paintContext.ActionData.idx != sourcePaint)
            {
                if (sourcePaint == 0)
                {
                    sourcePaint = GameUtils.FindPaintIdForBlockFace(bv, blockFace, out var _, channel);
                }

                if (paintContext.ActionData.idx != sourcePaint)
                {
                    ItemTexture.SmartFloodFill(paintContext, world, chunkCluster, entityId, playerData, sourcePaint, hitInfo.hit.pos, normalized, vector1, vector2, channel);
                }
            }
        }

        yield break;
    }

    private static IEnumerator HandleSmartMultiplePaint(PaintOperationContext paintContext, World world, ChunkCluster chunkCluster, int entityId, PersistentPlayerData playerData, Vector3i blockPos, BlockFace blockFace, BlockValue bv, WorldRayHitInfo hitInfo, float radius)
    {
        yield return HandleSmartAreaPaint(paintContext, world, chunkCluster, entityId, playerData, blockPos, blockFace, bv, hitInfo, radius, "Multiple");
    }

    private static IEnumerator HandleSmartSprayPaint(PaintOperationContext paintContext, World world, ChunkCluster chunkCluster, int entityId, PersistentPlayerData playerData, Vector3i blockPos, BlockFace blockFace, BlockValue bv, WorldRayHitInfo hitInfo, float radius)
    {
        yield return HandleSmartAreaPaint(paintContext, world, chunkCluster, entityId, playerData, blockPos, blockFace, bv, hitInfo, radius, "Spray");
    }

    private static IEnumerator HandleSmartAreaPaint(PaintOperationContext paintContext, World world, ChunkCluster chunkCluster, int entityId, PersistentPlayerData playerData, Vector3i blockPos, BlockFace blockFace, BlockValue bv, WorldRayHitInfo hitInfo, float radius, string mode)
    {
        if (hitInfo.hitTriangleIdx == -1)
        {
            yield break;
        }

        // Calculate area paint vectors
        Vector3 hitFaceNormal = GameUtils.GetNormalFromHitInfo(blockPos, hitInfo.hitCollider, hitInfo.hitTriangleIdx, out var _);
        Vector3 normalized = hitFaceNormal.normalized;
        Vector3 vector1, vector2;

        if (Utils.FastAbs(normalized.x) >= Utils.FastAbs(normalized.y) && Utils.FastAbs(normalized.x) >= Utils.FastAbs(normalized.z))
        {
            vector1 = Vector3.up;
            vector2 = Vector3.forward;
        }
        else if (Utils.FastAbs(normalized.y) >= Utils.FastAbs(normalized.x) && Utils.FastAbs(normalized.y) >= Utils.FastAbs(normalized.z))
        {
            vector1 = Vector3.right;
            vector2 = Vector3.forward;
        }
        else
        {
            vector1 = Vector3.right;
            vector2 = Vector3.up;
        }

        vector1 = ItemActionTextureBlock.ProjectVectorOnPlane(normalized, vector1).normalized;
        vector2 = ItemActionTextureBlock.ProjectVectorOnPlane(normalized, vector2).normalized;

        Vector3 pos = hitInfo.hit.pos;
        Vector3 origin = hitInfo.ray.origin;

        // Use our smart batching system
        ItemTexture.SmartAreaPaint(paintContext, world, chunkCluster, entityId, playerData, pos, origin, vector1, vector2, radius, mode);

        yield break;
    }
}