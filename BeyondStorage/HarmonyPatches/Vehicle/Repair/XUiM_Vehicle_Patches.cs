using Audio;
using BeyondStorage.Scripts.Game.Vehicle;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Vehicle;

[HarmonyPatch(typeof(XUiM_Vehicle))]
internal static class XUiMVehiclePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiM_Vehicle.RepairVehicle))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool XUiM_Vehicle_RepairVehicle_Prefix(XUi _xui, global::Vehicle vehicle, ref bool __result)
    {
        // Resolve vehicle if not provided (original logic)
        if (vehicle == null)
        {
            vehicle = _xui.vehicle?.GetVehicle();
        }

        if (vehicle == null)
        {
            __result = false;
            return false; // Skip original method
        }

        // Get repair kit itemValue (original logic)
        ItemValue itemValue = ItemClass.GetItem("resourceRepairKit");
        if (itemValue.ItemClass == null)
        {
            __result = false;
            return false; // Skip original method
        }

        EntityPlayerLocal entityPlayer = _xui.playerUI.entityPlayer;
        LocalPlayerUI playerUI = _xui.playerUI;

        // Check repair needed (original logic)
        int repairAmountNeeded = vehicle.GetRepairAmountNeeded();
        if (repairAmountNeeded <= 0)
        {
            __result = false;
            return false; // Skip original method - no repair needed
        }

        // Calculate perk bonus (original logic)
        float perkBonus = 0f;
        ProgressionValue progressionValue = entityPlayer.Progression.GetProgressionValue("perkGreaseMonkey");
        if (progressionValue != null)
        {
            perkBonus += (float)progressionValue.Level * 0.1f;
        }

        bool itemConsumed = false;

        // Priority order: Bag → Toolbelt → Storage
        // Try to remove repair kit from bag first
        int removedFromBag = entityPlayer.bag.DecItem(itemValue, 1);
        if (removedFromBag > 0)
        {
            itemConsumed = true;
        }
        else
        {
            // Try to remove from toolbelt if bag didn't have any
            int removedFromToolbelt = entityPlayer.inventory.DecItem(itemValue, 1);
            if (removedFromToolbelt > 0)
            {
                itemConsumed = true;
            }
            else
            {
                // Try storage if neither bag nor toolbelt had repair kits
                int removedFromStorage = VehicleRepair.VehicleRepairRemoveRemaining(itemValue, 1);
                if (removedFromStorage > 0)
                {
                    itemConsumed = true;
                }
            }
        }

        // If we consumed a repair kit from any source, perform the repair
        if (itemConsumed)
        {
            // Perform repair (original logic)
            vehicle.RepairParts(1000, perkBonus);

            // Update UI and play success sound (original logic)
            playerUI.xui.CollectedItemList.RemoveItemStack(new ItemStack(itemValue, 1));
            Manager.PlayInsidePlayerHead("craft_complete_item");

            __result = true;
        }
        else
        {
            // No repair kits available anywhere, play failure sound (original logic)
            Manager.PlayInsidePlayerHead("misc/missingitemtorepair");
            __result = false;
        }

        return false; // Skip original method
    }
}