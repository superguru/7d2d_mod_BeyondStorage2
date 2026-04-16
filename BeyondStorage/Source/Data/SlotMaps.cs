using System.Collections.Generic;

namespace BeyondStorage.Source.Data;

/// <summary>
/// Pre-classified slot maps built once at registration and cloned per operation.
/// Separates the slot classification work from StorageTargetAdapter construction.
/// </summary>
internal sealed class SlotMaps
{
    internal const int DEFAULT_CAPACITY = CollectionFactory.DEFAULT_ITEMSTACK_LIST_CAPACITY;

    internal readonly Dictionary<int, List<ItemStack>> _filled;
    internal readonly Dictionary<int, List<ItemStack>> _partial;
    internal readonly List<ItemStack> _empty;

    internal SlotMaps() : this(DEFAULT_CAPACITY) { }

    internal SlotMaps(int sameCapacity) : this(sameCapacity, sameCapacity, sameCapacity) { }

    internal SlotMaps(int filledCapacity, int partialCapacity, int emptyCapacity)
    {
        if (filledCapacity <= 0)
        {
            filledCapacity = DEFAULT_CAPACITY;
        }

        if (partialCapacity <= 0)
        {
            partialCapacity = DEFAULT_CAPACITY;
        }

        if (emptyCapacity <= 0)
        {
            emptyCapacity = DEFAULT_CAPACITY;
        }

#pragma warning disable IDE0028 // Simplify collection initialization
        // This method of initialization directly allocates the correct capacity, which is a speed optimisation strategy
        _filled = new Dictionary<int, List<ItemStack>>(filledCapacity);
        _partial = new Dictionary<int, List<ItemStack>>(partialCapacity);
        _empty = new List<ItemStack>(emptyCapacity);
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    /// <summary>
    /// Creates a per-operation copy. The cloned maps share ItemStack references
    /// but have independent list and dictionary structures, allowing safe mutation
    /// via ReclassifySlot without affecting the registration-time originals.
    /// </summary>
    internal SlotMaps Clone()
    {
        var clone = new SlotMaps(_filled.Count, _partial.Count, _empty.Count);

        foreach (var kvp in _filled)
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            // This method of initialization directly allocates the correct capacity, which is a speed optimisation strategy
            var filledList = new List<ItemStack>(kvp.Value);
#pragma warning restore IDE0028 // Simplify collection initialization
            filledList.Reverse();
            clone._filled[kvp.Key] = filledList;
        }

        foreach (var kvp in _partial)
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            // This method of initialization directly allocates the correct capacity, which is a speed optimisation strategy
            var partialList = new List<ItemStack>(kvp.Value);
#pragma warning restore IDE0028 // Simplify collection initialization
            partialList.Reverse();
            clone._partial[kvp.Key] = partialList;
        }

        clone._empty.AddRange(_empty);
        clone._empty.Reverse();

        return clone;
    }
}