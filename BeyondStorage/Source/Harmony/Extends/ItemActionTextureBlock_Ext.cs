using System.Collections;
using BeyondStorage.Game.Item;
using BeyondStorage.Infrastructure;
using HarmonyLib;
using UnityEngine;
using static ItemActionRanged;
using static ItemActionTextureBlock;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(ItemActionTextureBlock))]
internal static class ItemActionTextureBlock_Ext
{
    private const float FloodFillVectorScale = 0.3f;
    private const float MultiplePaintDefaultRadius = 1.25f;
    private const float SprayPaintDefaultRadius = 7.5f;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionTextureBlock.checkAmmo))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionTextureBlock_checkAmmo_Prefix(ItemActionTextureBlock __instance, ItemActionData _actionData, ref bool __result)
    {
        // Handle infinite ammo and creative modes first (same as original)
        if (__instance.InfiniteAmmo || GameStats.GetInt(EnumGameStats.GameModeId) == 2 || GameStats.GetInt(EnumGameStats.GameModeId) == 8)
        {
            __result = true;
            return false; // Skip original method
        }

        // Get entity-held ammo count (equivalent to original logic)
        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        int bagAmmoCount = holdingEntity.bag.GetItemCount(__instance.currentMagazineItem);
        int inventoryAmmoCount = holdingEntity.inventory.GetItemCount(__instance.currentMagazineItem);
        int entityAvailableCount = bagAmmoCount + inventoryAmmoCount;

        // Use our custom ammo checking logic that includes storage
        __result = ItemTexture.ItemTexture_checkAmmo(entityAvailableCount, _actionData, __instance.currentMagazineItem);
        return false; // Skip original method
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionTextureBlock.decreaseAmmo))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionTextureBlock_decreaseAmmo_Prefix(ItemActionTextureBlock __instance, ItemActionData _actionData, ref bool __result)
    {
        // Handle infinite ammo and creative modes first (same as original)
        if (__instance.InfiniteAmmo || GameStats.GetInt(EnumGameStats.GameModeId) == 2 || GameStats.GetInt(EnumGameStats.GameModeId) == 8)
        {
            __result = true;
            return false; // Skip original method
        }

        // Get the action data and paint cost (same as original)
        ItemActionTextureBlockData textureBlockData = (ItemActionTextureBlockData)_actionData;
        int paintCost = BlockTextureData.list[textureBlockData.idx].PaintCost;

        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        ItemValue ammoType = __instance.currentMagazineItem;

        // Calculate entity-held ammo (same as original)
        int bagAmmoCount = holdingEntity.bag.GetItemCount(ammoType);
        int inventoryAmmoCount = holdingEntity.inventory.GetItemCount(ammoType);
        int entityAvailableCount = bagAmmoCount + inventoryAmmoCount;

        // Get total available count including storage
        int totalAvailableCount = ItemTexture.ItemTexture_GetAmmoCount(ammoType, entityAvailableCount);

        // Check if we have enough total ammo
        if (totalAvailableCount < paintCost)
        {
            __result = false;
            return false; // Skip original method
        }

        // Remove ammo from entity inventory first (same priority as original)
        int remainingNeeded = paintCost;
        remainingNeeded -= holdingEntity.bag.DecItem(ammoType, remainingNeeded);

        if (remainingNeeded > 0)
        {
            remainingNeeded -= holdingEntity.inventory.DecItem(ammoType, remainingNeeded);
        }

        // Remove any remaining needed from storage
        if (remainingNeeded > 0)
        {
            ItemTexture.ItemTexture_RemoveAmmo(ammoType, remainingNeeded, false, null);
        }

        __result = true;
        return false; // Skip original method
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionTextureBlock.fireShotLater))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionTextureBlock_fireShotLater_Prefix(
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
                yield return HandleSmartMultiplePaint(paintContext, world, chunkCluster, holdingEntity.entityId, playerDataFromEntityID, blockPos, blockFace, bv, hitInfo, MultiplePaintDefaultRadius);
                break;

            case EnumPaintMode.Spray:
                yield return HandleSmartSprayPaint(paintContext, world, chunkCluster, holdingEntity.entityId, playerDataFromEntityID, blockPos, blockFace, bv, hitInfo, SprayPaintDefaultRadius);
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

        vector1 = ItemActionTextureBlock.ProjectVectorOnPlane(normalized, vector1).normalized * FloodFillVectorScale;
        vector2 = ItemActionTextureBlock.ProjectVectorOnPlane(normalized, vector2).normalized * FloodFillVectorScale;

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

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionTextureBlock.floodFill))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionTextureBlock_floodFill_Prefix(
    ItemActionTextureBlock __instance,
    World _world,
    ChunkCluster _cc,
    int _entityId,
    ItemActionTextureBlockData _actionData,
    PersistentPlayerData _lpRelative,
    int _sourcePaint,
    Vector3 _hitPosition,
    Vector3 _hitFaceNormal,
    Vector3 _dir1,
    Vector3 _dir2,
    int _channel)
    {
        const string d_MethodName = nameof(ItemActionTextureBlock_floodFill_Prefix);

        try
        {
            // Create PaintOperationContext with the necessary data
            var paintContext = new PaintOperationContext(__instance, _actionData, __instance.currentMagazineItem);

            ModLogger.DebugLog($"{d_MethodName}: Created PaintOperationContext {paintContext.OperationId} for flood fill operation");

            // Call our static smart flood fill implementation with the context
            ItemTexture.SmartFloodFill(paintContext, _world, _cc, _entityId, _lpRelative, _sourcePaint, _hitPosition, _hitFaceNormal, _dir1, _dir2, _channel);

            ModLogger.DebugLog($"{d_MethodName}: Successfully executed SmartFloodFill with context {paintContext.OperationId}");
        }
        catch (System.Exception ex)
        {
            ModLogger.Error($"{d_MethodName}: Error in SmartFloodFill: {ex}");
            // Return true to let original method run as fallback
            return true;
        }

        // Return false to skip the original floodFill method
        return false;
    }
}