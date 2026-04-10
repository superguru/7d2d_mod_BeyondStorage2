using BeyondStorage.Scripts.Entities;
using HarmonyLib;

namespace BeyondStorage.Source.HarmonyPatches.Features;


[HarmonyPatch(typeof(TEFeatureSignable))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class TEFeatureSignable_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TEFeatureSignable.SetText), [typeof(string), typeof(bool), typeof(PlatformUserIdentifierAbs)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void TEFeatureSignable_SetText_Postfix(TEFeatureSignable __instance, string _text, bool _syncData = true, PlatformUserIdentifierAbs _signingPlayer = null)
    {
        EntityNameCache.RemoveName(__instance);
    }
}