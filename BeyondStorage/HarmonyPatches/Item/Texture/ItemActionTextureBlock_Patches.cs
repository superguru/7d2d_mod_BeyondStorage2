using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(ItemActionTextureBlock))]
internal static class ItemActionTextureBlockPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionTextureBlock.checkAmmo))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionTextureBlock_checkAmmo_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        //TODO use a prefix instead of transpiler if possible
        var targetMethodName = $"{typeof(ItemActionTextureBlock)}.{nameof(ItemActionTextureBlock.checkAmmo)}";

        // Create search pattern for GetItemStacksForFilter method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Cgt),
            new CodeInstruction(OpCodes.Ret)
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            // the return value of holdingEntity.inventory.GetItemCount(itemValue, false, 0, 0, false) is on the stack already
            new CodeInstruction(OpCodes.Ldarg_1),  // _actionData (ItemActionData)
            new CodeInstruction(OpCodes.Ldarg_0),  // this (ItemActionTextureBlock)
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemActionTextureBlock), nameof(ItemActionTextureBlock.currentMagazineItem))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemTexture), nameof(ItemTexture.ItemTexture_checkAmmo))),
            // ItemTexture_checkAmmo already returns a bool (0 or 1), so we can return it directly
            new CodeInstruction(OpCodes.Ret)
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

        var response = ILPatchEngine.ApplyPatches(request);

        if (response.IsPatched)
        {
            // Get rid of the original instructions by converting them to NOP
            ModLogger.DebugLog($"{targetMethodName}: Successfully patched, changing the searchPattern instructions to NOP");

            var patched = response.BestInstructions(request);

            // Find the original searchPattern instructions and convert them to NOP
            for (int i = 0; i < response.OriginalPositions.Count; i++)
            {
                int originalPosition = response.OriginalPositions[i] + request.ReplacementInstructions.Count;

                // Convert each instruction in the searchPattern to NOP
                for (int j = 0; j < searchPattern.Count; j++)
                {
                    int targetIndex = originalPosition + j;
                    if (targetIndex < patched.Count)
                    {
                        var originalInstruction = patched[targetIndex];
                        patched[targetIndex] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = originalInstruction.labels, // Preserve labels
                            blocks = originalInstruction.blocks   // Preserve exception handling blocks
                        };

                        ModLogger.DebugLog($"{targetMethodName}: Converted instruction at index {targetIndex} ({originalInstruction.opcode}) to NOP");
                    }
                }
            }
        }

        return response.BestInstructions(request);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionTextureBlock.decreaseAmmo))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionTextureBlock_decreaseAmmo_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodName = $"{typeof(ItemActionTextureBlock)}.{nameof(ItemActionTextureBlock.decreaseAmmo)} ";

        var currentCode = Patch_decreaseAmmoPatch_GetAmmoCount(targetMethodName + "1/2", [.. originalInstructions]);
        currentCode = Patch_decreaseAmmoPatch_RemoveAmmo(targetMethodName + "2/2", currentCode);

        return currentCode;
    }

    private static List<CodeInstruction> Patch_decreaseAmmoPatch_GetAmmoCount(string targetMethodName, List<CodeInstruction> originalCode)
    {
        /*
            .locals init (
		        [0] class ItemActionTextureBlock/ItemActionTextureBlockData,    // textureBlockData
                [1] int32,                                                      // paintCost and then becomes stillNeeded
    		    [2] class EntityAlive,                                          // holdingEntity
	    	    [3] class ItemValue,                                            // currentMagazineItem aka ammoType
		        [4] int32                                                       // entityAvailableCount aka itemCount2 (used to store the result of GetItemCount, which is the ammo count we need to remove)
	        )
        */
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldc_I4_1),      // // true (_ignoreModdedItems)
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Inventory), nameof(Inventory.GetItemCount), [typeof(ItemValue), typeof(bool), typeof(int), typeof(int), typeof(bool)])),
            new CodeInstruction(OpCodes.Stloc_S, 4),    // stloc.s 4, which is entityAvailableCount
            new CodeInstruction(OpCodes.Ldloc_S, 4),    // load entityAvailableCount
            new CodeInstruction(OpCodes.Add),           // AddStackRangeForFilter bag count + inventory count
            // New code goes here, which means ReplacementOffset = 5
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Stloc_S, 4),    // update entityAvailableCount with the result of (bag count + inventory count)
            new CodeInstruction(OpCodes.Ldloc_3),       // itemValue
            new CodeInstruction(OpCodes.Ldloc_S, 4),    // load entityAvailableCount

            // public static int ItemTexture_GetAmmoCount(ItemValue ammoType, int entityInventoryCount)
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemTexture), nameof(ItemTexture.ItemTexture_GetAmmoCount))),
            new CodeInstruction(OpCodes.Stloc_S, 4),    // update entityAvailableCount with the return value of ItemTexture_GetAmmoCount (which is totalAvailableCount)
            new CodeInstruction(OpCodes.Ldloc_S, 4),    // load entityAvailableCount (which is now totalAvailableCount)
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = originalCode,
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodName,
            ReplacementOffset = 5,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 0,
            ExtraLogging = false
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }

    private static List<CodeInstruction> Patch_decreaseAmmoPatch_RemoveAmmo(string targetMethodName, List<CodeInstruction> originalCode)
    {
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Inventory), nameof(Inventory.DecItem), [typeof(ItemValue), typeof(int), typeof(bool), typeof(IList<ItemStack>)])),
            // ReplacementOffset = 3 means this is where the new code will be inserted
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret),
        };

        // paintCost means stillNeeded, which is the amount of ammo we still need to remove
        var replacementInstructions = new List<CodeInstruction>
        {
            // The stack now has the value of DecItem return value (countRemoved)
            new CodeInstruction(OpCodes.Stloc_S, 4),    // set entityAvailableCount with Inventory.DecItem result, which is the count of items removed from the inventory

            // Set up the arguments for ItemTexture_RemoveAmmo
            new CodeInstruction(OpCodes.Ldloc_3),     // itemValue
            new CodeInstruction(OpCodes.Ldloc_1),     // paintCost aka stillNeeded
            new CodeInstruction(OpCodes.Ldc_I4_0),    // _ignoreModdedItems
            new CodeInstruction(OpCodes.Ldnull),      // _removedItems

            //public static int ItemTexture_RemoveAmmo(ItemValue ammoType, int stillNeeded, bool _ignoreModdedItems = false, IList<ItemStack> _removedItems = null)
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemTexture), nameof(ItemTexture.ItemTexture_RemoveAmmo))),
            new CodeInstruction(OpCodes.Stloc_1),     // update paintCost (with ItemTexture_RemoveAmmo return value)

            // At this point we've physically removed some texture ammo from the world, so we have to return true no
            // matter if the initial ammo requirement was completely met or not, as per the game implementation.
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret),         // return true if paintCost > 0 

            // this ensures the new code definitely returns a value
            new CodeInstruction(OpCodes.Ldc_I4_0),      // push 0 (false) onto the stack
            new CodeInstruction(OpCodes.Ret),
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = originalCode,
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodName,
            ReplacementOffset = 3,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 0,
            ExtraLogging = false
        };

        var response = ILPatchEngine.ApplyPatches(request);
        var result = response.BestInstructions(request);

        var sizeDiff = result.Count - originalCode.Count;
        if (request.ExtraLogging)
        {
            ModLogger.DebugLog($"{targetMethodName}: patched code vs original size difference is {sizeDiff}");
        }

        if (sizeDiff >= 5)
        {
            var popIndex = result.FindIndex(sizeDiff, instr => instr.opcode == OpCodes.Pop);
            if (popIndex >= 0)
            {
                result[popIndex].opcode = OpCodes.Nop; // Convert the POP to a NOP
                if (request.ExtraLogging)
                {
                    ModLogger.DebugLog($"{targetMethodName}: Converted POP to NOP at index {popIndex}");
                }
            }
            else
            {
                ModLogger.Error($"{targetMethodName}: Could not find POP to convert to NOP, this is unexpected. popIndex is {popIndex}");
            }
        }

        return result;
    }
}