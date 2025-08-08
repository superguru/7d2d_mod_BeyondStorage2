using System.Linq;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(XUiC_ItemActionList))]
public class XUiCItemActionListPatches
{
    // Used For:
    //      Item Repair (tracks item action list visibility)
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemActionList.Init))]
    private static void XUiC_ItemActionList_Init_Postfix(XUiC_ItemActionList __instance)
    {
        __instance.OnVisiblity += ActionList_VisibilityChanged;
    }

    // Capture when the visibility of the Action List is changed
    private static void ActionList_VisibilityChanged(XUiController _sender, bool _visible)
    {
        ItemRepair.ActionListVisible = _visible;
    }

    // Used For:
    //      Item Repair (captures if item actions list contains repairing)
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemActionList.SetCraftingActionList))]
    private static void XUiC_ItemActionList_SetCraftingActionList_Postfix(XUiC_ItemActionList __instance)
    {
        ActionList_UpdateVisibleActions(__instance);
    }

    // Used For:
    //      Item Repair (captures if item actions list contains repairing)
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemActionList.SetServiceActionList))]
    private static void XUiC_ItemActionList_SetServiceActionList_Postfix(XUiC_ItemActionList __instance)
    {
        ActionList_UpdateVisibleActions(__instance);
    }

    private static void ActionList_UpdateVisibleActions(XUiC_ItemActionList itemActionList)
    {
        const string d_MethodName = nameof(ActionList_UpdateVisibleActions);

        var context = StorageContextFactory.Create(d_MethodName);
        if (context != null)
        {
            if (!context.Config.EnableForItemRepair)
            {
                return;
            }

            ItemRepair.RepairActionShown = ActionList_HasRepair(itemActionList);
        }
        else
        {
            ModLogger.Error($"{d_MethodName}: Failed to create StorageContext");
        }
    }

    // Check if the list of actions contains Repair
    private static bool ActionList_HasRepair(XUiC_ItemActionList itemActionList)
    {
        return itemActionList.itemActionEntries.OfType<ItemActionEntryRepair>().Any();
    }
}