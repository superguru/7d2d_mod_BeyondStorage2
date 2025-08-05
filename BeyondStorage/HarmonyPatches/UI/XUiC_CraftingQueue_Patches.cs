using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiC_CraftingQueue))]
public class XUiCCraftingQueuePatches
{
    // Fixed an internal bug where crafting queue is not kept in sync with some other UI elements.
    // Still a bug in 2.x - confirmed in 2.0 - 2.2
    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_CraftingQueue.AddRecipeToCraftAtIndex))]
    private static bool XUiC_CraftingQueue_AddRecipeToCraftAtIndex_Prefix(XUiC_CraftingQueue __instance, ref bool __result, int _index)
    {
        var inBounds = _index < __instance.queueItems.Length;
        if (inBounds)
        {
            return true;
        }

        ModLogger.Error("XUiC_CraftingQueue.AddRecipeToCraftAtIndex OutOfBounds!");
        ModLogger.DebugLog($"Queue Length: {__instance.queueItems.Length}; _index: {_index}; {_index >= __instance.queueItems.Length}");

        __result = false;
        return false;
    }
}