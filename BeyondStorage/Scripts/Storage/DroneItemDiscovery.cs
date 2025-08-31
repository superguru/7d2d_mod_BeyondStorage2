using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game;
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

        if (!ValidateDroneManagerAvailability(d_MethodName))
        {
            return;
        }

        var droneManagerInstance = DroneManager.Instance;
        var drones = droneManagerInstance.dronesActive;

        if (!ValidateActiveDronesList(d_MethodName, drones))
        {
            return;
        }

        for (var i = 0; i < drones.Count; i++)
        {
            var drone = drones[i];
            if (drone == null)
            {
                continue;
            }

            if (!ValidateDroneAccessibility(world, drone, config.Range))
            {
                continue;
            }

            ProcessDroneItems(context, drone);
        }
    }

    /// <summary>
    /// Validates that DroneManager instance is available and logs detailed diagnostic information if not.
    /// </summary>
    /// <param name="methodName">The calling method name for logging</param>
    /// <returns>True if DroneManager.Instance is available, false otherwise</returns>
    private static bool ValidateDroneManagerAvailability(string methodName)
    {
        var droneManagerInstance = DroneManager.Instance;
        if (droneManagerInstance != null)
        {
            return true;
        }

        var diagnosticState = WorldTools.GetWorldDiagnosticState();
        ModLogger.DebugLog($"{methodName}: DroneManager.Instance is null, aborting. This is a problem in the game itself, because DroneManager should ALWAYS be available. {diagnosticState}");

        return false;
    }

    /// <summary>
    /// Validates that the dronesActive list is available and logs detailed diagnostic information if not.
    /// </summary>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="drones">The dronesActive list to validate</param>
    /// <returns>True if dronesActive list is available, false otherwise</returns>
    private static bool ValidateActiveDronesList(string methodName, List<EntityDrone> drones)
    {
        if (drones != null)
        {
            return true;
        }

        var diagnosticState = WorldTools.GetWorldDiagnosticState();
        ModLogger.DebugLog($"{methodName}: DroneManager.Instance.dronesActive is null, aborting. This is a problem in the game itself, because DroneManager should ALWAYS have a list of drones. {diagnosticState}");

        return false;
    }

    /// <summary>
    /// Validates that a drone is accessible by the local user and within range.
    /// </summary>
    /// <param name="world">The world player context</param>
    /// <param name="drone">The drone to validate</param>
    /// <param name="range">The maximum range for access</param>
    /// <returns>True if drone is accessible, false otherwise</returns>
    private static bool ValidateDroneAccessibility(WorldPlayerContext world, EntityDrone drone, float range)
    {
        // Check ownership
        if (!world.IsOwnedbyLocalUser(drone))
        {
            return false;
        }

        // Check range
        if (!world.IsWithinRange(drone.position, range))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a drone is ready for item processing by checking all necessary state conditions.
    /// </summary>
    /// <param name="drone">The drone to validate</param>
    /// <param name="worldPlayerContext">The world player context for user validation</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <returns>True if drone is ready for processing, false otherwise</returns>
    private static bool ValidateDroneProcessingReadiness(EntityDrone drone, WorldPlayerContext worldPlayerContext, string methodName)
    {
        // Check if drone has items
        if (drone.bag == null || drone.bag.IsEmpty())
        {
            return false;
        }

        // HAS to be done first, otherwise we might try to access a network synced drone
        if (drone.isInteractionLocked || drone.isOwnerSyncPending)
        {
            return false;
        }

        if (drone.isShutdownPending || drone.isShutdown)
        {
            return false;
        }

        if (!drone.IsUserAllowed(worldPlayerContext.InternalLocalUserIdentifier))
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: Drone {drone} is not accessible by the local user, skipping.");
#endif
            return false;
        }

        return true;
    }

    private static int ProcessDroneItems(StorageContext context, EntityDrone drone)
    {
        const string d_MethodName = nameof(ProcessDroneItems);

        if (!ValidateDroneProcessingReadiness(drone, context.WorldPlayerContext, d_MethodName))
        {
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
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {drone}");
#endif
        }

        return validStacksRegistered;
    }
}