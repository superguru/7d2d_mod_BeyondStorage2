using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class VehicleUtils
{
    public const int DEFAULT_VEHICLE_LIST_CAPACITY = 8;

    public static void GetAvailableVehicleStorages(BatchRemovalContext context)
    {
        const string d_method_name = nameof(GetAvailableVehicleStorages);

        if (context == null)
        {
            LogUtil.Error($"{d_method_name}: context is null, aborting.");
            return;
        }

        if (context.WorldPlayerContext == null)
        {
            LogUtil.Error($"{d_method_name}: WorldPlayerContext is null, aborting.");
            return;
        }

        var configRange = context.Config.Range;

        var vehicles = VehicleManager.Instance?.vehiclesActive;
        if (vehicles == null)
        {
            LogUtil.Error($"{d_method_name}: VehicleManager returned null list, aborting.");
            return;
        }

        foreach (var vehicle in vehicles)
        {
            // Must have storage and a non-empty bag
            if (vehicle.bag == null || vehicle.bag.IsEmpty() || !vehicle.hasStorage())
            {
                continue;
            }

            // Range check using WorldPlayerContext
            if (!context.WorldPlayerContext.IsWithinRange(vehicle.position, configRange))
            {
                continue;
            }

            // Locked for player check
            if (vehicle.IsLockedForLocalPlayer(context.WorldPlayerContext.Player))
            {
                continue;
            }

            context.Vehicles.Add(vehicle);
        }
    }
}