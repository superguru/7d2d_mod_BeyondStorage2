using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_ContainerStandardControls))]
public class Stack_MoveAll_Patch
{
    private static bool s_isMovingAll = false;
    private static readonly object s_lockObject = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ContainerStandardControls.MoveAll))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_MoveAll_Event_Prefix(XUiC_ContainerStandardControls __instance)
    {
        lock (s_lockObject)
        {
            s_isMovingAll = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ContainerStandardControls.MoveAll))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Handle_MoveAll_Event_Postfix(XUiC_ContainerStandardControls __instance)
    {
        lock (s_lockObject)
        {
            s_isMovingAll = false;
        }

        UIRefreshHelper.LogAndRefreshUI(StackOps.MoveAll_Operation, itemStack: CurrencyCache.GetEmptyCurrencyStack(), callCount: 0);
    }

    public static bool IsMovingAll()
    {
        lock (s_lockObject)
        {
            return s_isMovingAll;
        }
    }
}