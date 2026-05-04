using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BeyondStorage.Game.Item;
using HarmonyLib;

namespace BeyondStorage.Harmony.Patches;

[HarmonyPatch(typeof(XUiC_IngredientEntry))]
internal static class XUiC_IngredientEntry_Patch
{
    // Used for:
    //      Item Crafting (shows item count available in crafting window(s))
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_IngredientEntry.GetBindingValueInternal))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_IngredientEntry_GetBindingValue_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(XUiC_IngredientEntry)}.{nameof(XUiC_IngredientEntry.GetBindingValueInternal)}";

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. instructions],
            TargetMethodName = targetMethodString,
            SearchPattern =
            [
                new(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [typeof(ItemValue)]))
            ],
            ReplacementInstructions =
            [
                // Ldarg_0      this
                new(OpCodes.Ldarg_0),
                // ItemCommon.EntryBindingAddAllStorageCount(this.xui.PlayerInventory.GetItemCount(this.ingredient.itemValue)), this)
                new(OpCodes.Call, AccessTools.Method(typeof(ItemCraft), nameof(ItemCraft.EntryBinding_AddPullableSourceStorageItemCount)))
            ],
            ReplacementOffset = 1,  // Insert after the GetItemCount call
            IsInsertMode = true,    // Insert new instructions rather than replacing
            MaxPatches = 0,         // Unlimited patches (replace all occurrences)
            MinimumSafetyOffset = 0,
            ExtraLogging = false
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request).AsEnumerable();
    }
}