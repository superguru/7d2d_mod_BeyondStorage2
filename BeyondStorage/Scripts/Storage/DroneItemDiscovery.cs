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
            ModLogger.DebugLog($"{d_MethodName}: DroneManager returned null list, aborting. This is a problem in the game itself, because DroneManager should ALWAYS have a list of drones.");
            return;
        }

        for (var i = 0; i < drones.Count; i++)
        {
            var drone = drones[i];
            if (drone == null)
            {
                continue;
            }

            // Check accessibility
            if (!world.IsOwnedbyLocalUser(drone))
            {
                continue;
            }

            if (!world.IsWithinRange(drone.position, config.Range))
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

        // HAS to be done first, otherwise we might try to access a network synced drone
        if (drone.isInteractionLocked || drone.isOwnerSyncPending)
        {
            return;
        }

        if (drone.isShutdownPending || drone.isShutdown)
        {
            return;
        }

        if (!drone.IsUserAllowed(context.WorldPlayerContext.InternalLocalUserIdentifier))
        {
            ModLogger.DebugLog($"{d_MethodName}: Drone {drone} is not accessible by the local user, skipping.");
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
            ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {drone}");
        }
    }
}