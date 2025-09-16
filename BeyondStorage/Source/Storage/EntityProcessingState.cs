using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Game;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Helper class to encapsulate entity processing state and reduce parameter passing.
/// Tracks statistics for both vehicles and drones processed from the entity list.
/// </summary>
internal class EntityProcessingState
{
    public readonly StorageContext Context;
    public readonly ConfigSnapshot Config;
    public readonly WorldPlayerContext World;
    public readonly int PlayerId;

    // Overall entity processing stats
    public int EntitiesProcessed = 0;
    public int NullEntities = 0;

    // Vehicle-specific stats
    public int VehiclesProcessed = 0;
    public int ValidVehiclesFound = 0;

    // Drone-specific stats
    public int DronesProcessed = 0;
    public int ValidDronesFound = 0;

    public EntityProcessingState(StorageContext context)
    {
        Context = context;
        Config = context.Config;
        World = context.WorldPlayerContext;
        PlayerId = World.PlayerEntityId;
    }
}