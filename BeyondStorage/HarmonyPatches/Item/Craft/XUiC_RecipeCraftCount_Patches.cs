using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(XUiC_RecipeCraftCount))]
public class XUiCRecipeCraftCountPatches
{
    // Used for:
    //          Item Crafting (gets max craftable amount)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_RecipeCraftCount.calcMaxCraftable))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_RecipeCraftCount_calcMaxCraftable_Patch(
        IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(XUiC_RecipeCraftCount)}.{nameof(XUiC_RecipeCraftCount.calcMaxCraftable)}";
        ModLogger.Info($"Transpiling {targetMethodString}");

        // Append our itemStack array to current inventory
        var codes = new List<CodeInstruction>(instructions);
        var set = false;
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode != OpCodes.Callvirt || (MethodInfo)codes[i].operand !=
                AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetAllItemStacks)))
            {
                continue;
            }

            ModLogger.DebugLog("Appending our item stacks to current inventory");

            // ItemCraft.MaxCraftGetAllStorageStacks(this.xui.PlayerInventory.GetItemStacksForFilter()).ToArray()
            codes.Insert(i + 1,
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(ItemCraft), nameof(ItemCraft.ItemCraft_MaxGetAllStorageStacks))));
            set = true;
            break;
        }

        if (!set)
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