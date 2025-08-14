using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiC_CraftingQueue))]
#if DEBUG
[HarmonyDebug]
#endif
public class XUiCCraftingQueuePatches
{
    // Fixed an internal bug where crafting queue is not kept in sync with some other UI elements.
    // Still a bug in 2.x - confirmed in 2.0 - 2.2
    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_CraftingQueue.AddRecipeToCraftAtIndex))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool XUiC_CraftingQueue_AddRecipeToCraftAtIndex_Prefix(XUiC_CraftingQueue __instance, ref bool __result, int _index, global::Recipe _recipe)
    {
        //TODO: Remove after debugging tests
        ModLogger.DebugLog($"XUiC_CraftingQueue.AddRecipeToCraftAtIndex: index: {_index}, recipe: {_recipe?.GetName() ?? "null"}");

        var inBounds = _index < __instance.queueItems.Length;
        if (inBounds)
        {
            __result = true;
            return true;
        }

        string recipeName = _recipe?.GetName() ?? "null";
        ModLogger.DebugLog($"Game bug patch: XUiC_CraftingQueue.AddRecipeToCraftAtIndex(index: {_index}; queueLen: {__instance.queueItems.Length}, recipe [{recipeName}]); disallowing operation");

        __result = false;
        return false;
    }
}