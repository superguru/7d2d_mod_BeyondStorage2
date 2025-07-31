using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Vehicle;

public static class VehicleRefuel
{
    public static int VehicleRefuelRemoveRemaining(ItemValue itemValue, int lastRemovedCount, int totalRequired)
    {
        const string d_MethodName = nameof(VehicleRefuelRemoveRemaining);

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

        // skip if not enabled
        if (!ModConfig.EnableForVehicleRefuel())
        {
            return lastRemovedCount;
        }

        var itemName = itemValue.ItemClass.GetItemName();
        var newRequiredCount = totalRequired - lastRemovedCount;

        var context = StorageAccessContext.Create(d_MethodName);
        var removedFromStorage = context?.RemoveRemaining(itemValue, newRequiredCount) ?? 0;

        LogUtil.DebugLog($"{d_MethodName} - item {itemName}; lastRemoved {lastRemovedCount}; totalRequired {totalRequired}; newReqAmt {newRequiredCount}; removedFromStorage {removedFromStorage}; newResult {lastRemovedCount + removedFromStorage}");
        return lastRemovedCount + removedFromStorage;  // return new refueled count
    }

    public static bool CanRefuel(EntityVehicle vehicle, bool alreadyHasItem)
    {
        const string d_MethodName = nameof(CanRefuel);

        // return early if already able to refuel from inventory
        if (alreadyHasItem)
        {
            return true;
        }

        // attempt to get fuelItem, return false if unable to find
        var fuelItem = vehicle.GetVehicle().GetFuelItem();
        if (fuelItem == "")
        {
            return false;
        }

        var fuelItemValue = ItemClass.GetItem(fuelItem);
        var context = StorageAccessContext.Create(d_MethodName);
        var storageHas = context?.HasItem(fuelItemValue) ?? false;
        LogUtil.DebugLog($"{d_MethodName} - fuelItem {fuelItem}; storageHas {storageHas}");

        return storageHas;
    }
}