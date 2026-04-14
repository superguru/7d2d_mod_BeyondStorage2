using System.Collections.Generic;

namespace BeyondStorage.Source.Data;

/// <summary>
/// Pre-classified slot maps built once at registration and cloned per operation.
/// Separates the slot classification work from StorageTargetAdapter construction.
/// </summary>
internal sealed class SlotMaps
{
    internal readonly Dictionary<int, List<ItemStack>> _filled = [];
    internal readonly Dictionary<int, List<ItemStack>> _partial = [];
    internal readonly List<ItemStack> _empty = new(CollectionFactory.DEFAULT_ITEMSTACK_LIST_CAPACITY);

    /// <summary>
    /// Creates a per-operation copy. The cloned maps share ItemStack references
    /// but have independent list and dictionary structures, allowing safe mutation
    /// via ReclassifySlot without affecting the registration-time originals.
    /// </summary>
    internal SlotMaps Clone()
    {
        var clone = new SlotMaps();

        foreach (var kvp in _filled)
        {
            clone._filled[kvp.Key] = [.. kvp.Value];
        }

        foreach (var kvp in _partial)
        {
            clone._partial[kvp.Key] = [.. kvp.Value];
        }

        clone._empty.AddRange(_empty);

        return clone;
    }
}