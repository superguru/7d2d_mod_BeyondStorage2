using System;
using System.Linq;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Diagnostics;
internal static class PurgeBadDrones
{
    public static void DeleteBadDronesForLocalPlayer()
    {
        const string d_MethodName = nameof(DeleteBadDronesForLocalPlayer);

        if (WorldTools.IsServer())
        {
            return;
        }

        var worldContext = WorldPlayerContext.TryCreate(d_MethodName);
        if (worldContext == null)
        {
            ModLogger.Warning($"{d_MethodName}: Failed to create WorldPlayerContext, cannot delete bad drones");
            return;
        }

        worldContext.Player.GetOwnedEntities();

        var player = worldContext.Player;
        var owned = player.ownedEntities.Where(o => o?.ClassId > 0 && EntityClass.list[o.ClassId].entityClassName == "entityJunkDrone").ToList();
        ModLogger.DebugLog($"{d_MethodName}: found player owned drone entities {owned.Count()}");
        int dronesRemoved = 0;
        foreach (var drone in owned)
        {
            var entityId = drone.entityId;

            Entity entity = worldContext.World.GetEntity(entityId);
            if (entity != null)
            {
                continue;
            }

            ModLogger.DebugLog($"{d_MethodName}: removing bad drone entityId={entityId}");
            DeleteEntityById(entityId);

            player.ownedEntities.Remove(drone);
            var data = drone?.EntityCreationData;
            if (data != null)
            {
                data.belongsPlayerId = 0;
            }

            ModLogger.DebugLog($"{d_MethodName}: removed bad drone {++dronesRemoved}, entityId={entityId}");
        }

        if (dronesRemoved > 0)
        {
            DroneManager.Instance?.Save();
        }
    }

    /// <summary>
    /// Deletes an entity by its entity ID.
    /// </summary>
    /// <param name="entityId">The entity ID to delete</param>
    /// <returns>True if the entity was found and deleted, false otherwise</returns>
    public static bool DeleteEntityById(int entityId)
    {
        const string d_MethodName = nameof(DeleteEntityById);

        try
        {
            // Get the worldContext instance
            World world = GameManager.Instance.World;
            if (world == null)
            {
                ModLogger.Warning($"{d_MethodName}: World is null, cannot delete entity {entityId}");
                return false;
            }

            // Find the entity
            Entity entity = world.GetEntity(entityId);
            if (entity == null)
            {
                ModLogger.Warning($"{d_MethodName}: Entity {entityId} not found in worldContext");
                return false;
            }

            ModLogger.DebugLog($"{d_MethodName}: Deleting entity {entityId} of type {entity.GetType().Name}");

            // Remove the entity from the worldContext
            world.RemoveEntity(entityId, EnumRemoveEntityReason.Despawned);

            ModLogger.DebugLog($"{d_MethodName}: Successfully deleted entity {entityId}");
            return true;
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"{d_MethodName}: Failed to delete entity {entityId}: {ex.Message}", ex);
            return false;
        }
    }
}
