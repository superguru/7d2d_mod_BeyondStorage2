using System.Collections.Generic;
using BeyondStorage.Scripts.Game.Item;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(XUiM_PlayerInventory))]
internal static class XUiMPlayerInventoryCommonPatches
{
    // Cache the reflection calls at class level (best performance)
    private static readonly System.Reflection.MethodInfo s_onBackpackChanged =
        AccessTools.Method(typeof(XUiM_PlayerInventory), "onBackpackItemsChanged");
    private static readonly System.Reflection.MethodInfo s_onToolbeltChanged =
        AccessTools.Method(typeof(XUiM_PlayerInventory), "onToolbeltItemsChanged");

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.RemoveItems))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool XUiM_PlayerInventory_RemoveItems_Prefix(XUiM_PlayerInventory __instance, IList<ItemStack> _itemStacks, int _multiplier, IList<ItemStack> _removedItems)
    {
        // Use common sequential removal method: Bag → Toolbelt → Storage
        ItemCommon.RemoveItemsSequential(__instance.Backpack, __instance.Toolbelt, _itemStacks, _multiplier, true, _removedItems);

        // Use cached method references (fastest)
        s_onBackpackChanged?.Invoke(__instance, null);
        s_onToolbeltChanged?.Invoke(__instance, null);

        return false; // Skip the original method completely
    }
}