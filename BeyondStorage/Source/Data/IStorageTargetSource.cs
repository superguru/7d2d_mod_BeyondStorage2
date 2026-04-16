using BeyondStorage.Source.Storage;

namespace BeyondStorage.Source.Data;

/// <summary>
/// Non-generic contract for <see cref="StorageSourceAdapter{T}"/> as consumed by <see cref="StorageTargetAdapter"/>.
/// Allows mixed storage source types to be stored and used uniformly as push/pull targets.
/// </summary>
internal interface IStorageTargetSource : IStorageSource
{
    //============================================================================
    //TODO: Remove these once per slot classification is implemented
    //============================================================================
    ItemStack[] GetPushableItemStacks();
    ItemStack[] GetAllSlotItemsStacks();
    //============================================================================

    bool IsItemScopeMatch(ItemStack stack, ItemScope scope);

    string GetName();
}