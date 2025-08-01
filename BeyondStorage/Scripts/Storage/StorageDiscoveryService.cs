using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Multiplayer;

namespace BeyondStorage.Scripts.Storage
{
    /// <summary>
    /// Service responsible for discovering storage sources (tile entities and vehicles) based on configuration and accessibility.
    /// </summary>
    public static class StorageDiscoveryService
    {
        /// <summary>
        /// Discovers all available storage sources within range and adds them to the provided collection.
        /// </summary>
        /// <param name="sources">The collection to populate with discovered sources</param>
        /// <param name="worldPlayerContext">Context containing world and player information</param>
        /// <param name="config">Configuration snapshot with discovery settings</param>
        public static void DiscoverStorageSources(StorageSourceManager sources, WorldPlayerContext worldPlayerContext, ConfigSnapshot config)
        {
            const string d_MethodName = nameof(DiscoverStorageSources);

            if (!ValidateParameters(sources, worldPlayerContext, config, d_MethodName))
            {
                return;
            }

            DiscoverTileEntitySources(sources, worldPlayerContext, config);
            DiscoverVehicleStorages(sources, worldPlayerContext, config);
        }

        private static bool ValidateParameters(StorageSourceManager sources, WorldPlayerContext worldPlayerContext, ConfigSnapshot config, string methodName)
        {
            if (sources == null)
            {
                ModLogger.Error($"{methodName}: StorageSourceManager is null, aborting.");
                return false;
            }

            if (worldPlayerContext == null)
            {
                ModLogger.Error($"{methodName}: WorldPlayerContext is null, aborting.");
                return false;
            }

            if (config == null)
            {
                ModLogger.Error($"{methodName}: ConfigSnapshot is null, aborting.");
                return false;
            }

            return true;
        }

        private static void DiscoverTileEntitySources(StorageSourceManager sources, WorldPlayerContext worldPlayerContext, ConfigSnapshot config)
        {
            const string d_MethodName = nameof(DiscoverTileEntitySources);

            int chunksProcessed = 0;
            int nullChunks = 0;
            int tileEntitiesProcessed = 0;

            foreach (var chunk in worldPlayerContext.ChunkCacheCopy)
            {
                if (chunk == null)
                {
                    nullChunks++;
                    continue;
                }

                chunksProcessed++;

                var tileEntityList = chunk.tileEntities?.list;
                if (tileEntityList == null)
                {
                    continue;
                }

                foreach (var tileEntity in tileEntityList)
                {
                    tileEntitiesProcessed++;

                    if (tileEntity.IsRemoving)
                    {
                        continue;
                    }

                    var tileEntityWorldPos = tileEntity.ToWorldPos();

                    // Early range check to avoid unnecessary processing
                    if (!worldPlayerContext.IsWithinRange(tileEntityWorldPos, config.Range))
                    {
                        continue;
                    }

                    // Check locks early
                    if (TileEntityLockManager.LockedTileEntities.Count > 0)
                    {
                        if (TileEntityLockManager.LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) && entityId != worldPlayerContext.PlayerEntityId)
                        {
                            continue;
                        }
                    }

                    // Check accessibility
                    if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                    {
                        if (!worldPlayerContext.CanAccessLockable(tileLockable))
                        {
                            continue;
                        }
                    }

                    // Process each type separately with clear logic
                    if (config.PullFromDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
                    {
                        ProcessDewCollector(sources, dewCollector);
                        continue;
                    }

                    if (config.PullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                    {
                        ProcessWorkstation(sources, workstation);
                        continue;
                    }

                    // Process lootables (containers) - always enabled since they're primary storage
                    if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable))
                    {
                        ProcessLootable(sources, lootable, tileEntity, config);
                        continue;
                    }
                }
            }

            ModLogger.DebugLog($"{d_MethodName}: Processed {chunksProcessed} chunks, {nullChunks} null chunks, {tileEntitiesProcessed} tile entities");
        }

        private static void ProcessDewCollector(StorageSourceManager sources, TileEntityDewCollector dewCollector)
        {
            if (dewCollector.bUserAccessing)
            {
                return;
            }

            if (!HasValidItems(dewCollector.items))
            {
                return;
            }

            sources.DewCollectors.Add(dewCollector);
        }

        private static void ProcessWorkstation(StorageSourceManager sources, TileEntityWorkstation workstation)
        {
            if (!workstation.IsPlayerPlaced)
            {
                return;
            }

            if (!HasValidItems(workstation.output))
            {
                return;
            }

            sources.Workstations.Add(workstation);
        }

        private static void ProcessLootable(StorageSourceManager sources, ITileEntityLootable lootable, TileEntity tileEntity, ConfigSnapshot config)
        {
            if (!lootable.bPlayerStorage)
            {
                return;
            }

            if (config.OnlyStorageCrates)
            {
                if (!tileEntity.TryGetSelfOrFeature(out TEFeatureStorage _))
                {
                    return;
                }
            }

            if (!HasValidItems(lootable.items))
            {
                return;
            }

            sources.Lootables.Add(lootable);
        }

        private static bool HasValidItems(ItemStack[] items)
        {
            if (items == null || items.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item != null && item.count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DiscoverVehicleStorages(StorageSourceManager sources, WorldPlayerContext worldPlayerContext, ConfigSnapshot config)
        {
            const string d_MethodName = nameof(DiscoverVehicleStorages);

            if (!config.PullFromVehicleStorage)
            {
                return;
            }

            var vehicles = VehicleManager.Instance?.vehiclesActive;
            if (vehicles == null)
            {
                ModLogger.Error($"{d_MethodName}: VehicleManager returned null list, aborting.");
                return;
            }

            foreach (var vehicle in vehicles)
            {
                if (vehicle.bag == null || vehicle.bag.IsEmpty() || !vehicle.hasStorage())
                {
                    continue;
                }

                if (!worldPlayerContext.IsWithinRange(vehicle.position, config.Range))
                {
                    continue;
                }

                if (vehicle.IsLockedForLocalPlayer(worldPlayerContext.Player))
                {
                    continue;
                }

                sources.Vehicles.Add(vehicle);
            }
        }
    }
}