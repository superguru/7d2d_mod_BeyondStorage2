using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Vehicle;

public static class VehicleRefuel
{
    public static int VehicleRefuelRemoveRemaining(ItemValue itemValue, int lastRemoved, int totalNeeded)
    {
        const string d_MethodName = nameof(VehicleRefuelRemoveRemaining);
        int DEFAULT_RETURN_VALUE = lastRemoved;

        if (totalNeeded <= 0 || lastRemoved >= totalNeeded)
        {
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, config => config.EnableForVehicleRefuel,
            out StorageContext context, out _, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var newRequiredCount = totalNeeded - lastRemoved;

        var removedFromStorage = context.RemoveRemaining(itemValue, newRequiredCount);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; lastRemoved {lastRemoved}; totalNeeded {totalNeeded}; newReqAmt {newRequiredCount}; removedFromStorage {removedFromStorage}; newResult {lastRemoved + removedFromStorage}");
#endif
        return lastRemoved + removedFromStorage;  // return new refueled count
    }

    public static bool CanRefuel(EntityVehicle vehicle, bool alreadyHasItem)
    {
        const string d_MethodName = nameof(CanRefuel);
        const bool DEFAULT_RETURN_VALUE = false;

        if (vehicle == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: vehicle is null, returning alreadyHasItem {DEFAULT_RETURN_VALUE}");
            return DEFAULT_RETURN_VALUE;
        }

        // return early if already able to refuel from inventory
        if (alreadyHasItem)
        {
            return alreadyHasItem;
        }

        if (!ValidationHelper.ValidateStorageContextWithFeature(d_MethodName, config => config.EnableForVehicleRefuel, out StorageContext context))
        {
            return alreadyHasItem;
        }

        // attempt to get fuelItem, return false if unable to find
        var fuelItem = vehicle.GetVehicle()?.GetFuelItem() ?? "";
        if (string.IsNullOrEmpty(fuelItem))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var fuelItemValue = ItemClass.GetItem(fuelItem);
        if (!ValidationHelper.ValidateItemValue(fuelItemValue, d_MethodName, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var storageHas = context.HasItem(fuelItemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: fuelItem {itemName}; storageHas {storageHas}");
#endif
        return storageHas;
    }
}