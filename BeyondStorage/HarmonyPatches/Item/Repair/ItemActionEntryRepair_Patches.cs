using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(ItemActionEntryRepair))]
internal static class ItemActionEntryRepairPatches
{
    // Used For:
    //      Item Repair (Allows Repair)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryRepair.OnActivated))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionEntryRepair_OnActivated_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryRepair)}.{nameof(ItemActionEntryRepair.OnActivated)}";
        ModLogger.Info($"Transpiling {targetMethodString}");

        var codes = instructions.ToList();
        var startIndex = codes.FindIndex(instruction => instruction.opcode == OpCodes.Ldloc_1);

        if (startIndex != -1)
        {
            ModLogger.Info($"Patching {targetMethodString}");

            List<CodeInstruction> newCode = [
                // ldloc.s      itemClass                                                                     
                codes[startIndex + 1].Clone(),
                // callvirt     instance int32 XMLData.Item.ItemData::get_Id()
                codes[startIndex + 2].Clone(),
                // ldc.i4.0
                codes[startIndex + 3].Clone(),
                // newobj       instance void ItemValue::.ctor(int32, bool)
                codes[startIndex + 4].Clone(),
                // ldloc.s      7  // int b
                codes[startIndex + 6].Clone(),
                // ItemRepair.ItemRepairOnActivatedGetItemCount(new ItemValue(itemClass.Id))
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemRepair), nameof(ItemRepair.ItemRepairOnActivatedGetItemCount))),
                // ldloc.s      7  // int b
                codes[startIndex + 6].Clone(),
                // call         int32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(int32, int32)
                codes[startIndex + 7].Clone(),
                // stloc.s      10 // int num
                codes[startIndex + 8].Clone()
            ];

            // Insert below start
            codes.InsertRange(startIndex + 9, newCode);

            ModLogger.Info($"Successfully patched {targetMethodString}");
        }
        else
        {
            ModLogger.Error($"Failed to patch {targetMethodString}");
        }

        return codes.AsEnumerable();
    }

    // Used For:
    //      Item Repair (Button Enabled)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryRepair.RefreshEnabled))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionEntryRepair_RefreshEnabled_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryRepair)}.{nameof(ItemActionEntryRepair.RefreshEnabled)}";
        ModLogger.Info($"Transpiling {targetMethodString}");
        var startIndex = -1;
        var endIndex = -1;
        var codes = new List<CodeInstruction>(instructions);
        for (var i = 0; i < codes.Count; i++)
        {
            if (startIndex != -1 && codes[i].opcode == OpCodes.Ldc_I4_0 && codes[i + 1].opcode == OpCodes.Bgt)
            {
                endIndex = i;

                List<CodeInstruction> newCode = [
                    codes[startIndex - 4].Clone(),
                    // getId
                    codes[startIndex - 3].Clone(),
                    // Ldc_I4_0
                    codes[startIndex - 2].Clone(),
                    // new ItemValue(itemClass.Id, 0)
                    codes[startIndex - 1].Clone(),
                    // ItemRepair.ItemRepairRefreshGetItemCount(new ItemValue(itemClass.Id, 0))
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(ItemRepair), nameof(ItemRepair.ItemRepairRefreshGetItemCount))),
                    // ldloc.s  'int32'
                    codes[startIndex + 1].Clone(),
                    // call         int32 [UnityEngine.CoreModule]UnityEngine.Mathf::Min(int32, int32)
                    codes[startIndex + 2].Clone(),
                    // ldloc.3      // itemClass
                    codes[startIndex + 3].Clone(),
                    // ldfld        class DataItem`1<int32> ItemClass::RepairAmount
                    codes[startIndex + 4].Clone(),
                    // callvirt     instance !0/*int32*/ class DataItem`1<int32>::get_Value()
                    codes[startIndex + 5].Clone(),
                    // mul
                    codes[startIndex + 6].Clone(),
                    // ldc.i4.0
                    codes[startIndex + 7].Clone(),
                    // bgt.s        IL_013c
                    codes[startIndex + 8].Clone()
                ];
                // Insert our code below the previous jump (Bgt)
                codes.InsertRange(endIndex + 2, newCode);
                // Small smoke test that we're copying the code we expect
                if (startIndex + 8 != endIndex + 1)
                {
                    ModLogger.Error($"{targetMethodString} patch: Expected Equals False | Start+8 {startIndex + 8} == End+1 {endIndex + 1}");
                }

                break;
            }

            if (startIndex != -1 || codes[i].opcode != OpCodes.Callvirt || (MethodInfo)codes[i].operand !=
                AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [
                    typeof(ItemValue)
                ]))
            {
                continue;
            }

            startIndex = i;
        }

        if (startIndex == -1 || endIndex == -1)
        {
            ModLogger.Error($"Failed to patch {targetMethodString}");
        }
        else
        {
            ModLogger.Info($"Successfully patched {targetMethodString}");
        }

        return codes.AsEnumerable();
    }
}