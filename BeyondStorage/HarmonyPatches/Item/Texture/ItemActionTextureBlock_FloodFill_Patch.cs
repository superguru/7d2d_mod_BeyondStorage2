using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;
using UnityEngine;
using static ItemActionTextureBlock;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(ItemActionTextureBlock))]
public class ItemActionTextureBlockFloodFillPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("floodFill")]
#if DEBUG
    [HarmonyDebug]
#endif
    public static bool ItemActionTextureBlock_floodFill_Prefix(
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
            // Call our static smart flood fill implementation
            ItemTexture.SmartFloodFill(__instance, _world, _cc, _entityId, _actionData, _lpRelative, _sourcePaint, _hitPosition, _hitFaceNormal, _dir1, _dir2, _channel);

            ModLogger.DebugLog($"{d_MethodName}: Successfully executed SmartFloodFill");
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