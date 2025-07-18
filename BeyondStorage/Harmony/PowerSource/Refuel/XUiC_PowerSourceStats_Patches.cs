using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BeyondStorage.Scripts.ContainerLogic.PowerSource;
using BeyondStorage.Scripts.Utils;
using HarmonyLib;
using UniLinq;

namespace BeyondStorage.PowerSource.Refuel;

[HarmonyPatch(typeof(XUiC_PowerSourceStats))]
public class XUiCPowerSourceStatsPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_PowerSourceStats.BtnRefuel_OnPress))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_PowerSourceStats_BtnRefuel_OnPress_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(XUiC_PowerSourceStats)}.{nameof(XUiC_PowerSourceStats.BtnRefuel_OnPress)}";
        var codeList = instructions as List<CodeInstruction> ?? instructions?.ToList() ?? new List<CodeInstruction>();
        bool patchApplied = false;

        for (int i = 0; i < codeList.Count; i++)
        {
            // Look for: callvirt Bag.DecItem
            if (codeList[i].opcode == OpCodes.Callvirt &&
                codeList[i].operand is MethodInfo mi &&
                mi == AccessTools.Method(typeof(Bag), nameof(Bag.DecItem)))
            {
                LogUtil.DebugLog($"Patching {targetMethodString} at instruction {i}");

                // Ensure we have enough instructions before and after for safe patching
                if (i - 5 >= 0 && i + 2 < codeList.Count)
                {
                    var injectedInstructions = new List<CodeInstruction>
                    {
                        // ldloc.s      _itemValue
                        codeList[i - 5].Clone(),
                        // ldloc.s      _count2 (last removed count)
                        codeList[i + 2].Clone(),
                        // ldloc.2      // _count1
                        new CodeInstruction(OpCodes.Ldloc_2),
                        // conv.i4      (int) _count1
                        new CodeInstruction(OpCodes.Conv_I4),
                        // PowerSourceRefuel.RefuelRemoveRemaining(ItemValue itemValue, int lastRemoved, int totalNeeded)
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerSourceRefuel), nameof(PowerSourceRefuel.RefuelRemoveRemaining))),
                        // stloc.s      _count2     |   update result
                        codeList[i + 1].Clone()
                    };

                    codeList.InsertRange(i + 2, injectedInstructions);
                    patchApplied = true;
                }
                else
                {
                    LogUtil.Error($"Patch for {targetMethodString} failed: insufficient instruction context at index {i}.");
                }
                break;
            }
        }

        if (!patchApplied)
        {
            LogUtil.Error($"Failed to patch {targetMethodString}: target instruction not found.");
        }
        else if (patchApplied)
        {
            LogUtil.DebugLog($"Successfully patched {targetMethodString}");
        }

        return codeList.AsEnumerable();
    }
}