using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Reload;

[HarmonyPatch(typeof(Animator3PRangedReloadState))]
public class Animator3PRangedReloadStatePatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Animator3PRangedReloadState.GetAmmoCountToReload))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> Animator3PRangedReloadState_GetAmmoCountToReload_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(Animator3PRangedReloadState)}.{nameof(Animator3PRangedReloadState.GetAmmoCountToReload)}";
        return AnimatorCommon.GetCountToReload_Transpiler(targetMethodString, instructions);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Animator3PRangedReloadState.GetAmmoCount))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Animator3PRangedReloadState_GetAmmoCount_Postfix(ref int __result, ItemValue ammo, int modifiedMagazineSize)
    {
        const string d_MethodName = nameof(Animator3PRangedReloadState_GetAmmoCount_Postfix);

        if (!ValidationHelper.ValidateItemAndContext(ammo, d_MethodName, config => config.EnableForReload,
            out _, out _, out string itemName))
        {
            return;
        }

        __result = AnimatorCommon.GetAmmoCount(ammo, __result, modifiedMagazineSize);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Enhanced ammo count for {itemName} from {__result - AnimatorCommon.GetAmmoCount(ammo, 0, modifiedMagazineSize)} to {__result}");
#endif
    }
}