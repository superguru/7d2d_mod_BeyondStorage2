using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(XUiC_IngredientEntry))]
internal static class XUiCIngredientEntryPatches
{
    // Used for:
    //      Item Crafting (shows item count available in crafting window(s))
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_IngredientEntry.GetBindingValue))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_IngredientEntry_GetBindingValue_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(XUiC_IngredientEntry)}.{nameof(XUiC_IngredientEntry.GetBindingValue)}";
        ModLogger.Info($"Transpiling {targetMethodString}");

        var codes = new List<CodeInstruction>(instructions);
        var found = false;
        var patchCount = 0;

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode != OpCodes.Callvirt || (MethodInfo)codes[i].operand != AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [typeof(ItemValue)]))
            {
                continue;
            }

            ModLogger.DebugLog($"XUiC_IngredientEntry_GetBindingValue_Patch: Adding method to add item counts from all storages (patch #{++patchCount})");

            found = true;
            List<CodeInstruction> newCode = [
                // Ldarg_0      this
                new CodeInstruction(OpCodes.Ldarg_0),
                // ItemCommon.EntryBindingAddAllStorageCount(this.xui.PlayerInventory.GetItemCount(this.ingredient.itemValue)), this)
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCraft), nameof(ItemCraft.EntryBinding_AddPullableSourceStorageItemCount)))
            ];
            codes.InsertRange(i + 1, newCode);
        }

        if (!found)
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