using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Multiplayer;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Helper class to encapsulate tile entity processing state and reduce parameter passing.
/// Tracks statistics for chunk and tile entity processing including specific tile entity types.
/// </summary>
internal class TileEntityProcessingState
{
    public readonly StorageContext Context;
    public readonly ConfigSnapshot Config;
    public readonly WorldPlayerContext World;
    public readonly int PlayerId;
    public readonly bool HasLockedEntities;

    // Chunk processing stats
    public int ChunksProcessed = 0;
    public int NullChunks = 0;

    // Overall tile entity processing stats
    public int TileEntitiesProcessed = 0;

    // Specific tile entity type stats
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