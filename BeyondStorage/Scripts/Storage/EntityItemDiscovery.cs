using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Handles finding and processing items from entity storage sources (vehicles and drones).
/// This service iterates through World.Entities.list to find storage-capable entities,
/// replacing the manager-based approach used by VehicleItemDiscovery and DroneItemDiscovery.
/// </summary>
internal static class EntityItemDiscovery
{
    public static void FindItems(StorageContext context)
    {
#if DEBUG
        const string d_MethodName = nameof(FindItems);
#endif
        if (!ValidateWorldEntityList(context))
        {
            return;
        }

        var processingState = new EntityProcessingState(context);
        var entities = GameManager.Instance.World.Entities.list;

        // Cache configuration values
        var pullFromVehicles = processingState.Config.PullFromVehicleStorage;
        var pullFromDrones = processingState.Config.PullFromDrones;
        var configRange = processingState.Config.Range;

        foreach (var entity in entities)
        {
            ProcessEntity(entity, processingState, pullFromVehicles, pullFromDrones, configRange);
        }

#if DEBUG
        LogProcessingResults(d_MethodName, processingState);
#endif
    }

    private static bool ValidateWorldEntityList(StorageContext context)
    {
        var world = GameManager.Instance?.World;
        if (world == null)
        {
            var diagnosticState = WorldTools.GetWorldDiagnosticState();
            ModLogger.DebugLog($"{nameof(FindItems)}: GameManager.Instance.World is null, aborting. {diagnosticState}");
            return false;
        }

        var entities = world.Entities;
        if (entities == null)
        {
            var diagnosticState = WorldTools.GetWorldDiagnosticState();
            ModLogger.DebugLog($"{nameof(FindItems)}: World.Entities is null, aborting. {diagnosticState}");
            return false;
        }

        var entityList = entities.list;
        if (entityList == null)
        {
            var diagnosticState = WorldTools.GetWorldDiagnosticState();
            ModLogger.DebugLog($"{nameof(FindItems)}: World.Entities.list is null, aborting. {diagnosticState}");
            return false;
        }

        return true;
    }

    private static void ProcessEntity(Entity entity, EntityProcessingState state, bool pullFromVehicles, bool pullFromDrones, float configRange)
    {
        if (entity == null)
        {
            state.NullEntities++;
            return;
        }

        state.EntitiesProcessed++;

        // Early range check to avoid unnecessary processing
        if (!state.World.IsWithinRange(entity.position, configRange))
        {
            return;
        }

        // Process based on entity type using pattern matching
        if (pullFromVehicles && entity is EntityVehicle vehicle)
        {
            ProcessVehicleEntity(state.Context, vehicle, state);
            return;
        }

        if (pullFromDrones && entity is EntityDrone drone)
        {
            ProcessDroneEntity(state.Context, drone, state);
            return;
        }
    }

    #region Vehicle Processing

    private static void ProcessVehicleEntity(StorageContext context, EntityVehicle vehicle, EntityProcessingState state)
    {
        state.VehiclesProcessed++;

        if (!ShouldProcessVehicle(vehicle, state))
        {
            return;
        }

        ProcessVehicleItems(context, vehicle);
        state.ValidVehiclesFound++;
    }

    private static bool ShouldProcessVehicle(EntityVehicle vehicle, EntityProcessingState state)
    {
        // Check if vehicle has storage and items
        if (vehicle.bag == null || vehicle.bag.IsEmpty() || !vehicle.hasStorage())
        {
            return false;
        }

        // Check if vehicle is locked for local player
        if (vehicle.IsLockedForLocalPlayer(state.World.Player))
        {
            return false;
        }

        return true;
    }

    private static int ProcessVehicleItems(StorageContext context, EntityVehicle vehicle)
    {
#if DEBUG
        const string d_MethodName = nameof(ProcessVehicleItems);
#endif

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<EntityVehicle>(
            vehicle,
            sources.EqualsVehicleFunc,
            sources.GetItemsVehicleFunc,
            sources.MarkModifiedVehicleFunc
        );

        int validStacksRegistered = 0;
        sources?.DataStore?.RegisterSource(sourceAdapter, out validStacksRegistered);

        if (validStacksRegistered > 0)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {vehicle}");
#endif
        }

        return validStacksRegistered;
    }

    #endregion

    #region Drone Processing

    private static void ProcessDroneEntity(StorageContext context, EntityDrone drone, EntityProcessingState state)
    {
        state.DronesProcessed++;

        if (!ShouldProcessDrone(drone, state))
        {
            return;
        }

        ProcessDroneItems(context, drone);
        state.ValidDronesFound++;
    }

    private static bool ShouldProcessDrone(EntityDrone drone, EntityProcessingState state)
    {
        // Check ownership
        if (!state.World.IsOwnedbyLocalUser(drone))
        {
            return false;
        }

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

        if (!drone.IsUserAllowed(state.World.InternalLocalUserIdentifier))
        {
#if DEBUG
            ModLogger.DebugLog($"{nameof(ProcessDroneEntity)}: Drone {drone} is not accessible by the local user, skipping.");
#endif
            return false;
        }

        return true;
    }

    private static int ProcessDroneItems(StorageContext context, EntityDrone drone)
    {
#if DEBUG
        const string d_MethodName = nameof(ProcessDroneItems);
#endif

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

    #endregion

    #region Logging and Diagnostics

    private static void LogProcessingResults(string methodName, EntityProcessingState state)
    {
        ModLogger.DebugLog($"{methodName}: Processed {state.EntitiesProcessed} entities " +
                          $"({state.NullEntities} null), " +
                          $"Vehicles: {state.ValidVehiclesFound}/{state.VehiclesProcessed}, " +
                          $"Drones: {state.ValidDronesFound}/{state.DronesProcessed}");
    }

    #endregion
}