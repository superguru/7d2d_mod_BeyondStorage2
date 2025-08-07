using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Vehicle;

public static class VehicleRefuel
{
    public static int VehicleRefuelRemoveRemaining(ItemValue itemValue, int lastRemovedCount, int totalRequired)
    {
        const string d_MethodName = nameof(VehicleRefuelRemoveRemaining);

        if (itemValue == null)
        {
            ModLogger.Warning($"{d_MethodName}: itemValue is null, returning lastRemovedCount {lastRemovedCount}");
            return lastRemovedCount;
        }

        // skip if already at required amount
        if (lastRemovedCount == totalRequired)
        {
            return lastRemovedCount;
        }

        // skip if we don't need to remove anything
        if (totalRequired <= 0)
        {
            return lastRemovedCount;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // skip if not enabled
        if (!context.Config.EnableForVehicleRefuel)
        {
            return lastRemovedCount;
        }

        var itemName = itemValue.ItemClass.GetItemName();
        var newRequiredCount = totalRequired - lastRemovedCount;

        var removedFromStorage = context.RemoveRemaining(itemValue, newRequiredCount);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; lastRemoved {lastRemovedCount}; totalRequired {totalRequired}; newReqAmt {newRequiredCount}; removedFromStorage {removedFromStorage}; newResult {lastRemovedCount + removedFromStorage}");
#endif
        return lastRemovedCount + removedFromStorage;  // return new refueled count
    }

    public static bool CanRefuel(EntityVehicle vehicle, bool alreadyHasItem)
    {
        const string d_MethodName = nameof(CanRefuel);

        if (vehicle == null)
        {
            ModLogger.Warning($"{d_MethodName}: vehicle is null, returning alreadyHasItem {alreadyHasItem}");
            return alreadyHasItem;
        }

        // return early if already able to refuel from inventory
        if (alreadyHasItem)
        {
            return true;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // skip if not enabled
        if (!context.Config.EnableForVehicleRefuel)
        {
            return alreadyHasItem;
        }

        // attempt to get fuelItem, return false if unable to find
        var fuelItem = vehicle.GetVehicle().GetFuelItem();
        if (fuelItem == "")
        {
            return false;
        }

        var fuelItemValue = ItemClass.GetItem(fuelItem);
        var storageHas = context.HasItem(fuelItemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: fuelItem {fuelItem}; storageHas {storageHas}");
#endif
        return storageHas;
    }
}