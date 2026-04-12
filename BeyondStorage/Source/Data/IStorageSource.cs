using System;

namespace BeyondStorage.Source.Data;

public interface IStorageSource : IEquatable<IStorageSource>
{
    ItemStack[] GetConsumableItemStacks();
    Type GetSourceType();
    void MarkModified();
}
