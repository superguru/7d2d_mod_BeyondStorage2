using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyondStorage.Scripts.Data;

internal class StorageTargetAdapter<T> where T : class
{
    private readonly StorageSourceAdapter<T> _source;
    private readonly HashSet<ItemStack> _filledSlots = [];
    private readonly HashSet<ItemStack> _emptySlots = [];
    private readonly Dictionary<int, List<ItemStack>> _partialSlots = [];
    private readonly HashSet<ItemStack> _invalidSlots = [];

    public StorageTargetAdapter(StorageSourceAdapter<T> source, float distance)
    {
        _source = source;
        Distance = distance;
    }

    public float Distance { get; }

    public bool Dirty { get; set; } = true;

    private void BuildDescriptorMaps()
    {
        //TODO: We need to build maps about metadata. This is currently not efficient at all.
        if (!Dirty)
        {
            return;
        }

        Clear();

        var items = _source.GetAllSlotItemsStacks();
        for (int i = 0; i < items.Length; i++)
        {
            var slot = items[i];

            if (!ItemX.IsValidItemStack(slot))
            {
                _invalidSlots.Add(slot);
                continue;
            }

            if (ItemX.IsFull(slot))
            {
                _filledSlots.Add(slot);
                continue;
            }

            if (ItemX.IsEmpty(slot))
            {
                _emptySlots.Add(slot);
                continue;
            }

            var itemType = ItemX.ItemTypeOf(slot);
            if (!_partialSlots.TryGetValue(itemType, out var slots)) 
            {
                slots = CollectionFactory.CreateItemStackList();
                _partialSlots[itemType] = slots;
            }

            slots.Add(slot);
        }

        Dirty = false;
    }

    private void Clear()
    {
        _filledSlots.Clear();
        _emptySlots.Clear();
        _invalidSlots.Clear();
        _partialSlots.Clear();
    }

    internal IList<ItemStack> GetPartialSlotsFor(ItemStack stack)
    {
        BuildDescriptorMaps();

        var itemType = ItemX.ItemTypeOf(stack);
        if (_partialSlots.TryGetValue(itemType, out var slots))
        {
            return slots;
        }

        return [];
    }

    internal string GetTargetName()
    {
        if (_source == null)
        {
            return "null source has no name";
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
}
