using System;
using System.Collections.Generic;
using BeyondStorage.Source.Data;

namespace BeyondStorage.Source.Storage;

/// <summary>
/// Tracks the state of smart storage operations (push/pull), including items transferred and containers affected.
/// </summary>
internal class StorageOperationState
{

    private readonly HashSet<object> _affectedStorages = [];
    private readonly HashSet<object> _affectedStacks = [];
    private readonly HashSet<int> _uniqueItems = [];

    /// <summary>
    /// Gets the name of the master storage involved in this operation.
    /// </summary>
    public string MasterStorageName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageOperationState"/> class.
    /// </summary>
    /// <param name="masterStorageName">The name of the master storage (cannot be null or empty)</param>
    /// <exception cref="ArgumentException">Thrown when masterStorageName is null or empty</exception>
    public StorageOperationState(string masterStorageName)
    {
        if (string.IsNullOrEmpty(masterStorageName))
        {
            throw new ArgumentException("Master storage name cannot be null or empty", nameof(masterStorageName));
        }

        MasterStorageName = masterStorageName;
    }

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
    internal void RecordTransfer(StorageTargetAdapter storage, ItemStack stack, int itemCount)
    {
        if (storage == null || stack == null || itemCount <= 0)
        {
            return;
        }

        var itemType = ItemX.ItemTypeOf(stack);
        if (ItemX.IsEmpty(stack))
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
        return $"Storage operation on '{MasterStorageName}' affected {StackCount} stack(s) across {StorageCount} storage(s), having {ItemTypeCount} item type(s) and {ItemCount} item(s)";
    }
}