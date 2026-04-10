using System.Collections.Generic;
using BeyondStorage.Scripts.Data;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Tracks the state of a smart push operation, including items transferred and targets affected.
/// </summary>
internal class PushProcessingState
{
    private readonly HashSet<object> _affectedTargets = [];

    /// <summary>
    /// Gets the number of distinct target containers that received items during this push operation.
    /// </summary>
    public int TargetCount => _affectedTargets.Count;

    /// <summary>
    /// Gets the total number of items moved from the source during this push operation.
    /// </summary>
    public int TotalItemsMoved { get; set; } = 0;

    /// <summary>
    /// Records that items were transferred to a specific target.
    /// </summary>
    /// <param name="target">The target container that received items</param>
    /// <param name="itemCount">The number of items transferred to this target</param>
    internal void RecordTransfer<T>(StorageTargetAdapter<T> target, int itemCount) where T : class
    {
        if (target == null || itemCount <= 0)
        {
            return;
        }

        TotalItemsMoved += itemCount;
        _ = _affectedTargets.Add(target);
    }

    /// <summary>
    /// Resets the state to initial values.
    /// Useful for reusing the same state object across multiple operations.
    /// </summary>
    internal void Reset()
    {
        TotalItemsMoved = 0;
        _affectedTargets.Clear();
    }

    /// <summary>
    /// Gets a summary of the push operation for logging or display purposes.
    /// </summary>
    /// <returns>A formatted string describing the operation results</returns>
    public override string ToString()
    {
        return $"Moved {TotalItemsMoved} items to {TargetCount} target(s)";
    }
}