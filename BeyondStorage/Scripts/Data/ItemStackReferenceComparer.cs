using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BeyondStorage.Scripts.Data;

/// <summary>
/// Reference-based equality comparer for ItemStack objects.
/// Uses reference equality and RuntimeHelpers.GetHashCode for consistent hashing.
/// This ensures we track the exact ItemStack instances from storage sources,
/// even when their values (like count) are modified during removal operations.
/// </summary>
internal sealed class ItemStackReferenceComparer : IEqualityComparer<ItemStack>
{
    public static readonly ItemStackReferenceComparer Instance = new();

    private ItemStackReferenceComparer() { }

    public bool Equals(ItemStack x, ItemStack y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(ItemStack obj)
    {
        return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }
}