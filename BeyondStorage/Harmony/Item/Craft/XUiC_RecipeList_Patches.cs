using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.ContainerLogic.Item;
using BeyondStorage.Scripts.Utils;
using HarmonyLib;

namespace BeyondStorage.Item.Craft;

[HarmonyPatch(typeof(XUiC_RecipeList))]
public class XUiCRecipeListPatches
{
    // == BEFORE ==
    // IL_0081: callvirt     instance class ItemStack[] XUiC_ItemStackGrid::GetSlots()
    // IL_0086: callvirt     instance void class [mscorlib]System.Collections.Generic.List`1<class ItemStack>::AddRange(class [mscorlib]System.Collections.Generic.IEnumerable`1<!0/*class ItemStack*/>)
    // IL_008b: ldarg.0      // this [Label4]
    // IL_008c: ldloc.0      // updateStackList List<ItemStack>
    // IL_008d: call         instance void XUiC_RecipeList::BuildRecipeInfosList(class [mscorlib]System.Collections.Generic.List`1<class ItemStack>)

    // == AFTER ==
    // IL_0084: callvirt ItemStack[] XUiC_ItemStackGrid::GetSlots()
    // IL_0089: callvirt System.Void System.Collections.Generic.List`1<ItemStack>::AddRange(System.Collections.Generic.IEnumerable`1<T>)
    // IL_008e: ldloc.0      // updateStackList List<ItemStack> [Label4]
    // IL_008f: call AddPullableStorageStacks(List<ItemStack>)
    // IL_0094: ldarg.0      // this
    // IL_0095: ldloc.0      // updateStackList List<ItemStack>
    // IL_0096: call System.Void XUiC_RecipeList::BuildRecipeInfosList(System.Collections.Generic.List`1<ItemStack>)

    // Used for:
    //      Item Crafts (shown as available in the list)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_RecipeList.Update))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_RecipeList_Update_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodName = $"{typeof(XUiC_RecipeList)}.{nameof(XUiC_RecipeList.Update)}";

        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0), // this
            new CodeInstruction(OpCodes.Ldloc_0), // updateStackList
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XUiC_RecipeList), nameof(XUiC_RecipeList.BuildRecipeInfosList)))
        };

        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_0), // updateStackList
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCraft), nameof(ItemCraft.ItemCraft_AddPullableSourceStorageStacks)))
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodName,
            ReplacementOffset = 0,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 0,
            ExtraLogging = false
        };

        var patchResult = ILPatchEngine.ApplyPatches(request);

        if (patchResult.IsPatched)
        {
            // Handle label transfer - move labels from original ldarg.0 to new ldloc.0
            var originalLdargIndex = patchResult.OriginalPositions[0]; // ldarg.0 position in original
            if (originalLdargIndex >= 0 && originalLdargIndex < request.OriginalInstructions.Count)
            {
                var originalInstruction = request.OriginalInstructions[originalLdargIndex];
                if (originalInstruction.labels.Count > 0)
                {
                    var newLdlocIndex = patchResult.Positions[0]; // First replacement instruction (ldloc.0)
                    if (request.ExtraLogging)
                    {
                        Logger.DebugLog($"{targetMethodName}: Moving {originalInstruction.labels.Count} labels from original ldarg.0 to new ldloc.0");
                    }

                    // Move labels to the new ldloc.0 instruction
                    var newInstruction = request.NewInstructions[newLdlocIndex].Clone();
                    foreach (var label in originalInstruction.labels)
                    {
                        newInstruction.labels.Add(label);
                    }
                    request.NewInstructions[newLdlocIndex] = newInstruction;
                }
                else
                {
                    // Could not find the label instruction, log an error
                    Logger.Error($"{targetMethodName} patch failed: Could not find the label instruction for the branch.");
                    return originalInstructions; // Return original instructions if patch failed
                }
            }
        }

        return patchResult.BestInstructions(request);
    }
}