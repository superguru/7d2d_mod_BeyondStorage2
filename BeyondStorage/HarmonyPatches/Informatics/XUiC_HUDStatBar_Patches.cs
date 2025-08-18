using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Ranged;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_HUDStatBar))]
public class XUiCHUDStatBarPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_HUDStatBar.updateActiveItemAmmo))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_HUDStatBar_updateActiveItemAmmo_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(XUiC_HUDStatBar)}.{nameof(XUiC_HUDStatBar.updateActiveItemAmmo)}";

        // Create search pattern for GetItemStacksForFilter method call
        var searchPattern = new List<CodeInstruction>
        {
            // public int Bag::GetItemCount(ItemValue _itemValue, int _seed = -1, int _meta = -1, bool _ignoreModdedItems = true)
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Bag), nameof(Bag.GetItemCount), [typeof(ItemValue),typeof(int),typeof(int),typeof(bool)]))
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0),  // this
            new CodeInstruction(OpCodes.Ldarg_0),  // this
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUiC_HUDStatBar), nameof(XUiC_HUDStatBar.currentAmmoCount))),
            new CodeInstruction(OpCodes.Ldarg_0),  // this
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUiC_HUDStatBar), nameof(XUiC_HUDStatBar.activeAmmoItemValue))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Ranged), nameof(Ranged.GetAmmoCount))),
            new CodeInstruction(OpCodes.Add),
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(XUiC_HUDStatBar), nameof(XUiC_HUDStatBar.currentAmmoCount))),
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 3,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 2,
            ExtraLogging = false
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}