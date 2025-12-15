using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Configuration;

/// <summary>
/// Configuration class for Beyond Storage mod settings
/// </summary>
public class BsConfig
{
    // ========== Versioning =========
    /// <summary>
    /// Config schema version - matches ModInfo.Version when config was created/migrated
    /// </summary>
    public string version = ConfigVersioning.CurrentVersion;

    // ========== Source selection / eligibility =========
    /// <summary>
    /// How far to pull from (-1 is infinite range, only limited by chunks loaded)
    /// </summary>
    public float range = -1.0f;

    /// <summary>
    /// If set to true it will try and pull items from nearby drones
    /// </summary>
    public bool pullFromDrones = true;

    /// <summary>
    /// If set to true it will try and pull items from nearby dew collectors
    /// </summary>
    public bool pullFromDewCollectors = true;

    /// <summary>
    /// If set to true it will try and pull items from nearby workstation output stacks
    /// </summary>
    public bool pullFromWorkstationOutputs = true;

    /// <summary>
    /// If set to true it will ignore tile entities that aren't Storage Containers (crates)
    /// otherwise will check all lootable containers placed by player(s)
    /// </summary>
    public bool pullFromPlayerCraftedNonCrates = false;

    /// <summary>
    /// If set to true it will try and pull items from nearby vehicle storages
    /// </summary>
    public bool pullFromVehicleStorage = true;

    // ========== Multiplayer =========
    /// <summary>
    /// If set true on a server it will force all clients to use server settings for Beyond Storage
    /// </summary>
    public bool serverSyncConfig = true;

    // ========== Housekeeping =========
    /// <summary>
    /// If set true additional logging will be printed to logs/console
    /// </summary>
    public bool isDebug = false;

    /// <summary>
    /// If set true will log settings access (only works if isDebug is true and DEBUG is defined)
    /// </summary>
    public bool isDebugLogSettingsAccess = false;

    /// <summary>
    /// Optional metadata description field for configuration documentation purposes
    /// </summary>
    public string metaDescription = string.Empty;
}