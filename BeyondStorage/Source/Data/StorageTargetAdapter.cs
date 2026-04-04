using System.Collections.Generic;

namespace BeyondStorage.Scripts.Data;

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
            ClassifySlot(items[i]);
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

    internal IList<ItemStack> GetPartialSlotsFor(ItemStack stack)
    {
        var itemType = ItemX.ItemTypeOf(stack);
        if (_partialSlots.TryGetValue(itemType, out var slots))
        {
            return slots;
        }

        return [];
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

    internal string GetTargetName()
    {
        if (_source == null)
        {
            return "null source in target has no name";
        }

        return _source.GetName();
    }

    internal ItemStack[] GetAllSlotItemStacks()
    {
        if (_source == null)
        {
            return [];
        }

        return _source.GetAllSlotItemsStacks();
    }

    public void MarkModified()
    {
        _source?.MarkModified();
    }
}
