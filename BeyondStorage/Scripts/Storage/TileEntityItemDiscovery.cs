using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Multiplayer;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Handles finding and processing items from tile entity storage sources.
/// </summary>
internal static class TileEntityItemDiscovery
{
    public static void FindItems(StorageContext context)
    {
        const string d_MethodName = nameof(FindItems);

        var config = context.Config;
        var world = context.WorldPlayerContext;
        var sources = context.Sources;
        var playerId = world.PlayerEntityId;

        bool pullFromDewCollectors = config.PullFromDewCollectors;
        bool pullFromWorkstationOutputs = config.PullFromWorkstationOutputs;
        bool hasLockedEntities = TileEntityLockManager.LockedTileEntities.Count > 0;

        int chunksProcessed = 0;
        int nullChunks = 0;
        int tileEntitiesProcessed = 0;

        foreach (var chunk in world.ChunkCacheCopy)
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
                if (!world.IsWithinRange(tileEntityWorldPos, config.Range))
                {
                    continue;
                }

                // Check locks early
                if (hasLockedEntities)
                {
                    if (TileEntityLockManager.LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) && entityId != playerId)
                    {
                        continue;
                    }
                }

                // Check accessibility
                if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                {
                    if (!world.CanAccessLockable(tileLockable))
                    {
                        continue;
                    }
                }

                // Process each type separately with clear logic
                if (pullFromDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
                {
                    ProcessDewCollectorItems(sources, dewCollector);
                    continue;
                }

                if (pullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                {
                    ProcessWorkstationItems(sources, workstation);
                    continue;
                }

                // Process lootables (containers) - always enabled since they're primary storage
                if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable))
                {
                    ProcessLootableItems(sources, lootable, config, tileEntity);
                    continue;
                }
            }
        }

        ModLogger.DebugLog($"{d_MethodName}: Processed {chunksProcessed} chunks, {nullChunks} null chunks, {tileEntitiesProcessed} tile entities");
    }

    private static void ProcessDewCollectorItems(StorageDataManager sources, TileEntityDewCollector dewCollector)
    {
        const string d_MethodName = nameof(ProcessDewCollectorItems);

        if (dewCollector.bUserAccessing)
        {
            return;
        }

        var sourceAdapter = new StorageSourceAdapter<TileEntityDewCollector>(
            dewCollector,
            sources.EqualsDewCollectorFunc,
            sources.GetItemsDewCollectorFunc,
            sources.MarkModifiedDewCollectorFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);

        if (validStacksRegistered > 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: Found {validStacksRegistered} item stacks");
        }
    }

    private static void ProcessWorkstationItems(StorageDataManager sources, TileEntityWorkstation workstation)
    {
        const string d_MethodName = nameof(ProcessWorkstationItems);

        if (!workstation.IsPlayerPlaced)
        {
            return;
        }

        var sourceAdapter = new StorageSourceAdapter<TileEntityWorkstation>(
            workstation,
            sources.EqualsWorkstationFunc,
            sources.GetItemsWorkstationFunc,
            sources.MarkModifiedWorkstationFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);

        if (validStacksRegistered > 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: Found {validStacksRegistered} item stacks");
        }
    }

    private static void ProcessLootableItems(StorageDataManager sources, ITileEntityLootable lootable, ConfigSnapshot config, TileEntity tileEntity)
    {
        const string d_MethodName = nameof(ProcessLootableItems);

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

        var sourceAdapter = new StorageSourceAdapter<ITileEntityLootable>(
            lootable,
            sources.EqualsLootableFunc,
            sources.GetItemsLootableFunc,
            sources.MarkModifiedLootableFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);

        if (validStacksRegistered > 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: EntityId {lootable.EntityId} found {validStacksRegistered} item stacks");
        }
    }
}