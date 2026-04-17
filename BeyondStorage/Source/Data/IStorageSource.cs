using System;

namespace BeyondStorage.Source.Data;

public interface IStorageSource : IEquatable<IStorageSource>
{
    /// <summary>
    /// Returns all slots without any filtering, including empty slots.
    /// Classification of consumable, pushable, and empty stacks is the responsibility
    /// of <see cref="StorageSourceItemDataStore"/>, not the source.
    /// </summary>
    ItemStack[] GetAllItemStacks();
    Type GetSourceType();
    void MarkModified();
}
