using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Handles finding and processing items from drone storage sources.
/// </summary>
internal static class DroneItemDiscovery
{
    public static void FindItems(StorageContext context)
    {
        const string d_MethodName = nameof(FindItems);

        var config = context.Config;
        var world = context.WorldPlayerContext;

        if (!config.PullFromDrones)
        {
            return;
        }

        var drones = DroneManager.Instance?.dronesActive;
        if (drones == null)
        {
            ModLogger.Error($"{d_MethodName}: VehicleManager returned null list, aborting.");
            return;
        }

        foreach (var drone in drones)
        {
            if (drone == null)
            {
                continue;
            }

            if (!world.IsWithinRange(drone.position, config.Range))
            {
                continue;
            }

            // Check accessibility
            if (world.IsOwnedbyLocalUser(drone))
            {
                continue;
            }


            ProcessDroneItems(context, drone);
        }
    }

    private static void ProcessDroneItems(StorageContext context, EntityDrone drone)
    {
        const string d_MethodName = nameof(ProcessDroneItems);

        if (drone.bag == null || drone.bag.IsEmpty())
        {
            return;
        }

        var sources = context.Sources;

        var sourceAdapter = new StorageSourceAdapter<EntityDrone>(
            drone,
            sources.EqualsDroneCollectorFunc,
            sources.GetItemsDroneCollectorFunc,
            sources.MarkModifiedDroneCollectorFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);

        if (validStacksRegistered > 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: Entity {drone.EntityName} has {validStacksRegistered} item stacks");
        }
    }
}