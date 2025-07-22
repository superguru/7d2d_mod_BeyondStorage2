using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class VehicleUtils
{
    public static IEnumerable<EntityVehicle> GetAvailableVehicleStorages()
    {
        const string d_method_name = "GetAvailableVehicleStorages";

        var world = GameManager.Instance.World;
        if (world == null)
        {
            yield break;
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            yield break;
        }

        LogUtil.DebugLog($"{d_method_name}: Starting");

        var playerPos = player.position;
        var configRange = ModConfig.Range();

        var entities = world.Entities?.list;
        if (entities == null)
        {
            yield break;
        }

        foreach (var entity in entities)
        {
            // Only consider vehicles
            if (entity is not EntityVehicle vehicle)
            {
                continue;
            }

            // Must have storage and a non-empty bag
            if (vehicle.bag == null || vehicle.bag.IsEmpty() || !vehicle.hasStorage())
            {
                continue;
            }

            // Range check
            if (configRange > 0 && Vector3.Distance(playerPos, vehicle.position) >= configRange)
            {
                continue;
            }

            // Locked for player check
            if (vehicle.IsLockedForLocalPlayer(player))
            {
                continue;
            }

            yield return vehicle;
        }
    }
}