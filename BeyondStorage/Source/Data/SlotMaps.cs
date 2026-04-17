using System;
using System.Collections.Generic;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.Data;

/// <summary>
/// Pre-classified slot maps built once at registration and cloned per operation.
/// Separates the slot classification work from StorageTargetAdapter construction.
/// </summary>
internal sealed class SlotMaps
{
    internal const int DEFAULT_ITEMTYPES_CAPACITY = 1024;

    private int _itemListCapacity = CollectionFactory.DEFAULT_ITEMSTACK_LIST_CAPACITY;

    private readonly Dictionary<int, List<ItemStack>> _filled;
    private readonly Dictionary<int, List<ItemStack>> _partial;
    private readonly List<ItemStack> _empty;

    internal SlotMaps() : this(DEFAULT_ITEMTYPES_CAPACITY) { }

    internal SlotMaps(int itemListCapacity)
    {
        _itemListCapacity = Math.Max(itemListCapacity, CollectionFactory.DEFAULT_ITEMSTACK_LIST_CAPACITY);

#pragma warning disable IDE0028 // Simplify collection initialization
        // This method of initialization directly allocates the correct capacity, which is a speed optimisation strategy
        _filled = new Dictionary<int, List<ItemStack>>(_itemListCapacity);
        _partial = new Dictionary<int, List<ItemStack>>(_itemListCapacity);
        _empty = new List<ItemStack>(_itemListCapacity);
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    /// <summary>
    /// Creates a per-operation copy. The cloned maps share ItemStack references
    /// but have independent list and dictionary structures, allowing safe mutation
    /// via ReclassifySlot without affecting the registration-time originals.
    /// </summary>
    internal SlotMaps Clone()
    {
        var clone = new SlotMaps(_itemListCapacity);

        foreach (var kvp in _filled)
        {
            // This method of initialization directly allocates the correct capacity, which is a speed optimisation strategy
            var filledList = new List<ItemStack>(kvp.Value);
            filledList.Reverse();
            clone._filled[kvp.Key] = filledList;
        }

        foreach (var kvp in _partial)
        {
            // This method of initialization directly allocates the correct capacity, which is a speed optimisation strategy
            var partialList = new List<ItemStack>(kvp.Value);
            partialList.Reverse();
            clone._partial[kvp.Key] = partialList;
        }

        clone._empty.AddRange(_empty);
        clone._empty.Reverse();

        return clone;
    }

    internal void GetSlotDataLists(out Dictionary<int, List<ItemStack>> filledSlots, out Dictionary<int, List<ItemStack>> partialSlots, out List<ItemStack> emptySlots)
    {
        filledSlots = _filled;
        partialSlots = _partial;
        emptySlots = _empty;
    }

    /// <summary>
    /// Classifies and adds a single slot into the appropriate map.
    /// </summary>
    internal void RegisterSlot(ItemStack slot)
    {
        var itemType = ItemX.ItemTypeOf(slot);

        if (itemType == UniqueItemTypes.EMPTY || ItemX.IsEmpty(slot))
        {
            _empty.Add(slot);
            return;
        }

        var registry = ItemX.IsFull(slot) ? _filled : _partial;
        if (!registry.TryGetValue(itemType, out var slots))
        {
            slots = CollectionFactory.CreateItemStackList(_itemListCapacity);
            registry[itemType] = slots;
        }

        slots.Add(slot);

        if (slots.Count > _itemListCapacity)
        {
#if DEBUG
            ModLogger.DebugLog($"Slot list for item type {ItemX.Info(slot)} exceeded capacity of {_itemListCapacity}.");
#endif
            _itemListCapacity *= 2;
        }
    }
}