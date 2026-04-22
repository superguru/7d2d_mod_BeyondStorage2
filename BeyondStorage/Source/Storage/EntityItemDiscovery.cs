using BeyondStorage.Source.Data;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.Storage;

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
#endif
        if (!ValidateWorldEntityList())
        {
            return;
        }

        var processingState = new EntityProcessingState(context);
        var entities = GameManager.Instance.World.Entities.list;

        // Cache configuration values
        var pullFromVehicles = processingState.Config.PullFromVehicleStorage;
        var pullFromDrones = processingState.Config.PullFromDrones;

        foreach (var entity in entities)
        {
            ProcessEntity(entity, processingState, pullFromVehicles, pullFromDrones);
        }

#if DEBUG
        //LogProcessingResults(d_MethodName, processingState);
#endif
    }

    private static bool ValidateWorldEntityList()
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

    private static void ProcessEntity(Entity entity, EntityProcessingState state, bool pullFromVehicles, bool pullFromDrones)
    {
        if (entity == null)
        {
            state.NullEntities++;
            return;
        }

        state.EntitiesProcessed++;


        float distance = state.World.DistanceToPlayer(entity.position);

        if (pullFromVehicles && entity is EntityVehicle vehicle)
        {
            ProcessVehicleEntity(vehicle, distance, state);
            return;
        }

        if (pullFromDrones && entity is EntityDrone drone)
        {
            ProcessDroneEntity(drone, distance, state);
            return;
        }
    }

    #region Vehicle Processing

    private static void ProcessVehicleEntity(EntityVehicle vehicle, float distance, EntityProcessingState state)
    {
        state.VehiclesProcessed++;

        if (!ShouldProcessVehicle(vehicle, state))
        {
            return;
        }

        ProcessVehicleItems(vehicle, distance, state);
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

    private static int ProcessVehicleItems(EntityVehicle vehicle, float distance, EntityProcessingState state)
    {
#if DEBUG
        //const string d_MethodName = nameof(ProcessVehicleItems);
#endif
        var context = state.Context;
        var sourceAdapter = StorageSourceAdapterFactory.CreateVehicleStorageSourceAdapter(context, vehicle);

        context.Sources.DataStore.RegisterSource(sourceAdapter, distance, out int consumableStacksRegistered);
        state.ValidVehiclesFound++;
#if DEBUG
        if (consumableStacksRegistered > 0)
        {
            //ModLogger.DebugLog($"{d_MethodName}: {consumableStacksRegistered} item stacks pulled from {vehicle}");
        }
#endif
        return consumableStacksRegistered;
    }

    #endregion

    #region Drone Processing

    private static void ProcessDroneEntity(EntityDrone drone, float distance, EntityProcessingState state)
    {
        state.DronesProcessed++;

        if (!ShouldProcessDrone(drone, state))
        {
            return;
        }

        ProcessDroneItems(drone, distance, state);
    }

    private static bool ShouldProcessDrone(EntityDrone drone, EntityProcessingState state)
    {
#if DEBUG
        const string d_MethodName = nameof(ShouldProcessDrone);
#endif
        // Check ownership
        if (!state.World.IsOwnedByLocalUser(drone))
        {
            return false;
        }

        // Check if drone has items
        if (drone.bag == null || drone.bag.IsEmpty())
        {
            return false;
        }

        // HAS to be done right after sanity checks, otherwise we might try to access a network synced drone
        if (drone.isOwnerSyncPending)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Drone {drone} is currently locked={drone.isInteractionLocked} or pending sync={drone.isOwnerSyncPending}, skipping.");
#endif
            return false;
        }

        if (drone.isShutdownPending || drone.isShutdown)
        {
            return false;
        }

        if (!drone.IsUserAllowed(state.World.InternalLocalUserIdentifier))
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Drone {drone} is not accessible by the local user, skipping.");
#endif
            return false;
        }

        return true;
    }

    private static int ProcessDroneItems(EntityDrone drone, float distance, EntityProcessingState state)
    {
#if DEBUG
        //const string d_MethodName = nameof(ProcessDroneItems);
#endif
        var context = state.Context;
        var sourceAdapter = StorageSourceAdapterFactory.CreateDroneStorageSourceAdapter(context, drone);

        context.Sources.DataStore.RegisterSource(sourceAdapter, distance, out int consumableStacksRegistered);
        state.ValidDronesFound++;

#if DEBUG
        if (consumableStacksRegistered > 0)
        {
            //ModLogger.DebugLog($"{d_MethodName}: {consumableStacksRegistered} item stacks pulled from {drone}");
        }
#endif
        return consumableStacksRegistered;
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