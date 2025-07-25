using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BeyondStorage.Scripts.ContainerLogic.Item;
using BeyondStorage.Scripts.Utils;
using HarmonyLib;


namespace BeyondStorage.Item.Action;

[HarmonyPatch(typeof(ItemActionEntryCraft))]
public class ItemActionEntryCraftPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryCraft.HasItems))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_ItemStackGrid_HandleSlotChangedEvent_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryCraft)}.{nameof(ItemActionEntryCraft.HasItems)}";
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

            ///* Original code snippet:
            /// List<ItemStack> allItemStacks = xui.PlayerInventory.GetAllItemStacks();
            //-02/* 0x0025B88E 03                 */ IL_0002: ldarg.1
            //-01/* 0x0025B88F 7BB15C0004         */ IL_0003: ldfld class XUiM_PlayerInventory XUi::PlayerInventory
            //+00/* 0x0025B894 6F19780006         */ IL_0008: callvirt instance class [mscorlib] System.Collections.Generic.List`1<class ItemStack> XUiM_PlayerInventory::GetAllItemStacks()
            //+01/* 0x0025B899 0B                 */ IL_000d: stloc.1
            //Count is 4, max index is 3

            patchIndex = codes.FindIndex(patchIndex, code =>
                code.opcode == OpCodes.Callvirt &&
                code.operand is MethodInfo methodInfo
                && methodInfo.Name == "GetAllItemStacks");

            //patchIndex = codes.FindIndex(patchIndex, code => code.opcode == OpCodes.Ldfld && code.operand is FieldInfo fieldInfo && fieldInfo.Name == "isBurning");
            //patchIndex = codes.FindIndex(patchIndex, code => code.opcode == OpCodes.Ldarg_0);
            if (patchIndex < 0)
            {
                // No more matches found
                break;
            }

            LogUtil.DebugLog($"Found patch point at index {patchIndex} in {targetMethodString}");

            if (patchIndex < 3)  // max index is 3, so we need at least 4 instructions to insert new code
            {
                LogUtil.Warning($"Patch index {patchIndex} is too low to insert the new code. Skipping patch.");
                patchIndex++;
                continue;
            }

            // We need to call public static List<ItemStack> ItemCraft.ItemCraftGetAllAvailableItemStacksFromXui(XUi xui) INSTEAD of GetAllItemStacks
            // locals [1] class [mscorlib]System.Collections.Generic.List`1<class ItemStack>  /* List<ItemStack> allItemStacks */

            codes[patchIndex - 02] = new CodeInstruction(OpCodes.Nop);
            codes[patchIndex - 01] = new CodeInstruction(OpCodes.Ldarg_1);
            codes[patchIndex + 00] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCraft), nameof(ItemCraft.ItemCraft_GetAllAvailableItemStacksFromXui)));
            codes[patchIndex + 01] = new CodeInstruction(OpCodes.Stloc_1);

            patchCount++;

            LogUtil.DebugLog($"Applied patch #{patchCount} at index {patchIndex - 2} in {targetMethodString}");
            patchIndex++; // Move past the newly inserted code
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