using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Functions;

[HarmonyPatch(typeof(XUiM_PlayerInventory))]
internal static class XUiM_PlayerInventory_Currency_Patches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.RefreshCurrency))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiM_PlayerInventory_RefreshCurrency_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(XUiM_PlayerInventory)}.{nameof(XUiM_PlayerInventory.RefreshCurrency)}";

        // Create search pattern for GetItemStacksForFilter method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0),  // this
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.currencyItem))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [typeof(ItemValue)])),
            new CodeInstruction(OpCodes.Stloc_0),  // itemCount (set)
            new CodeInstruction(OpCodes.Ldloc_0),  // itemCount (load onto stack)
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0),  // this
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.currencyItem))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCommon), nameof(ItemCommon.ItemCommon_GetStorageItemCount))),
            new CodeInstruction(OpCodes.Add),
            new CodeInstruction(OpCodes.Stloc_0),  // itemCount (set)
            new CodeInstruction(OpCodes.Ldloc_0),  // itemCount (load onto stack)
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 5,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 2,
            ExtraLogging = false,
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}