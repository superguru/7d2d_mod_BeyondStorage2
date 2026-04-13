using System.Collections.Generic;

namespace BeyondStorage.Source.Data;

internal class StorageTargetAdapter<T> where T : class
{
    private readonly StorageSourceAdapter<T> _source;

    private readonly List<ItemStack> _emptySlots = [];

    private readonly Dictionary<int, List<ItemStack>> _filledSlots = [];
    private readonly Dictionary<int, List<ItemStack>> _partialSlots = [];

    public StorageTargetAdapter(StorageSourceAdapter<T> source, float distance)
    {
        _source = source;
        Distance = distance;

        BuildDescriptorMaps();
    }

    public float Distance { get; }

    private void BuildDescriptorMaps()
    {
        Clear();

        var items = _source.GetAllSlotItemsStacks();
        for (int i = 0; i < items.Length; i++)
        {
            ClassifySlot(items[i], orderedFirst: false);
        }
    }

    private void ClassifySlot(ItemStack slot, bool orderedFirst = false)
    {
        var itemType = ItemX.ItemTypeOf(slot);
        if (itemType == UniqueItemTypes.EMPTY || slot?.count <= 0)
        {
            _emptySlots.Add(slot);
        }
        else if (ItemX.IsFull(slot))
        {
            RegisterSlot(_filledSlots, itemType, slot, orderedFirst);
        }
        else
        {
            RegisterSlot(_partialSlots, itemType, slot, orderedFirst);
        }
    }

    internal void ReclassifySlot(ItemStack slot)
    {
        if (slot == null)
        {
            return;
        }

        var itemType = ItemX.ItemTypeOf(slot);
        if (itemType == UniqueItemTypes.EMPTY)
        {
            return;
        }

        if (_filledSlots.TryGetValue(itemType, out var filledList))
        {
            var slotIndex = filledList.IndexOfReference(slot);
            if (slotIndex >= 0)
            {
                ReclassifySlot(filledList, slot, slotIndex);
            }
        }
        else if (_partialSlots.TryGetValue(itemType, out var partialList))
        {
            var slotIndex = partialList.IndexOfReference(slot);
            if (slotIndex >= 0)
            {
                ReclassifySlot(partialList, slot, slotIndex);
            }
        }
        else
        {
            var slotIndex = _emptySlots.IndexOfReference(slot);
            if (slotIndex >= 0)
            {
                _emptySlots.Remove(slot);
                ClassifySlot(slot, orderedFirst: true);
            }
        }
    }

    internal void ReclassifySlot(IList<ItemStack> currentList, ItemStack slot, int slotIndex)
    {
        currentList.RemoveAt(slotIndex);
        ClassifySlot(slot, orderedFirst: true);
    }

    private void RegisterSlot(Dictionary<int, List<ItemStack>> registry, int itemType, ItemStack slot, bool orderedFirst = false)
    {
        if (!registry.TryGetValue(itemType, out var slots))
        {
            slots = CollectionFactory.CreateItemStackList();
            registry[itemType] = slots;
        }

        if (orderedFirst)
        {
            slots.Insert(0, slot);
        }
        else
        {
            slots.Add(slot);
        }
    }

    private void Clear()
    {
        _filledSlots.Clear();
        _emptySlots.Clear();
        _partialSlots.Clear();
    }

    internal IList<ItemStack> GetEmptySlotsFor(ItemStack sourceSlot)
    {
        var itemType = ItemX.ItemTypeOf(sourceSlot);
        if (itemType == UniqueItemTypes.EMPTY)
        {
            return [];
        }

        if (_filledSlots.ContainsKey(itemType) || _partialSlots.ContainsKey(itemType))
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

    internal string GetTargetName()
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

    internal bool HasSameSource<S>(StorageSourceAdapter<S> other) where S : class
    {
        var result = _source.Equals(other);

        return result;
    }
}
