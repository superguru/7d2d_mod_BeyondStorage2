namespace BeyondStorage.Source.Storage;

/// <summary>
/// Specifies how to filter storage items based on slot lock status.
/// </summary>
public enum StorageFilter
{
    /// <summary>Returns all items regardless of lock status</summary>
    AllItems,
    /// <summary>Returns only items from unlocked slots</summary>
    UnlockedOnly,
    /// <summary>Returns only items from locked slots</summary>
    LockedOnly
}