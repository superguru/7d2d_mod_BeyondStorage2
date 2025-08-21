using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Harmony;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Functions;

[HarmonyPatch(typeof(ItemActionEntrySell))]
public static class Item_Action_Entry_Sell_Patches
{
    private static bool s_isMidSale = false;
    private static object s_lockObject = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionEntrySell.OnActivated))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void ItemActionEntrySell_OnActivated_Prefix(ItemActionEntrySell __instance)
    {
#if DEBUG
        const string d_MethodName = nameof(ItemActionEntrySell_OnActivated_Prefix);
#endif
        // Thread-safe update of call counter and history
        lock (s_lockObject)
        {
            s_isMidSale = true;
        }
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Entering mid-sale state");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemActionEntrySell.OnActivated))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void ItemActionEntrySell_OnActivated_Postfix(ItemActionEntrySell __instance)
    {
#if DEBUG
        const string d_MethodName = nameof(ItemActionEntrySell_OnActivated_Postfix);
#endif
        // Thread-safe update of call counter and history
        lock (s_lockObject)
        {
            s_isMidSale = false;
        }
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Exiting mid-sale state");
#endif
    }

    public static bool IsPlayerMidSale()
    {
        // Thread-safe check for mid-sale state
        lock (s_lockObject)
        {
            return s_isMidSale;
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntrySell.OnActivated))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionEntrySell_OnActivated_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntrySell)}.{nameof(ItemActionEntrySell.OnActivated)}";

        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_S, 17),   // itemStack, which is set to the item being sold by the Player
            new CodeInstruction(OpCodes.Ldc_I4_0),      // push 0==false for TraderData::AddToPrimaryInventory param addMarkup
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TraderData), nameof(TraderData.AddToPrimaryInventory), [typeof(ItemStack), typeof(bool)])),
        };

        // Create replacement instructions to call playerInventory.xui.CollectedItemList.RemoveItemStack(itemStack3)
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_S, 17),   // Load itemStack, which is set to the item being sold by the Player
            new CodeInstruction(OpCodes.Ldloc_S, 13),   // count
            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ItemStack), nameof(ItemStack.count))),

            new CodeInstruction(OpCodes.Ldloc_S, 19),    // playerInventory
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.xui))),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(XUi), nameof(XUi.CollectedItemList))),
            new CodeInstruction(OpCodes.Ldloc_S, 17),   // Load itemStack, which is set to the item being sold by the Player
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