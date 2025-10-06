using System.Linq;
using BeyondStorage.Scripts.Diagnostics;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.BugPatches;

[HarmonyPatch(typeof(XUiC_CraftingQueue))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiCCraftingQueuePatches
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
        const string d_MethodName = nameof(XUiC_CraftingQueue_AddRecipeToCraftAtIndex_Prefix);

        var inBounds = _index < __instance.queueItems.Length;
        if (inBounds)
        {
            __result = true;
            return true;
        }

        string recipeName = _recipe?.GetName() ?? "null";
        var message = $"Game bug patch: {d_MethodName}(index: {_index}; queueLen: {__instance.queueItems.Length}, recipe [{recipeName}]); disallowing operation";
#if DEBUG
        string instanceDiagnostics = $"Instance: {__instance?.GetType().FullName ?? "null"}, " +
            $"QueueItems: {(__instance?.queueItems?.Length ?? -1)}, " +
            $"ToolGrid: {(__instance?.toolGrid?.GetType().Name ?? "null")}, " +
            $"IsCrafting: {(__instance?.IsCrafting() ?? false)}, " +
            $"WindowGroup: {__instance?.windowGroup?.ID ?? "null"}, " +
            $"EntityId: {(__instance?.xui?.playerUI?.entityPlayer?.entityId ?? -1)}, " +
            $"IsVisible: {(__instance?.windowGroup?.isShowing ?? false)}, " +
            $"IsDirty: {(__instance?.IsDirty ?? false)}, " +
            $"Parent: {(__instance?.parent?.GetType().Name ?? "null")}, " +
            $"ViewComponent: {(__instance?.viewComponent?.GetType().Name ?? "null")}";

        // Check queue contents and add crafting status
        if (__instance?.queueItems != null)
        {
            var queueContents = string.Join(", ", __instance.queueItems.Select((item, idx) =>
            {
                var recipeStack = item as XUiC_RecipeStack;
                var recipeName = recipeStack?.GetRecipe()?.GetName() ?? "empty";
                var isCrafting = recipeStack?.IsCrafting ?? false;
                var count = recipeStack?.recipeCount ?? 0;
                return $"[{idx}]: {recipeName}{(isCrafting ? " (crafting)" : "")}{(count > 0 ? $" x{count}" : "")}";
            }));
            instanceDiagnostics += $", QueueContents: {{{queueContents}}}";
        }

        message = $"{message}\nDiagnostics: {instanceDiagnostics}";
        message = StackTraceProvider.AppendStackTrace(message);
#endif
        ModLogger.DebugLog(message);

        __result = false;
        return false;
    }
}