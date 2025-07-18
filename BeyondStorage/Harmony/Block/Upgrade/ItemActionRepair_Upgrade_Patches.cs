using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BeyondStorage.Scripts.ContainerLogic.Block;
using BeyondStorage.Scripts.Utils;
using HarmonyLib;

namespace BeyondStorage.Block.Upgrade;

[HarmonyPatch(typeof(ItemActionRepair))]
public class ItemActionRepairUpgradePatches
{
    // Used For:
    //          Block Upgrade (Check for enough items)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionRepair.CanRemoveRequiredResource))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionRepair_CanRemoveRequiredResource_Patch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var targetMethodString = $"{typeof(ItemActionRepair)}.{nameof(ItemActionRepair.CanRemoveRequiredResource)}";
        LogUtil.Info($"Transpiling {targetMethodString}");

        var codes = instructions.ToList();

        var targetOpCode = OpCodes.Callvirt.Name;
        var targetType = typeof(Bag);
        var targetMethod = nameof(Bag.GetItemCount);

        var targetIndex = -1;

        LogUtil.Info($"Looking for: {targetOpCode} for {targetType.Name} method {targetMethod}");

        for (var i = 0; i < codes.Count; i++)
        {
            //LogUtil.Info($"opcode {i} is {codes[i].opcode}");
            if (codes[i].opcode.Name.Equals(targetOpCode))
            {
                // Bag.GetItemCount is overloaded. Can be more acccurate with the method signature, but this is good enough
                MethodInfo methodInfo = (MethodInfo)codes[i].operand;
                if ((methodInfo.DeclaringType == targetType) && methodInfo.Name.Equals(targetMethod))
                {
                    //LogUtil.Info($"targetOpCode for {methodInfo.DeclaringType.Name} method {methodInfo.Name} means found");

                    targetIndex = i;
                    break;
                }
            }
        }

        if (targetIndex > -1)
        {
            LogUtil.DebugLog("Adding method to count items from all storages");

            var newLabel = generator.DefineLabel();
            // ldloc.s  _itemValue [newLabel]
            var newLabelDestCi = new CodeInstruction(codes[targetIndex - 4].opcode, codes[targetIndex - 4].operand);
            newLabelDestCi.labels.Add(newLabel);
            List<CodeInstruction> newCode = [
                // blt.s    [newLabel]
                new CodeInstruction(OpCodes.Blt_S, newLabel),
                // ldc.i4.1
                new CodeInstruction(OpCodes.Ldc_I4_1),
                // ret
                new CodeInstruction(OpCodes.Ret),
                // ldloc.s  _itemValue [newLabel]
                newLabelDestCi,
                // BlockUpgrade.BlockUpgradeGetItemCount(_itemValue)
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BlockUpgrade), nameof(BlockUpgrade.BlockUpgradeGetItemCount))),
                // Moves result onto stack
                new CodeInstruction(OpCodes.Ldloc_3)
            ];
            codes.InsertRange(targetIndex + 2, newCode);
            // == END 'Proper' Code ==

            LogUtil.Info($"Successfully patched {targetMethodString}");
        }
        else
        {
            LogUtil.Error($"Failed to patch {targetMethodString}");
        }

        return codes.AsEnumerable();
    }

    // Used For:
    //          Block Upgrade (Remove items)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionRepair.RemoveRequiredResource))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionRepair_RemoveRequiredResource_Patch(
        IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(ItemActionRepair)}.{nameof(ItemActionRepair.RemoveRequiredResource)}";
        LogUtil.Info($"Transpiling {targetMethodString}");
        var codes = new List<CodeInstruction>(instructions);
        var found = false;
        for (var i = 0; i < codes.Count; i++)
        {
            // if (data.holdingEntity.bag.DecItem(_itemValue, result) != result)
            if (codes[i].opcode != OpCodes.Callvirt ||
                (MethodInfo)codes[i].operand != AccessTools.Method(typeof(Bag), nameof(Bag.DecItem)))
            {
                continue;
            }

            LogUtil.DebugLog("Adding method to remove items from all storages");

            found = true;
            List<CodeInstruction> newCode = [
                // _itemValue
                new CodeInstruction(OpCodes.Ldloc_1),
                // result
                new CodeInstruction(OpCodes.Ldloc_2),
                // BlockUpgrade.BlockUpgradeRemoveRemaining(bag.DecItem(...), _itemValue, result)
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(BlockUpgrade), nameof(BlockUpgrade.BlockUpgradeRemoveRemaining)))
            ];
            codes.InsertRange(i + 1, newCode);

            break;
        }

        if (!found)
        {
            LogUtil.Error($"Failed to patch {targetMethodString}");
        }
        else
        {
            LogUtil.Info($"Successfully patched {targetMethodString}");
        }

        return codes.AsEnumerable();
    }
}