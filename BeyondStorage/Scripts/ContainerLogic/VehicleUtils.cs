using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class VehicleUtils
{
    public static IEnumerable<EntityVehicle> GetAvailableVehicleStorages()
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();
        var playerPos = player.position;
        var configRange = ModConfig.Range();

        foreach (var entity in GameManager.Instance.World.Entities.list)
        {
            // skip anything not a vehicle
            if (entity is not EntityVehicle vehicle)
            {
                continue;
            }

            // skip vehicles outside of range
            bool isInRange = (configRange <= 0 || Vector3.Distance(playerPos, vehicle.position) < configRange);
            if (!isInRange)
            {
                continue;
            }

            // verify bag isn't null
            if (vehicle.bag == null)
            {
                continue;
            }

            // skip if empty
            if (vehicle.bag.IsEmpty())
            {
                continue;
            }

            // skip vehicles without storage
            if (!vehicle.hasStorage())
            {
                continue;
            }

            // skip vehicles locked for the player
            if (vehicle.IsLockedForLocalPlayer(player))
            {
                continue;
            }

            yield return vehicle;
        }
    }
}