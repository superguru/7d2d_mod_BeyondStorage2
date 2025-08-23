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
#if DEBUG
        //const string d_MethodName = nameof(XUiM_PlayerInventory_RemoveItems_Prefix);
#endif
        // Cache frequently accessed properties
        var backpack = __instance.Backpack;
        var toolbelt = __instance.Toolbelt;

        // Use foreach - it's faster for IList<T> and avoids repeated bounds checking
        foreach (var itemStack in _itemStacks)
        {
            // Cache the current item stack reference and its properties
            var itemValue = itemStack.itemValue;
            int stillNeeded = itemStack.count * _multiplier;
#if DEBUG
            //var itemName = ItemX.NameOf(itemValue);
            //ModLogger.DebugLog($"{d_MethodName}: Removing {stillNeeded} of {itemName}");
#endif
            // First DecItem call: Remove from backpack
            var removed = backpack.DecItem(itemValue, stillNeeded, true, _removedItems);
            stillNeeded -= removed;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Removed {removed} of {itemName} from Backpack, still need {stillNeeded}");
#endif
            // If still need more, try toolbelt
            if (stillNeeded > 0)
            {
                removed = toolbelt.DecItem(itemValue, stillNeeded, true, _removedItems);
                stillNeeded -= removed;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Removed {removed} of {itemName} from Toolbelt, still need {stillNeeded}");
#endif
                // If still need more, try storage
                if (stillNeeded > 0)
                {
                    removed = ItemCommon.ItemRemoveRemaining(itemValue, stillNeeded, true, _removedItems);
                    stillNeeded -= removed;
#if DEBUG
                    //ModLogger.DebugLog($"{d_MethodName}: Removed {removed} of {itemName} from Storage, still need {stillNeeded}");
#endif
                }
            }
        }

        // Use cached method references (fastest)
        s_onBackpackChanged?.Invoke(__instance, null);
        s_onToolbeltChanged?.Invoke(__instance, null);

        return false; // Skip the original method completely
    }
}