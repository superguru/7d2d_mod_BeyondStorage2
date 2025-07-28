using System.Collections.Generic;
using System.Linq;
using System.Reflection;
//using System.Reflection;
using System.Reflection.Emit;
using BeyondStorage.Scripts.ContainerLogic.Recipe;
//using BeyondStorage.Scripts.ContainerLogic.Recipe;
using BeyondStorage.Scripts.Utils;
using HarmonyLib;


namespace BeyondStorage.Recipe;

[HarmonyPatch(typeof(XUiC_WorkstationOutputGrid))]
public class WorkstationPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_WorkstationOutputGrid.UpdateData))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_WorkstationOutputGrid_UpdateData_Patch(IEnumerable<CodeInstruction> instructions)
    {
        // This is called when the recipe finishes crafting on a currently opened workstation window
        var targetMethodString = $"{typeof(XUiC_WorkstationOutputGrid)}.{nameof(XUiC_WorkstationOutputGrid.UpdateData)}";
        LogUtil.Info($"Transpiling {targetMethodString}");

        var codes = new List<CodeInstruction>(instructions);

        int patchIndex = 0;
        int patchCount = 0;
        int MAX_PATCHES = 1;

        while ((patchIndex >= 0) && (patchIndex < codes.Count - 1))
        {
            if ((MAX_PATCHES > 0) && (patchCount >= MAX_PATCHES))
            {
                LogUtil.Info($"Reached maximum patches ({MAX_PATCHES}) for {targetMethodString}. Stopping further patches.");
                break;
            }

            patchIndex = codes.FindIndex(patchIndex, code => code.opcode == OpCodes.Callvirt && code.operand is MethodInfo methodInfo && methodInfo.Name == "UpdateBackend");
            //patchIndex = codes.FindIndex(patchIndex, code => code.opcode == OpCodes.Ldfld && code.operand is FieldInfo fieldInfo && fieldInfo.Name == "isBurning");
            //patchIndex = codes.FindIndex(patchIndex, code => code.opcode == OpCodes.Ldarg_0);
            if (patchIndex < 0)
            {
                // No more matches found
                break;
            }
            LogUtil.DebugLog($"Found patch point {patchCount + 1} at index {patchIndex} in {targetMethodString}");

            if (patchIndex < 0)
            {
                LogUtil.Warning($"Patch index {patchIndex} is too low to insert the new code. Skipping patch.");
                patchIndex++;
                continue;
            }

            List<CodeInstruction> newCode = [
                //new CodeInstruction(OpCodes.Ldarg_0), // this
                //new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUiC_RecipeStack), nameof(XUiC_RecipeStack.windowGroup))), // ldfld XUiWindowGroup XUiController::windowGroup
                //new CodeInstruction(OpCodes.Ldarg_1),  // stackList
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WorkstationRecipe), nameof(WorkstationRecipe.ForegroundWorkstation_CraftCompleted))),
            ];

            codes.InsertRange(patchIndex + 1, newCode);
            patchCount++;

            LogUtil.DebugLog($"Inserted patch #{patchCount} at index {patchIndex - 2} in {targetMethodString}");
            patchIndex += newCode.Count + 1; // Move past the newly inserted code
        }

        if (patchCount > 0)
        {
            LogUtil.Info($"Successfully patched {targetMethodString} in {patchCount} places");
        }
        else
        {
            LogUtil.Warning($"No patches applied to {targetMethodString}");
        }

        return codes.AsEnumerable();
    }
}