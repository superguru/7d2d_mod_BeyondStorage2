using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;
using UnityEngine;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class VehicleUtils
{
    public const int DEFAULT_VEHICLE_LIST_CAPACITY = 8;

    public static List<EntityVehicle> GetAvailableVehicleStorages()
    {
        const string d_method_name = "GetAvailableVehicleStorages";

        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.Error($"{d_method_name}: World is null, aborting.");
            return [];
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.Error($"{d_method_name}: Player is null, aborting.");
            return [];
        }

        LogUtil.DebugLog($"{d_method_name}: Starting");

        var playerPos = player.position;
        var configRange = ModConfig.Range();

        var vehicles = VehicleManager.Instance?.vehiclesActive;
        if (vehicles == null)
        {
            LogUtil.Error($"{d_method_name}: VehicleManager returned null list, aborting.");
            return [];
        }

        var result = new List<EntityVehicle>(DEFAULT_VEHICLE_LIST_CAPACITY);

        foreach (var vehicle in vehicles)
        {
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