using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_RecipeTrackerIngredientEntry))]
internal static class XUiC_RecipeTracker_IngredientEntry_Patches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_RecipeTrackerIngredientEntry.Ingredient), MethodType.Setter)]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_RecipeTrackerIngredientEntry_Ingredient_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(XUiC_RecipeTrackerIngredientEntry)}.{nameof(XUiC_RecipeTrackerIngredientEntry.Ingredient)}";

        // Create search pattern for GetItemStacksForFilter method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [typeof(ItemValue)])),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(XUiC_RecipeTrackerIngredientEntry), nameof(XUiC_RecipeTrackerIngredientEntry.currentCount))),
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0),  // this
            new CodeInstruction(OpCodes.Ldarg_0),  // this
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUiC_RecipeTrackerIngredientEntry), nameof(XUiC_RecipeTrackerIngredientEntry.ingredient))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemStack), nameof(ItemStack.itemValue))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCommon), nameof(ItemCommon.ItemCommon_GetTotalAvailableItemCount))),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(XUiC_RecipeTrackerIngredientEntry), nameof(XUiC_RecipeTrackerIngredientEntry.currentCount))),
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 2,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 2,
            ExtraLogging = false
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}