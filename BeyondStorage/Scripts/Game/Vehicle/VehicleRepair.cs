using Audio;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Vehicle;

public static class VehicleRepair
{
    public static int VehicleRepairRemoveRemaining(XUi xui, global::Vehicle vehicle, ItemValue itemValue)
    {
        const string d_MethodName = nameof(VehicleRepairRemoveRemaining);
        const int DEFAULT_RETURN_VALUE = 0;

        if (vehicle == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: vehicle is null, returning {DEFAULT_RETURN_VALUE}");
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, config => config.EnableForVehicleRepair,
            out StorageContext context, out _, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        // skip if no repairs needed
        var repairAmountNeeded = vehicle.GetRepairAmountNeeded();
        if (repairAmountNeeded <= 0)
        {
            return 0;
        }

        // attempt to remove item from storage
        var countRemoved = context.RemoveRemaining(itemValue, 1);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Removed {countRemoved} {itemName}");
#endif
        // if we didn't remove anything return back failed (0)
        if (countRemoved <= 0)
        {
            return DEFAULT_RETURN_VALUE;
        }

        var entityPlayer = xui.playerUI.entityPlayer;
        var playerUi = xui.playerUI;

        // repair percent
        var percent = 0.0f;

        // change percentage based on GreaseMonkey Perk
        var progressionValue = entityPlayer.Progression.GetProgressionValue("perkGreaseMonkey");
        if (progressionValue != null)
        {
            percent += progressionValue.Level * 0.1f;
        }

        // Repair vehicle
        vehicle.RepairParts(1000, percent);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Repaired {vehicle}");
#endif
        // show stack removed on UI
        playerUi.xui.CollectedItemList.RemoveItemStack(new ItemStack(itemValue, 1));

        // play sound of completed craft
        Manager.PlayInsidePlayerHead("craft_complete_item");

        // return amount removed
        return countRemoved;
    }
}