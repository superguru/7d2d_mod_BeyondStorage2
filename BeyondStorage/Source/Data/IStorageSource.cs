using System;

namespace BeyondStorage.Scripts.Data;

public interface IStorageSource : IEquatable<IStorageSource>
{
    ItemStack[] GetPullableItemStacks();
    Type GetSourceType();
    void MarkModified();
}
