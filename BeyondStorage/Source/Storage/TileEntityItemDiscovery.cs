using BeyondStorage.Source.Data;
using BeyondStorage.Source.Entities;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.Multiplayer;

namespace BeyondStorage.Source.Storage;

/// <summary>
/// Handles finding and processing items from tile entity storage sources.
/// </summary>
internal static class TileEntityItemDiscovery
{
    public static void FindItems(StorageContext context)
    {
#if DEBUG
        //const string d_MethodName = nameof(FindItems);
#endif
        var processingState = new TileEntityProcessingState(context);

        foreach (var chunk in context.WorldPlayerContext.ChunkCacheCopy)
        {
            ProcessChunk(chunk, processingState);
        }

#if DEBUG
        //LogProcessingResults(d_MethodName, processingState);
#endif
    }

    private static void ProcessChunk(Chunk chunk, TileEntityProcessingState state)
    {
        if (chunk == null)
        {
            state.NullChunks++;
            return;
        }

        state.ChunksProcessed++;

        var tileEntityList = chunk.tileEntities?.list;
        if (tileEntityList == null)
        {
            return;
        }

        foreach (var tileEntity in tileEntityList)
        {
            ProcessTileEntity(tileEntity, state);
        }
    }

    private static void ProcessTileEntity(TileEntity tileEntity, TileEntityProcessingState state)
    {
        state.TileEntitiesProcessed++;

        if (!ShouldProcessTileEntity(tileEntity, state, out float distance))
        {
            return;
        }

        ProcessValidTileEntity(tileEntity, state, distance);
    }

    private static bool ShouldProcessTileEntity(TileEntity tileEntity, TileEntityProcessingState state, out float distance)
    {
        distance = 0f;

        if (tileEntity.IsRemoving)
        {
            return false;
        }

        var tileEntityWorldPos = tileEntity.ToWorldPos();

        // Early range check to avoid unnecessary processing
        if (!state.World.IsWithinRange(tileEntityWorldPos, state.Config.Range, out distance))
        {
            return false;
        }

        // Check locks early
        if (state.HasLockedEntities)
        {
            if (TileEntityLockManager.LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) &&
                entityId != state.PlayerId)
            {
                return false;
            }
        }

        // Check accessibility
        if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
        {
            return state.World.CanAccessLockable(tileLockable);
        }

        return true;
    }

    private static void ProcessValidTileEntity(TileEntity tileEntity, TileEntityProcessingState state, float distance)
    {
        // Process each type separately with clear logic and collect stats
        if (state.Config.PullFromCollectors && tileEntity is TileEntityCollector collector)
        {
            ProcessCollectorEntity(collector, state);
            return;
        }

        if (state.Config.PullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
        {
            ProcessWorkstationEntity(workstation, distance, state);
            return;
        }

        // Process lootables (containers) - always enabled since they're primary storage
        if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable))
        {
            ProcessLootableEntity(lootable, tileEntity, distance, state);
        }
    }

    #region Collector Processing

    private static void ProcessCollectorEntity(TileEntityCollector collector, TileEntityProcessingState state)
    {
        state.CollectorsProcessed++;

        if (!ShouldProcessCollector(collector))
        {
            return;
        }

        ProcessCollectorItems(collector, state);
    }

    private static bool ShouldProcessCollector(TileEntityCollector collector)
    {
        if (collector.bUserAccessing)
        {
            return false;
        }

        return true;
    }

    private static void ProcessCollectorItems(TileEntityCollector collector, TileEntityProcessingState state)
    {
#if DEBUG
        //const string d_MethodName = nameof(ProcessCollectorItems);
#endif
        var context = state.Context;
        var sourceAdapter = StorageSourceAdapterFactory.CreateCollectorStorageSourceAdapter(context, collector);

        context.Sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        state.ValidCollectorsFound++;

#if DEBUG
        if (validStacksRegistered > 0)
        {
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {collector}");
        }
#endif
    }

    #endregion

    #region Workstation Processing

    private static void ProcessWorkstationEntity(TileEntityWorkstation workstation, float distance, TileEntityProcessingState state)
    {
        state.WorkstationsProcessed++;

        if (!ShouldProcessWorkstation(workstation))
        {
            return;
        }

        ProcessWorkstation(workstation, distance, state);
    }

    private static bool ShouldProcessWorkstation(TileEntityWorkstation workstation)
    {
        if (!workstation.IsPlayerPlaced)
        {
            return false;
        }

        return true;
    }

    private static void ProcessWorkstation(TileEntityWorkstation workstation, float distance, TileEntityProcessingState state)
    {
#if DEBUG
        //const string d_MethodName = nameof(ProcessWorkstation);
#endif
        var context = state.Context;
        var sourceAdapter = StorageSourceAdapterFactory.CreateWorkstationStorageSourceAdapter(context, workstation);

        context.Sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        context.Sources.DataStore.RegisterStorageSource(sourceAdapter, distance);

        state.ValidWorkstationsFound++;

#if DEBUG
        if (validStacksRegistered > 0)
        {
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {workstation}");
        }
#endif
    }

    #endregion

    #region Lootable Processing

    private static void ProcessLootableEntity(ITileEntityLootable lootable, TileEntity tileEntity, float distance, TileEntityProcessingState state)
    {
        state.LootablesProcessed++;

        if (!ShouldProcessLootable(lootable))
        {
            return;
        }

        ProcessLootable(lootable, tileEntity, distance, state);
    }

    private static bool ShouldProcessLootable(ITileEntityLootable lootable)
    {
        return lootable.bPlayerStorage;
    }

    private static void ProcessLootable(ITileEntityLootable lootable, TileEntity tileEntity, float distance, TileEntityProcessingState state)
    {
#if DEBUG
        const string d_MethodName = nameof(ProcessLootable);
#endif
        var context = state.Context;

#if DEBUG
        LootableHandler.LogLootableSlotLocks(context, lootable, tileEntity, d_MethodName);
#endif

        var sourceAdapter = StorageSourceAdapterFactory.CreateLootableStorageSourceAdapter(context, lootable);

        context.Sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        context.Sources.DataStore.RegisterStorageSource(sourceAdapter, distance);

        state.ValidLootablesFound++;
        state.ValidContainersFound++;

#if DEBUG
        if (validStacksRegistered > 0)
        {
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {tileEntity}");
        }
#endif
    }

    #endregion

    #region Logging and Diagnostics

    private static void LogProcessingResults(string methodName, TileEntityProcessingState state)
    {
        ModLogger.DebugLog($"{methodName}: Processed {state.ChunksProcessed} chunks ({state.NullChunks} null), " +
                          $"{state.TileEntitiesProcessed} tile entities - " +
                          $"Collectors: {state.ValidCollectorsFound}/{state.CollectorsProcessed}, " +
                          $"Workstations: {state.ValidWorkstationsFound}/{state.WorkstationsProcessed}, " +
                          $"Lootables: {state.ValidLootablesFound}/{state.LootablesProcessed}");
    }

    #endregion
}