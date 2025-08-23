using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Reload;

[HarmonyPatch(typeof(Animator3PRangedReloadState))]
internal static class Animator3PRangedReloadStatePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Animator3PRangedReloadState.GetAmmoCountToReload))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool Animator3PRangedReloadState_GetAmmoCountToReload_Prefix(
        Animator3PRangedReloadState __instance,
        EntityAlive ea,
        ItemValue ammo,
        int modifiedMagazineSize,
        ref int __result)
    {
        // Get the private fields using reflection
        var actionData = AccessTools.Field(typeof(Animator3PRangedReloadState), "actionData").GetValue(__instance) as ItemActionRanged.ItemActionDataRanged;
        var actionRanged = AccessTools.Field(typeof(Animator3PRangedReloadState), "actionRanged").GetValue(__instance) as ItemActionRanged;

        if (actionData == null || actionRanged == null)
        {
            return true; // Continue with original method
        }

        // Use our common method that includes storage integration
        __result = AnimatorCommon.RemoveAndCountAmmoForReload(actionRanged, actionData, ea, ammo, modifiedMagazineSize);

        return false; // Skip original method
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Animator3PRangedReloadState.GetAmmoCount))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Animator3PRangedReloadState_GetAmmoCount_Postfix(ref int __result, ItemValue ammo, int modifiedMagazineSize)
    {
        __result = AnimatorCommon.GetAmmoCount(ammo, __result, modifiedMagazineSize);
    }
}