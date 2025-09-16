using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Reload;

[HarmonyPatch(typeof(AnimatorRangedReloadState))]
internal static class AnimatorRangedReloadStatePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(AnimatorRangedReloadState.GetAmmoCountToReload))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool AnimatorRangedReloadState_GetAmmoCountToReload_Prefix(
        AnimatorRangedReloadState __instance,
        EntityAlive ea,
        ItemValue ammo,
        int modifiedMagazineSize,
        ref int __result)
    {
        // Get the private fields using reflection
        var actionData = AccessTools.Field(typeof(AnimatorRangedReloadState), "actionData").GetValue(__instance) as ItemActionRanged.ItemActionDataRanged;
        var actionRanged = AccessTools.Field(typeof(AnimatorRangedReloadState), "actionRanged").GetValue(__instance) as ItemActionRanged;

        if (actionData == null || actionRanged == null)
        {
            return true; // Continue with original method
        }

        // Use our common method that includes storage integration
        __result = AnimatorCommon.RemoveAndCountAmmoForReload(actionRanged, actionData, ea, ammo, modifiedMagazineSize);

        return false; // Skip original method
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(AnimatorRangedReloadState.GetAmmoCount))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void AnimatorRangedReloadState_GetAmmoCount_Postfix(ref int __result, ItemValue ammo, int modifiedMagazineSize)
    {
        __result = AnimatorCommon.GetAmmoCount(ammo, __result, modifiedMagazineSize);
    }
}