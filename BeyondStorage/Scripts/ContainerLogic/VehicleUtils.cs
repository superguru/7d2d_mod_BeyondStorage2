using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class VehicleUtils
{
    public static List<EntityVehicle> GetAvailableVehicleStorages()
    {
        const string d_method_name = "GetAvailableVehicleStorages";

        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.DebugLog($"{d_method_name}: World is null, aborting.");
            return new List<EntityVehicle>();
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.DebugLog($"{d_method_name}: Player is null, aborting.");
            return new List<EntityVehicle>();
        }

        LogUtil.DebugLog($"{d_method_name}: Starting");

        var playerPos = player.position;
        var configRange = ModConfig.Range();

        var entities = world.Entities?.list;
        if (entities == null)
        {
            LogUtil.DebugLog($"{d_method_name}: Entities list is null, aborting.");
            return new List<EntityVehicle>();
        }

        var result = new List<EntityVehicle>();

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

            result.Add(vehicle);
        }

        return result;
    }
}