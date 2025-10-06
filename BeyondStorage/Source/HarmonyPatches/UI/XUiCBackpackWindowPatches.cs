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
    private const string SmartLootSortButtonId = "btnBeyondSmartLootSort";

    /// <summary>
    /// Gets the smart loot sort button from the backpack window
    /// </summary>
    /// <param name="instance">The backpack window instance</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <returns>The button control if found, null otherwise</returns>
    private static XUiController GetSmartLootSortButton(XUiC_BackpackWindow instance, string methodName)
    {
        var stdControls = instance.GetChildByType<XUiC_ContainerStandardControls>();
        if (stdControls == null)
        {
            ModLogger.DebugLog($"{methodName}: Standard controls not found in backpack window, please verify your game files");
            return null;
        }

        var btnBeyondSmartLootSort = stdControls.GetChildById(SmartLootSortButtonId);
        if (btnBeyondSmartLootSort == null)
        {
            ModLogger.DebugLog($"{methodName}: Smart loot sorting button not found, check windows.xml");
            return null;
        }

        return btnBeyondSmartLootSort;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_BackpackWindow.Cleanup))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_BackpackWindow_Cleanup_Prefix(XUiC_BackpackWindow __instance)
    {
        const string d_MethodName = nameof(XUiC_BackpackWindow_Cleanup_Prefix);

        try
        {
            var btnBeyondSmartLootSort = GetSmartLootSortButton(__instance, d_MethodName);
            if (btnBeyondSmartLootSort != null)
            {
                btnBeyondSmartLootSort.OnPress -= SmartSortingCommon.SmartLootSort_EventHandler;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Smart loot sorting button event handler removed");
#endif
            }
        }
        catch (System.Exception ex)
        {
            ModLogger.Warning($"{d_MethodName}: Error during cleanup: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_BackpackWindow.Init))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_BackpackWindow_Init_Postfix(XUiC_BackpackWindow __instance)
    {
        const string d_MethodName = nameof(XUiC_BackpackWindow_Init_Postfix);

        var btnBeyondSmartLootSort = GetSmartLootSortButton(__instance, d_MethodName);
        if (btnBeyondSmartLootSort != null)
        {
            btnBeyondSmartLootSort.OnPress += SmartSortingCommon.SmartLootSort_EventHandler;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Smart loot sorting button initialized");
#endif
        }
    }
}