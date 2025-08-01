using System;

namespace BeyondStorage.Scripts.Data;

internal interface IStorageSource : IEquatable<IStorageSource>
{
    void MarkModified();

    ItemStack[] GetItems();
}
