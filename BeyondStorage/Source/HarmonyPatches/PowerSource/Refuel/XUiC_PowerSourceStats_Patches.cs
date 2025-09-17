using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.PowerSource;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;
using UniLinq;

namespace BeyondStorage.HarmonyPatches.PowerSource;

[HarmonyPatch(typeof(XUiC_PowerSourceStats))]
internal static class XUiCPowerSourceStatsPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_PowerSourceStats.BtnRefuel_OnPress))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_PowerSourceStats_BtnRefuel_OnPress_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(XUiC_PowerSourceStats)}.{nameof(XUiC_PowerSourceStats.BtnRefuel_OnPress)}";
        var instructionsList = instructions as List<CodeInstruction> ?? instructions?.ToList() ?? new List<CodeInstruction>();

        // Create search pattern to find: callvirt Bag.DecItem
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Bag), nameof(Bag.DecItem)))
        };

        // Create replacement instructions that will be inserted after the Bag.DecItem call
        var replacementInstructions = new List<CodeInstruction>
        {
            // We need to construct the replacement instructions dynamically since we need to reference
            // the local variables from the original method. For now, we'll create placeholders.
            new CodeInstruction(OpCodes.Ldloc_S, 4), // _itemValue
            new CodeInstruction(OpCodes.Ldloc_S, 5), // _count2 (last removed count) 
            new CodeInstruction(OpCodes.Ldloc_2), // ldloc.2 _count1
            new CodeInstruction(OpCodes.Conv_I4), // conv.i4 (int) _count1
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerSourceRefuel), nameof(PowerSourceRefuel.RefuelRemoveRemaining), [typeof(ItemValue), typeof(int), typeof(int)])),
            new CodeInstruction(OpCodes.Stloc_S, 5) // _count2 (update result)
        };

        // Create the patch request
        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = instructionsList,
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 2, // Insert after the Bag.DecItem call
            IsInsertMode = true, // We want to insert, not overwrite
            MaxPatches = 1, // Only patch the first occurrence
            MinimumSafetyOffset = 5, // Ensure we have enough context before the match
            ExtraLogging = false // Enable detailed logging for debugging
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}