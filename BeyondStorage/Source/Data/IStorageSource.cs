using System;

namespace BeyondStorage.Source.Data;

public interface IStorageSource : IEquatable<IStorageSource>
{
    ItemStack[] GetPullableItemStacks();
    Type GetSourceType();
    void MarkModified();
}
