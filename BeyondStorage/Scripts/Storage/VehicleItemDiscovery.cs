using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Handles finding and processing items from vehicle storage sources.
/// </summary>
internal static class VehicleItemDiscovery
{
    public static void FindItems(StorageContext context)
    {
        const string d_MethodName = nameof(FindItems);

        var config = context.Config;
        var world = context.WorldPlayerContext;

        if (!config.PullFromVehicleStorage)
        {
            return;
        }

        var vehicles = VehicleManager.Instance?.vehiclesActive;
        if (vehicles == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: VehicleManager returned null list, aborting. This is a problem in the game itself, because VehicleManager should ALWAYS have a list of vehicles.");
            return;
        }

        foreach (var vehicle in vehicles)
        {
            if (vehicle == null)
            {
                continue;
            }

            if (!world.IsWithinRange(vehicle.position, config.Range))
            {
                continue;
            }

            if (vehicle.IsLockedForLocalPlayer(world.Player))
            {
                continue;
            }

            ProcessVehicleItems(context, vehicle);
        }
    }

    private static int ProcessVehicleItems(StorageContext context, EntityVehicle vehicle)
    {
        if (vehicle.bag == null || vehicle.bag.IsEmpty() || !vehicle.hasStorage())
        {
            return 0;
        }


        var sources = context.Sources;

        var sourceAdapter = new StorageSourceAdapter<EntityVehicle>(
            vehicle,
            sources.EqualsVehicleFunc,
            sources.GetItemsVehicleFunc,
            sources.MarkModifiedVehicleFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        return validStacksRegistered;
    }
}