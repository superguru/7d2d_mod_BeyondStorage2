using System.Collections.Generic;
using BeyondStorage.Source.Data;

namespace BeyondStorage.Source.Storage;

/// <summary>
/// Tracks the state of a smart push operation, including items transferred and targets affected.
/// </summary>
internal class PushProcessingState
{
    private readonly HashSet<object> _affectedTargets = [];
    private readonly HashSet<ItemStack> _affectedSourceStacks = [];
    private readonly HashSet<int> _uniqueItems = [];

    /// <summary>
    /// Gets the number of distinct target containers that received items during this push operation.
    /// </summary>
    public int TargetCount => _affectedTargets.Count;

    /// <summary>
    /// Gets the number of distinct source item stacks that were transferred from (partially or fully).
    /// </summary>
    public int TotalStackCount => _affectedSourceStacks.Count;

    /// <summary>
    /// Gets the number of unique item types that were moved during this push operation.
    /// </summary>
    public int UniqueItemCount => _uniqueItems.Count;

    /// <summary>
    /// Gets the total number of items moved from the source during this push operation.
    /// </summary>
    public int TotalItemCount { get; set; } = 0;

    /// <summary>
    /// Records that items were transferred from a source stack to a target.
    /// </summary>
    /// <param name="sourceStack">The source item stack that items were transferred from</param>
    /// <param name="target">The target container that received items</param>
    /// <param name="itemCount">The number of items transferred</param>
    internal void RecordTransfer<T>(ItemStack sourceStack, StorageTargetAdapter<T> target, int itemCount) where T : class
    {
        if (sourceStack == null || target == null || itemCount <= 0)
        {
            return;
        }

        TotalItemCount += itemCount;
        _ = _affectedTargets.Add(target);
        _ = _affectedSourceStacks.Add(sourceStack);
        var itemType = ItemX.ItemTypeOf(sourceStack);
        if (itemType != UniqueItemTypes.EMPTY)
        {
            _ = _uniqueItems.Add(itemType);
        }
    }

    /// <summary>
    /// Resets the state to initial values.
    /// Useful for reusing the same state object across multiple operations.
    /// </summary>
    internal void Reset()
    {
        TotalItemCount = 0;
        _affectedTargets.Clear();
        _affectedSourceStacks.Clear();
        _uniqueItems.Clear();
    }

    /// <summary>
    /// Gets a summary of the push operation for logging or display purposes.
    /// </summary>
    /// <returns>A formatted string describing the operation results</returns>
    public override string ToString()
    {
        return $"Moved {TotalStackCount} stack(s) to {TargetCount} target(s), having {UniqueItemCount} item type(s) and {TotalItemCount} item(s)";
    }
}