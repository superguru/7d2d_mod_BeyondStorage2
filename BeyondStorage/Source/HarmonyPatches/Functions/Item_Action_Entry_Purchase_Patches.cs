using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Functions;

[HarmonyPatch(typeof(ItemActionEntryPurchase))]
internal static class Item_Action_Entry_Purchase_Patches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryPurchase.RefreshEnabled))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionEntryPurchase_RefreshEnabled_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryPurchase)}.{nameof(ItemActionEntryPurchase.RefreshEnabled)}";

        // Create search pattern for GetItemStacksForFilter method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_2),      // playerInventory
            new CodeInstruction(OpCodes.Ldloc_S, 6),   // _itemValue
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [typeof(ItemValue)])),
            new CodeInstruction(OpCodes.Ldloc_S, 5),   // buyPrice (get)
            new CodeInstruction(OpCodes.Clt),          // Compare item count with buy price
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_S, 6),   // _itemValue
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCommon), nameof(ItemCommon.ItemCommon_GetStorageItemCount))),
            new CodeInstruction(OpCodes.Add),
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 3,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 2,
            ExtraLogging = false,
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryPurchase.OnActivated))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionEntryPurchase_OnActivated_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryPurchase)}.{nameof(ItemActionEntryPurchase.OnActivated)}";

        // Create search pattern for GetItemStacksForFilter method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_S, 6),    // playerInventory
            new CodeInstruction(OpCodes.Ldloc_S, 14),   // itemStack3, which is now set to TraderInfo.CurrencyItem
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.RemoveItem), [typeof(ItemStack)])),
        };

        // Create replacement instructions to call playerInventory.xui.CollectedItemList.RemoveItemStack(itemStack3)
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_S, 6),    // playerInventory
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.xui))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUi), nameof(XUi.CollectedItemList))),
            new CodeInstruction(OpCodes.Ldloc_S, 14),   // Load itemStack3 (currency stack)
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiC_CollectedItemList), nameof(XUiC_CollectedItemList.RemoveItemStack), [typeof(ItemStack)])),
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 3, // Insert after the RemoveItem call
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 2,
            ExtraLogging = false,
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}