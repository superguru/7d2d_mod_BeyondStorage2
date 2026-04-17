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
    /// Gets the type of transfer operation being performed.
    /// </summary>
    public SmartTransferOperation Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageOperationState"/> class.
    /// </summary>
    /// <param name="masterStorageName">The name of the master storage (cannot be null or empty)</param>
    /// <param name="operation">The type of transfer operation being performed</param>
    /// <exception cref="ArgumentException">Thrown when masterStorageName is null or empty</exception>
    public StorageOperationState(string masterStorageName, SmartTransferOperation operation)
    {
        if (string.IsNullOrEmpty(masterStorageName))
        {
            throw new ArgumentException("Master storage name cannot be null or empty", nameof(masterStorageName));
        }

        MasterStorageName = masterStorageName;
        Operation = operation;
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

    private bool ShouldRegisterStack(int initialStackSize, int currentStackSize, int maxStackSize)
    {
        switch (Operation)
        {
            case SmartTransferOperation.Push:
                /* | Stack Before | Stack After | */

                // | Full         | Partial     |
                var fromFullToPartial = ((initialStackSize == maxStackSize) && (currentStackSize < maxStackSize));

                // | Full         | Empty       |
                var fromFullToEmpty = ((initialStackSize == maxStackSize) && (currentStackSize == 0));

                // | Partial      | Empty       |
                var fromPartialToEmpty = ((initialStackSize < maxStackSize) && (currentStackSize == 0));

                return fromFullToPartial || fromFullToEmpty || fromPartialToEmpty;

            case SmartTransferOperation.TopUp:
                /* | Stack Before | Stack After | */

                // | Partial | Full |
                var fromPartialToFull = ((initialStackSize < maxStackSize) && (currentStackSize == maxStackSize));

                // | Partial | Partial |
                var fromPartialToPartial = ((initialStackSize < currentStackSize) && (currentStackSize < maxStackSize) && (initialStackSize > 0));

                return fromPartialToFull || fromPartialToPartial;
        }

        return false;
    }

    /// <summary>
    /// Records that items were affected by the operation
    /// </summary>
    internal void RecordTransfer(StorageTargetAdapter storage, ItemStack stack, int initialStackSize, int currentStackSize, int maxStackSize, int transferCount)
    {
        if (storage == null || stack == null || maxStackSize <= 0 || transferCount <= 0)
        {
            return;
        }

        var shouldRegisterStack = ShouldRegisterStack(initialStackSize, currentStackSize, maxStackSize);
        if (shouldRegisterStack)
        {
            _ = _affectedStacks.Add(stack);
        }

        var itemType = ItemX.ItemTypeOf(stack);
        if (itemType != UniqueItemTypes.EMPTY)
        {
            _ = _uniqueItems.Add(itemType);
        }

        if (transferCount > 0)
        {
            _ = _affectedStorages.Add(storage);
            ItemCount += transferCount;
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