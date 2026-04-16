namespace BeyondStorage.Source.Data;

/// <summary>
/// Non-generic contract for <see cref="StorageSourceAdapter{T}"/> as consumed by <see cref="StorageTargetAdapter"/>.
/// Allows mixed storage source types to be stored and used uniformly as push/pull targets.
/// </summary>
internal interface IStorageTargetSource : IStorageSource
{
    ItemStack[] GetPushableItemStacks();
    ItemStack[] GetAllSlotItemsStacks();

    string GetName();
}