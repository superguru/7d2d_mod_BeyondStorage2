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

    private static int ProcessDroneItems(StorageContext context, EntityDrone drone)
    {
#if DEBUG
        const string d_MethodName = nameof(ProcessDroneItems);
#endif
        if (drone.bag == null || drone.bag.IsEmpty())
        {
            return 0;
        }

        // HAS to be done first, otherwise we might try to access a network synced drone
        if (drone.isInteractionLocked || drone.isOwnerSyncPending)
        {
            return 0;
        }

        if (drone.isShutdownPending || drone.isShutdown)
        {
            return 0;
        }

        if (!drone.IsUserAllowed(context.WorldPlayerContext.InternalLocalUserIdentifier))
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Drone {drone} is not accessible by the local user, skipping.");
#endif
            return 0;
        }

        var sources = context.Sources;

        var sourceAdapter = new StorageSourceAdapter<EntityDrone>(
            drone,
            sources.EqualsDroneCollectorFunc,
            sources.GetItemsDroneCollectorFunc,
            sources.MarkModifiedDroneCollectorFunc
        );

        int validStacksRegistered = 0;
        sources?.DataStore?.RegisterSource(sourceAdapter, out validStacksRegistered);

        if (validStacksRegistered > 0)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {drone}");
#endif
        }

        return validStacksRegistered;
    }
}