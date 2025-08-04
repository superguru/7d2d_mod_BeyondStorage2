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
            ModLogger.Error($"{d_MethodName}: VehicleManager returned null list, aborting.");
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

    private static void ProcessVehicleItems(StorageContext context, EntityVehicle vehicle)
    {
        const string d_MethodName = nameof(ProcessVehicleItems);

        if (vehicle.bag == null || vehicle.bag.IsEmpty() || !vehicle.hasStorage())
        {
            return;
        }

        var sources = context.Sources;

        var sourceAdapter = new StorageSourceAdapter<EntityVehicle>(
            vehicle,
            sources.EqualsVehicleFunc,
            sources.GetItemsVehicleFunc,
            sources.MarkModifiedVehicleFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);

        if (validStacksRegistered > 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: Entity {vehicle.EntityName} has {validStacksRegistered} item stacks");
        }
    }
}