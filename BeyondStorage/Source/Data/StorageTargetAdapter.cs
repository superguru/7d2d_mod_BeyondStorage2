using System;
using System.Collections.Generic;
using BeyondStorage.Infrastructure;

namespace BeyondStorage.Data;

internal class StorageTargetAdapter : IEquatable<StorageTargetAdapter>
{
    private readonly IStorageTarget _source;

    private readonly List<ItemStack> _emptySlots;

    private readonly Dictionary<int, List<ItemStack>> _filledSlots;
    private readonly Dictionary<int, List<ItemStack>> _partialSlots;

    public StorageTargetAdapter(IStorageTarget source, float distance, SlotMaps maps)
    {
        _source = source;
        Distance = distance;

        maps.GetSlotDataLists(out _filledSlots, out _partialSlots, out _emptySlots);
    }

    public float Distance { get; }

    public bool Equals(StorageTargetAdapter other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _source.Equals(other._source);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as StorageTargetAdapter);
    }

    public override int GetHashCode()
    {
        return _source?.GetHashCode() ?? 0;
    }

    private void ClassifySlot(ItemStack slot)
    {
        var itemType = ItemX.ItemTypeOf(slot);
        if (itemType == UniqueItemTypes.EMPTY || ItemX.IsEmpty(slot))
        {
            _emptySlots.Add(slot);
        }
        else if (ItemX.IsFull(slot))
        {
            RegisterSlot(_filledSlots, itemType, slot);
        }
        else
        {
            RegisterSlot(_partialSlots, itemType, slot);
        }
    }

    internal void ReclassifySlot(ItemStack slot)
    {
        const string d_MethodName = nameof(ReclassifySlot);

        if (slot == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Attempted to reclassify a null slot, ignoring");
            return;
        }

        var itemType = ItemX.ItemTypeOf(slot);
        if (itemType == UniqueItemTypes.EMPTY)
        {
            ModLogger.DebugLog($"{d_MethodName}: Slot has empty item type, cannot determine source list");
            return;
        }

        // Check filled slots first
        if (_filledSlots.TryGetValue(itemType, out var filledList))
        {
            var slotIndex = filledList.LastIndexOfReference(slot);
            if (slotIndex >= 0)
            {
                ReclassifySlot(filledList, slot, slotIndex);
                return;
            }
        }

        // Check partial slots independently
        if (_partialSlots.TryGetValue(itemType, out var partialList))
        {
            var slotIndex = partialList.LastIndexOfReference(slot);
            if (slotIndex >= 0)
            {
                ReclassifySlot(partialList, slot, slotIndex);
                return;
            }
        }

        // Check empty slots from the tail — the slot returned by GetNextEmptyStackFor is always at Count-1
        var emptySlotIndex = _emptySlots.LastIndexOfReference(slot);
        if (emptySlotIndex >= 0)
        {
            _emptySlots.RemoveAt(emptySlotIndex);
            ClassifySlot(slot);
            return;
        }

        // Only log if not found anywhere
        ModLogger.DebugLog($"{d_MethodName}: Slot not found in any list for item type {ItemX.NameOf(itemType)}");
    }

    private void ReclassifySlot(IList<ItemStack> currentList, ItemStack slot, int slotIndex)
    {
        currentList.RemoveAt(slotIndex);
        ClassifySlot(slot);
    }

    private void RegisterSlot(Dictionary<int, List<ItemStack>> registry, int itemType, ItemStack slot)
    {
        if (!registry.TryGetValue(itemType, out var slots))
        {
            slots = CollectionFactory.CreateItemStackList();
            registry[itemType] = slots;
        }

        slots.Add(slot);
    }

    internal ItemStack GetNextEmptyStackFor(int itemType)
    {
        if (_emptySlots.Count == 0)
        {
            return null;
        }

        // Only eligible to fill an empty slot if the storage already holds this item type
        if ((_filledSlots.TryGetValue(itemType, out var filledList) && filledList.Count > 0)
            || (_partialSlots.TryGetValue(itemType, out var partialList) && partialList.Count > 0))
        {
            // This will be the "first available" slot in the storage, starting from top to bottom, left to right
            return _emptySlots[_emptySlots.Count - 1];
        }

        return null;
    }

    internal ItemStack GetNextFilledStackFor(int itemType)
    {
        if (_filledSlots.TryGetValue(itemType, out var slots))
        {
            var count = slots.Count;
            if (count > 0)
            {
                return slots[count - 1];
            }
        }

        return null;
    }

    internal ItemStack GetNextPartialStackFor(int itemType)
    {
        if (_partialSlots.TryGetValue(itemType, out var slots))
        {
            var count = slots.Count;
            if (count > 0)
            {
                return slots[count - 1];
            }
        }

        return null;
    }

    internal ItemStack GetNextPopulatedStackFor(int itemType)
    {
        // Prefer partial stacks over filled stacks:
        // - partial stacks are valid for Pull (as a source) and Push (as a destination)
        // - filled stacks are only valid for Pull (as a source), like for loadout top ups
        // ==>> so either empty the partial stacks first, or fill the partial stacks first, depending on the operation type

        var nextPartial = GetNextPartialStackFor(itemType);
        if (nextPartial != null)
        {
            return nextPartial;
        }

        var nextFilled = GetNextFilledStackFor(itemType);
        if (nextFilled != null)
        {
            return nextFilled;
        }

        return null;
    }

    internal string GetName()
    {
        if (_source == null)
        {
            return "null source in target has no name";
        }

        return _source.GetName();
    }

    public void MarkModified()
    {
        _source?.MarkModified();
    }

    internal bool HasSameSource(IStorageSource other)
    {
        var result = _source.Equals(other);

        return result;
    }
}
