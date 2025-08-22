using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Storage;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Reload;

[HarmonyPatch(typeof(AnimatorRangedReloadState))]
internal static class AnimatorRangedReloadStatePatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(AnimatorRangedReloadState.GetAmmoCountToReload))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> AnimatorRangedReloadState_GetAmmoCountToReload_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(AnimatorRangedReloadState)}.{nameof(AnimatorRangedReloadState.GetAmmoCountToReload)}";
        return AnimatorCommon.GetCountToReload_Transpiler(targetMethodString, instructions);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(AnimatorRangedReloadState.GetAmmoCount))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void AnimatorRangedReloadState_GetAmmoCount_Postfix(ref int __result, ItemValue ammo, int modifiedMagazineSize)
    {
        const string d_MethodName = nameof(AnimatorRangedReloadState_GetAmmoCount_Postfix);

        var context = StorageContextFactory.Create(d_MethodName);
        if (!StorageContextFactory.EnsureValidContext(context, d_MethodName) || !ModConfig.EnableForReload())
        {
            return;
        }

        __result = AnimatorCommon.GetAmmoCount(ammo, __result, modifiedMagazineSize);
    }
}