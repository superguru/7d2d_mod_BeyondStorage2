using Audio;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Vehicle;

public static class VehicleRepair
{
    public static int VehicleRepairRemoveRemaining(XUi xui, global::Vehicle vehicle, ItemValue itemValue)
    {
        const string d_MethodName = nameof(VehicleRepairRemoveRemaining);

        if (itemValue == null)
        {
            ModLogger.Warning($"{d_MethodName}: itemValue is null, returning 0");
            return 0;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // skip if not enabled
        if (!context.Config.EnableForVehicleRepair)
        {
            return 0;
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
        ModLogger.DebugLog($"{d_MethodName}: Removed {countRemoved} {itemValue.ItemClass.GetItemName()}");
#endif
        // if we didn't remove anything return back failed (0)
        if (countRemoved <= 0)
        {
            return countRemoved;
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