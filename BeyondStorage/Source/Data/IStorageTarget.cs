namespace BeyondStorage.Data;

/// <summary>
/// Non-generic contract for <see cref="StorageSourceAdapter{T}"/> as consumed by <see cref="StorageTargetAdapter"/>.
/// Allows mixed storage source types to be stored and used uniformly as push/pull targets.
/// </summary>
internal interface IStorageTarget : IStorageSource
{
    /// <summary>
    /// Returns raw slot data in a single read. The data store uses <see cref="SourceSlotData.AllSlots"/>
    /// and <see cref="SourceSlotData.LockedSlots"/> to classify slots into empty, partial, full,
    /// consumable, and pushable categories. No pre-filtering is applied by the source.
    /// </summary>
    SourceSlotData GetSlotData();

    string GetName();
}