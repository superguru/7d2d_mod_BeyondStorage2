using BeyondStorage.Scripts.Game.Functions;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Functions;

[HarmonyPatch(typeof(XUiM_PlayerInventory))]
public static class XUiMPlayerInventory_Buy_and_Sell_Patches
{
    //    [HarmonyPrefix]
    //    [HarmonyPatch(nameof(XUiM_PlayerInventory.CanSwapItems), [typeof(ItemStack), typeof(ItemStack), typeof(int)])]
    //#if DEBUG
    //    [HarmonyDebug]
    //#endif
    private static bool XUiM_PlayerInventory_CanSwapItems_Prefix(XUiM_PlayerInventory __instance, ItemStack _removedStack, ItemStack _addedStack, int _slotNumber, ref bool __result)
    {
        __result = PurchasingCommon.CanSwapItems(__instance, _removedStack, _addedStack, _slotNumber);
        return false; // Skip original method
    }

    //    [HarmonyPostfix]
    //    [HarmonyPatch(nameof(XUiM_PlayerInventory.CountAvailableSpaceForItem), [typeof(ItemValue), typeof(bool)])]
    //#if DEBUG
    //    [HarmonyDebug]
    //#endif
    private static void XUiM_PlayerInventory_CountAvailableSpaceForItem_Postfix(XUiM_PlayerInventory __instance, ItemValue itemValue, bool limitToOneStack, ref int __result)
    {
        //const string d_MethodName = nameof(XUiM_PlayerInventory_CountAvailableSpaceForItem_Postfix);

        // Use PurchasingCommon to enhance the space calculation
        //__result = PurchasingCommon.GetEnhancedAvailableSpace(itemValue, __result, limitToOneStack, d_MethodName);
    }
}