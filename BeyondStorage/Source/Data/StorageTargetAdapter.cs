using System.Collections.Generic;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.Data;

internal class StorageTargetAdapter
{
    private readonly IStorageTargetSource _source;

    private readonly List<ItemStack> _emptySlots;

    private readonly Dictionary<int, List<ItemStack>> _filledSlots;
    private readonly Dictionary<int, List<ItemStack>> _partialSlots;

    public StorageTargetAdapter(IStorageTargetSource source, float distance, SlotMaps maps)
    {
        _source = source;
        Distance = distance;

        _filledSlots = maps._filled;
        _partialSlots = maps._partial;
        _emptySlots = maps._empty;
    }

    public float Distance { get; }

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
            var slotIndex = filledList.IndexOfReference(slot);
            if (slotIndex >= 0)
            {
                ReclassifySlot(filledList, slot, slotIndex);
                return;
            }
        }

        // Check partial slots independently (not else if!)
        if (_partialSlots.TryGetValue(itemType, out var partialList))
        {
            var slotIndex = partialList.IndexOfReference(slot);
            if (slotIndex >= 0)
            {
                ReclassifySlot(partialList, slot, slotIndex);
                return;
            }
        }

        // Check empty slots
        var emptySlotIndex = _emptySlots.IndexOfReference(slot);
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

        slots.Insert(0, slot);
    }

    internal IList<ItemStack> GetEmptySlotsFor(ItemStack sourceSlot)
    {
        const string d_MethodName = nameof(GetEmptySlotsFor);

        var itemType = ItemX.ItemTypeOf(sourceSlot);
        if (itemType == UniqueItemTypes.EMPTY)
        {
            ModLogger.DebugLog($"{d_MethodName}: Source slot has empty item type, returning empty list");
            return [];
        }

        bool hasFilledSlots = _filledSlots.TryGetValue(itemType, out var filledList) && filledList.Count > 0;
        bool hasPartialSlots = _partialSlots.TryGetValue(itemType, out var partialList) && partialList.Count > 0;

        if (hasFilledSlots || hasPartialSlots)
        {
            return _emptySlots;
        }

        return [];
    }

    internal IList<ItemStack> GetFilledSlotsFor(ItemStack stack)
    {
        var itemType = ItemX.ItemTypeOf(stack);
        if (_filledSlots.TryGetValue(itemType, out var slots))
        {
            return slots;
        }

        return [];
    }

    internal IList<ItemStack> GetPartialSlotsFor(ItemStack stack)
    {
        var itemType = ItemX.ItemTypeOf(stack);
        if (_partialSlots.TryGetValue(itemType, out var slots))
        {
            return slots;
        }

        return [];
    }

    internal IList<ItemStack> GetPopulatedSlotsFor(ItemStack sourceSlot)
    {
        var filled = GetFilledSlotsFor(sourceSlot);
        var partial = GetPartialSlotsFor(sourceSlot);

        var result = CollectionFactory.CreateItemStackList(filled.Count + partial.Count);
        result.AddRange(filled);
        result.AddRange(partial);

        return result;
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
