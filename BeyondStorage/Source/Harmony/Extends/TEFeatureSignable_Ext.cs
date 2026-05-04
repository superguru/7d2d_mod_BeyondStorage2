using BeyondStorage.Entities;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extend;


[HarmonyPatch(typeof(TEFeatureSignable))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class TEFeatureSignable_Ext
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