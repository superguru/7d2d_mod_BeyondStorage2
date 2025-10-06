using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.Game.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiC_BackpackWindow))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiCBackpackWindowPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_BackpackWindow.Init))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_BackpackWindow_Init_Postfix(XUiC_BackpackWindow __instance)
    {
        const string d_MethodName = nameof(XUiC_BackpackWindow_Init_Postfix);

        ModLogger.DebugLog($"{d_MethodName}: Start");

        var stdControls = __instance.GetChildByType<XUiC_ContainerStandardControls>();
        if (stdControls == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Standard controls not found in backpack window");
            return;
        }

        var btnBeyondSmartLootSort = stdControls.GetChildById("btnBeyondSmartLootSort");
        if (btnBeyondSmartLootSort == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Smart loot sorting button not found, check windows.xml");
            return;
        }

        btnBeyondSmartLootSort.OnPress += SmartSortingCommon.SmartLootSort_EventHandler;

        ModLogger.DebugLog($"{d_MethodName}: Smart loot sorting button initialized");
    }
}