using System.Linq;
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
        public static void DiscoverStorageSources(StorageSourceCollection sources, WorldPlayerContext worldPlayerContext, ConfigSnapshot config)
        {
            if (worldPlayerContext == null)
            {
                ModLogger.Error($"{nameof(DiscoverStorageSources)}: WorldPlayerContext is null, aborting.");
                return;
            }

            DiscoverTileEntitySources(sources, worldPlayerContext, config);
            DiscoverVehicleStorages(sources, worldPlayerContext, config);
        }

        private static void DiscoverTileEntitySources(StorageSourceCollection sources, WorldPlayerContext worldPlayerContext, ConfigSnapshot config)
        {
            const string d_MethodName = nameof(DiscoverTileEntitySources);

            var dewCollectors = sources.DewCollectors;
            var workstations = sources.Workstations;
            var lootables = sources.Lootables;

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

                    bool isLootable = tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable);
                    bool hasStorageFeature = config.OnlyStorageCrates ? tileEntity.TryGetSelfOrFeature(out TEFeatureStorage _) : true;

                    if (!(tileEntity is TileEntityDewCollector ||
                          tileEntity is TileEntityWorkstation ||
                          isLootable))
                    {
                        continue;
                    }

                    var tileEntityWorldPos = tileEntity.ToWorldPos();

                    if (TileEntityLockManager.LockedTileEntities.Count > 0)
                    {
                        if (TileEntityLockManager.LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) && entityId != worldPlayerContext.PlayerEntityId)
                        {
                            continue;
                        }
                    }

                    if (!worldPlayerContext.IsWithinRange(tileEntityWorldPos, config.Range))
                    {
                        continue;
                    }

                    if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                    {
                        if (!worldPlayerContext.CanAccessLockable(tileLockable))
                        {
                            continue;
                        }
                    }

                    if (config.PullFromDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
                    {
                        if (dewCollector.bUserAccessing)
                        {
                            continue;
                        }

                        if (dewCollector.items?.Length <= 0 || !dewCollector.items.Any(item => item?.count > 0))
                        {
                            continue;
                        }

                        dewCollectors.Add(dewCollector);
                        continue;
                    }

                    if (config.PullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                    {
                        if (!workstation.IsPlayerPlaced)
                        {
                            continue;
                        }

                        if (workstation.output?.Length <= 0 || !workstation.output.Any(item => item?.count > 0))
                        {
                            continue;
                        }

                        workstations.Add(workstation);
                        continue;
                    }

                    if (lootable != null)
                    {
                        if (!lootable.bPlayerStorage)
                        {
                            continue;
                        }

                        if (config.OnlyStorageCrates && !hasStorageFeature)
                        {
                            continue;
                        }

                        if (lootable.items?.Length <= 0 || !lootable.items.Any(item => item?.count > 0))
                        {
                            continue;
                        }

                        lootables.Add(lootable);
                        continue;
                    }
                }
            }

            ModLogger.DebugLog($"{d_MethodName}: Processed {chunksProcessed} chunks, {nullChunks} null chunks, {tileEntitiesProcessed} tile entities");
        }

        private static void DiscoverVehicleStorages(StorageSourceCollection sources, WorldPlayerContext worldPlayerContext, ConfigSnapshot config)
        {
            const string d_MethodName = nameof(DiscoverVehicleStorages);

            var configRange = config.Range;

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

                if (!worldPlayerContext.IsWithinRange(vehicle.position, configRange))
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