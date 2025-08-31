using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game;
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
#if DEBUG
        const string d_MethodName = nameof(FindItems);
#endif
        var processingState = new TileEntityProcessingState(context);

        foreach (var chunk in context.WorldPlayerContext.ChunkCacheCopy)
        {
            ProcessChunk(chunk, processingState);
        }

#if DEBUG
        LogProcessingResults(d_MethodName, processingState);
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

        if (!ShouldProcessTileEntity(tileEntity, state))
        {
            return;
        }

        ProcessValidTileEntity(tileEntity, state);
    }

    private static bool ShouldProcessTileEntity(TileEntity tileEntity, TileEntityProcessingState state)
    {
        if (tileEntity.IsRemoving)
        {
            return false;
        }

        var tileEntityWorldPos = tileEntity.ToWorldPos();

        // Early range check to avoid unnecessary processing
        if (!state.World.IsWithinRange(tileEntityWorldPos, state.Config.Range))
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

    private static void ProcessValidTileEntity(TileEntity tileEntity, TileEntityProcessingState state)
    {
        // Process each type separately with clear logic and collect stats
        if (state.Config.PullFromDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
        {
            ProcessDewCollectorEntity(state.Context, dewCollector, state);
            return;
        }

        if (state.Config.PullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
        {
            ProcessWorkstationEntity(state.Context, workstation, state);
            return;
        }

        // Process lootables (containers) - always enabled since they're primary storage
        if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable))
        {
            ProcessLootableEntity(state.Context, lootable, tileEntity, state);
        }
    }

    #region Dew Collector Processing

    private static void ProcessDewCollectorEntity(StorageContext context, TileEntityDewCollector dewCollector, TileEntityProcessingState state)
    {
        state.DewCollectorsProcessed++;

        if (!ShouldProcessDewCollector(dewCollector))
        {
            return;
        }

        ProcessDewCollectorItems(context, dewCollector);
        state.ValidDewCollectorsFound++;
    }

    private static bool ShouldProcessDewCollector(TileEntityDewCollector dewCollector)
    {
        if (dewCollector.bUserAccessing)
        {
            return false;
        }

        return true;
    }

    private static int ProcessDewCollectorItems(StorageContext context, TileEntityDewCollector dewCollector)
    {
#if DEBUG
#endif

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

    #endregion

    #region Workstation Processing

    private static void ProcessWorkstationEntity(StorageContext context, TileEntityWorkstation workstation, TileEntityProcessingState state)
    {
        state.WorkstationsProcessed++;

        if (!ShouldProcessWorkstation(workstation))
        {
            return;
        }

        ProcessWorkstationItems(context, workstation);
        state.ValidWorkstationsFound++;
    }

    private static bool ShouldProcessWorkstation(TileEntityWorkstation workstation)
    {
        if (!workstation.IsPlayerPlaced)
        {
            return false;
        }

        return true;
    }

    private static int ProcessWorkstationItems(StorageContext context, TileEntityWorkstation workstation)
    {
#if DEBUG
#endif

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

    #endregion

    #region Lootable Processing

    private static void ProcessLootableEntity(StorageContext context, ITileEntityLootable lootable, TileEntity tileEntity, TileEntityProcessingState state)
    {
        state.LootablesProcessed++;

        if (!ShouldProcessLootable(context, lootable, tileEntity))
        {
            return;
        }

        ProcessLootableItems(context, lootable, tileEntity);
        state.ValidLootablesFound++;
    }

    private static bool ShouldProcessLootable(StorageContext context, ITileEntityLootable lootable, TileEntity tileEntity)
    {
        if (!lootable.bPlayerStorage)
        {
            return false;
        }

        // Storage crates have the Storage feature tag
        if (!tileEntity.TryGetSelfOrFeature(out TEFeatureStorage storage) && storage != null)
        {
            var config = context.Config;
            if (!config.PullFromPlayerCraftedNonCrates)
            {
                // Things a player built, like a wall safe, or a desk, is lootable, lootable.bPlayerStorage is true, but doesn't have the Storage feature tag
                return false;

                // Things just lootable out in the world, like a garbage can, is never eligible for pulling
            }
        }

        return true;
    }

    private static int ProcessLootableItems(StorageContext context, ITileEntityLootable lootable, TileEntity tileEntity)
    {
#if DEBUG
        const string d_MethodName = nameof(ProcessLootableItems);
#endif

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

    /// <summary>
    /// Helper class to encapsulate processing state and reduce parameter passing
    /// </summary>
    private class TileEntityProcessingState
    {
        public readonly StorageContext Context;
        public readonly ConfigSnapshot Config;
        public readonly WorldPlayerContext World;
        public readonly int PlayerId;
        public readonly bool HasLockedEntities;

        public int ChunksProcessed = 0;
        public int NullChunks = 0;
        public int TileEntitiesProcessed = 0;

        // Stats for collectors, workstations, and lootables
        public int DewCollectorsProcessed = 0;
        public int ValidDewCollectorsFound = 0;
        public int WorkstationsProcessed = 0;
        public int ValidWorkstationsFound = 0;
        public int LootablesProcessed = 0;
        public int ValidLootablesFound = 0;

        public TileEntityProcessingState(StorageContext context)
        {
            Context = context;
            Config = context.Config;
            World = context.WorldPlayerContext;
            PlayerId = World.PlayerEntityId;
            HasLockedEntities = TileEntityLockManager.LockedTileEntities.Count > 0;
        }
    }
}