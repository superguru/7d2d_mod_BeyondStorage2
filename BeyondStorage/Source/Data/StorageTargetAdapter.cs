using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyondStorage.Scripts.Data;

internal class StorageTargetAdapter<T> where T : class
{
    private readonly StorageSourceAdapter<T> _source;

    private readonly HashSet<ItemStack> _invalidSlots = [];
    private readonly Dictionary<int, List<ItemStack>> _filledSlots = [];
    private readonly Dictionary<int, List<ItemStack>> _emptySlots = [];
    private readonly Dictionary<int, List<ItemStack>> _partialSlots = [];

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

            var itemType = ItemX.ItemTypeOf(slot);

            if (ItemX.IsFull(slot))
            {
                RegisterSlot(_filledSlots, itemType, slot);
                continue;
            }

            if (ItemX.IsEmpty(slot))
            {
                RegisterSlot(_emptySlots, itemType, slot);
                continue;
            }

            RegisterSlot(_partialSlots, itemType, slot);
        }

        Dirty = false;
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

    private void Clear()
    {
        _invalidSlots.Clear();
        _filledSlots.Clear();
        _emptySlots.Clear();
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

    public void MarkModified()
    {
        if (_source != null)
        {
            _source.MarkModified();
        }

        Dirty = true;
    }
}
