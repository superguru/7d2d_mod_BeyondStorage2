using System;

namespace BeyondStorage.Scripts.Data;

public interface IStorageSource : IEquatable<IStorageSource>
{
    void MarkModified();

    ItemStack[] GetItems();
}
