using System;

namespace BeyondStorage.Scripts.Data;

public interface IStorageSource : IEquatable<IStorageSource>
{
    ItemStack[] GetItemStacks();
    Type GetSourceType();
    void MarkModified();
}
