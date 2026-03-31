using System;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Multiplayer;
using BeyondStorage.Scripts.TileEntities;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Handles finding and processing items from tile entity storage sources.
/// </summary>
internal static class TileEntityItemDiscovery
{
    public static void FindItems(StorageContext context)
    {
#if DEBUG
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
        if (state.Config.PullFromDewCollectors && tileEntity is TileEntityCollector dewCollector)
        {
            ProcessCollectorEntity(dewCollector, state);
            return;
        }

        if (state.Config.PullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
        {
            ProcessWorkstationEntity(workstation, state);
            return;
        }

        // Process lootables (containers) - always enabled since they're primary storage
        if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable))
        {
            ProcessLootableEntity(lootable, tileEntity, distance, state);
        }
    }

    #region Dew Collector Processing

    private static void ProcessCollectorEntity(TileEntityCollector collector, TileEntityProcessingState state)
    {
        state.DewCollectorsProcessed++;

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

    private static int ProcessCollectorItems(TileEntityCollector collector, TileEntityProcessingState state)
    {
#if DEBUG
        //const string d_MethodName = nameof(ProcessCollectorItems);
#endif        
        var context = state.Context;

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<TileEntityCollector>(
            collector,
            sources.EqualsCollectorFunc,
            sources.GetCollectorItemsFunc,
            sources.MarkCollectorModifiedFunc,
            sources.GetCollectorNameFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        if (state.ValidDewCollectorsFound < 1) { ModLogger.DebugLog($"BS_NAME_TEST: Collector Name = {sourceAdapter.GetName()}"); }  // TODO: Remove this after verifying names are correct
        state.ValidDewCollectorsFound++;

        if (validStacksRegistered > 0)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {dewCollector}");
#endif
        }

        return validStacksRegistered;
    }

    #endregion

    #region Workstation Processing

    private static void ProcessWorkstationEntity(TileEntityWorkstation workstation, TileEntityProcessingState state)
    {
        state.WorkstationsProcessed++;

        if (!ShouldProcessWorkstation(workstation))
        {
            return;
        }

        ProcessWorkstationItems(workstation, state);
    }

    private static bool ShouldProcessWorkstation(TileEntityWorkstation workstation)
    {
        if (!workstation.IsPlayerPlaced)
        {
            return false;
        }

        return true;
    }

    private static int ProcessWorkstationItems(TileEntityWorkstation workstation, TileEntityProcessingState state)
    {
#if DEBUG
        //const string d_MethodName = nameof(ProcessWorkstationItems);
#endif
        var context = state.Context;

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<TileEntityWorkstation>(
            workstation,
            sources.EqualsWorkstationFunc,
            sources.GetWorkstationItemsFunc,
            sources.MarkWorkstationModifiedFunc,
            sources.GetWorkstationNameFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        if (state.ValidWorkstationsFound < 1) { ModLogger.DebugLog($"BS_NAME_TEST: Lootable Name = {sourceAdapter.GetName()}"); }  // TODO: Remove this after verifying names are correct
        state.ValidWorkstationsFound++;

        if (validStacksRegistered > 0)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {workstation}");
#endif
        }

        return validStacksRegistered;
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

        ProcessLootableItems(lootable, tileEntity, state);
        ProcessLootableContainer(lootable, distance, state);
    }

    private static bool ShouldProcessLootable(ITileEntityLootable lootable)
    {
        return lootable.bPlayerStorage;
    }

    private static int ProcessLootableItems(ITileEntityLootable lootable, TileEntity tileEntity, TileEntityProcessingState state)
    {
#if DEBUG
        const string d_MethodName = nameof(ProcessLootableItems);
#endif

        var context = state.Context;

#if DEBUG
        LootableItemHandler.LogLootableSlotLocks(context, lootable, tileEntity, d_MethodName);
#endif

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<ITileEntityLootable>(
            lootable,
            sources.EqualsLootableFunc,
            sources.GetLootableItemsFunc,
            sources.MarkLootableModifiedFunc,
            sources.GetLootableNameFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        if (state.ValidLootablesFound<1) { ModLogger.DebugLog($"BS_NAME_TEST: Lootable Name = {sourceAdapter.GetName()}"); }  // TODO: Remove this after verifying names are correct
        state.ValidLootablesFound++;

        if (validStacksRegistered > 0)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: {validStacksRegistered} item stacks pulled from {tileEntity}");
#endif
        }
        return validStacksRegistered;
    }

    private static void ProcessLootableContainer(ITileEntityLootable lootable, float distance, TileEntityProcessingState state)
    {
        var context = state.Context;

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<ITileEntityLootable>(
            lootable,
            sources.EqualsLootableFunc,
            sources.GetLootableItemsFunc,
            sources.MarkLootableModifiedFunc,
            sources.GetLootableNameFunc
        );

        sources.DataStore.RegisterContainerSource(sourceAdapter, distance);
        state.ValidContainersFound++;
    }

    #endregion

    #region Logging and Diagnostics

    private static void LogProcessingResults(string methodName, TileEntityProcessingState state)
    {
        ModLogger.DebugLog($"{methodName}: Processed {state.ChunksProcessed} chunks ({state.NullChunks} null), " +
                          $"{state.TileEntitiesProcessed} tile entities - " +
                          $"DewCollectors: {state.ValidDewCollectorsFound}/{state.DewCollectorsProcessed}, " +
                          $"Workstations: {state.ValidWorkstationsFound}/{state.WorkstationsProcessed}, " +
                          $"Lootables: {state.ValidLootablesFound}/{state.LootablesProcessed}");
    }

    #endregion
}