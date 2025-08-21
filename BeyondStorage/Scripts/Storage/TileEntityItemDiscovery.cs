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
                    ProcessDewCollectorItems(context, dewCollector);
                    continue;
                }

                if (pullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                {
                    ProcessWorkstationItems(context, workstation);
                    continue;
                }

                // Process lootables (containers) - always enabled since they're primary storage
                if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable))
                {
                    ProcessLootableItems(context, lootable, tileEntity);
                    continue;
                }
            }
        }

        ModLogger.DebugLog($"{d_MethodName}: Processed {chunksProcessed} chunks, {nullChunks} null chunks, {tileEntitiesProcessed} tile entities");
    }

    private static int ProcessDewCollectorItems(StorageContext context, TileEntityDewCollector dewCollector)
    {
#if DEBUG
        //const string d_MethodName = nameof(ProcessDewCollectorItems);
#endif
        if (dewCollector.bUserAccessing)
        {
            return 0;
        }

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<TileEntityDewCollector>(
            dewCollector,
            sources.EqualsDewCollectorFunc,
            sources.GetItemsDewCollectorFunc,
            sources.MarkModifiedDewCollectorFunc
        );

        int validStacksRegistered = 0;
        sources?.DataStore?.RegisterSource(sourceAdapter, out validStacksRegistered);

        if (validStacksRegistered > 0)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {dewCollector}");
#endif
        }

        return validStacksRegistered;
    }

    private static int ProcessWorkstationItems(StorageContext context, TileEntityWorkstation workstation)
    {
#if DEBUG
        //const string d_MethodName = nameof(ProcessWorkstationItems);
#endif
        if (!workstation.IsPlayerPlaced)
        {
            return 0;
        }

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<TileEntityWorkstation>(
            workstation,
            sources.EqualsWorkstationFunc,
            sources.GetItemsWorkstationFunc,
            sources.MarkModifiedWorkstationFunc
        );

        int validStacksRegistered = 0;
        sources?.DataStore?.RegisterSource(sourceAdapter, out validStacksRegistered);

        if (validStacksRegistered > 0)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {workstation}");
#endif
        }

        return validStacksRegistered;
    }

    private static int ProcessLootableItems(StorageContext context, ITileEntityLootable lootable, TileEntity tileEntity)
    {
#if DEBUG
        const string d_MethodName = nameof(ProcessLootableItems);
#endif

        if (!lootable.bPlayerStorage)
        {
            return 0;
        }

        var config = context.Config;
        if (config.OnlyStorageCrates)
        {
            if (!tileEntity.TryGetSelfOrFeature(out TEFeatureStorage storage) && storage != null)
            {
                return 0;
            }
        }

#if DEBUG
        LootableItemHandler.LogLootableSlotLocks(context, lootable, tileEntity, d_MethodName);
#endif

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<ITileEntityLootable>(
            lootable,
            sources.EqualsLootableFunc,
            sources.GetItemsLootableFunc,
            sources.MarkModifiedLootableFunc
        );

        int validStacksRegistered = 0;
        sources?.DataStore?.RegisterSource(sourceAdapter, out validStacksRegistered);

        if (validStacksRegistered > 0)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {tileEntity}");
#endif
        }

        return validStacksRegistered;
    }
}