using System.Collections.Generic;
using BeyondStorage.Source.Data;

namespace BeyondStorage.Source.Storage;

/// <summary>
/// Tracks the state of smart storage operations (push/pull), including items transferred and containers affected.
/// </summary>
internal class StorageOperationState
{
    private static readonly ItemStackReferenceComparer s_itemStackComparer = ItemStackReferenceComparer.Instance;

    private readonly HashSet<object> _affectedStorages = [];
    private readonly HashSet<ItemStack> _affectedStacks = new(s_itemStackComparer);
    private readonly HashSet<int> _uniqueItems = [];

    /// <summary>
    /// Gets the number of distinct storages affected during this operation.
    /// </summary>
    public int StorageCount => _affectedStorages.Count;

    /// <summary>
    /// Gets the number of distinct item stacks affected.
    /// </summary>
    public int StackCount => _affectedStacks.Count;

    /// <summary>
    /// Gets the number of unique item types moved.
    /// </summary>
    public int ItemTypeCount => _uniqueItems.Count;

    /// <summary>
    /// Gets the total number of items moved.
    /// </summary>
    public int ItemCount { get; set; } = 0;

    /// <summary>
    /// Records that items were affected by the operation
    /// </summary>
    internal void RecordTransfer<T>(StorageTargetAdapter<T> storage, ItemStack stack, int itemCount) where T : class
    {
        if (storage == null || stack == null || itemCount <= 0)
        {
            return;
        }

        var itemType = ItemX.ItemTypeOf(stack);
        if (itemType != UniqueItemTypes.EMPTY)
        {
            _ = _uniqueItems.Add(itemType);
            _ = _affectedStorages.Add(storage);
            _ = _affectedStacks.Add(stack);

            ItemCount += itemCount;
        }
    }

    internal void Reset()
    {
        _affectedStorages.Clear();
        _affectedStacks.Clear();
        _uniqueItems.Clear();

        ItemCount = 0;
    }

    public override string ToString()
    {
        return $"Sorage operation affected {StackCount} stack(s) across {StorageCount} storage(s), having {ItemTypeCount} item type(s) and {ItemCount} item(s)";
    }
}