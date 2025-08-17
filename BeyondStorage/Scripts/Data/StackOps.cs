using System;
using System.Linq;

namespace BeyondStorage.Scripts.Data;

/// <summary>
/// Enum for stack operation types used for UI refresh triggers
/// </summary>
public enum StackOps
{
    ItemStack_DropMerge_Operation,
    ItemStack_Drop_Operation,
    ItemStack_DropSingleItem_Operation,
    ItemStack_Pickup_Operation,
    ItemStack_Pickup_Half_Stack_Operation,
    ItemStack_Shift_Operation
}

/// <summary>
/// Utilities for stack operations
/// </summary>
public static class StackOperation
{
    /// <summary>
    /// Determines whether the specified operation is a known stack operation.
    /// </summary>
    /// <param name="operation">The operation to validate</param>
    /// <returns>True if the operation is a defined enum value; otherwise, false</returns>
    public static bool IsValidOperation(StackOps operation)
    {
        return Enum.IsDefined(typeof(StackOps), operation);
    }

    /// <summary>
    /// Determines whether the specified operation name is a known stack operation constant.
    /// </summary>
    /// <param name="operationName">The operation name to validate</param>
    /// <returns>True if the operation name matches one of the defined enum values; otherwise, false</returns>
    public static bool IsValidOperation(string operationName)
    {
        return Enum.TryParse<StackOps>(operationName, out _);
    }

    /// <summary>
    /// Gets all valid operation enum values.
    /// </summary>
    /// <returns>Array of all StackOperation enum values</returns>
    public static StackOps[] GetAllOperations()
    {
        return (StackOps[])Enum.GetValues(typeof(StackOps));
    }

    /// <summary>
    /// Gets all valid operation string representations.
    /// </summary>
    /// <returns>Array of all operation string representations</returns>
    public static string[] GetAllOperationStrings()
    {
        return GetAllOperations().Select(op => op.ToString()).ToArray();
    }
}

/// <summary>
/// Simple enum for tracking swap operation types
/// </summary>
public enum SwapAction
{
    NoOperation,
    PickupFromSource,
    PlaceInEmptyTarget,
    SwapOrMergeOperation,
    SwapDifferentItems,
    StackMergeOrSplit,
    SwapSameItem
}

/// <summary>
/// Simplified state machine for swap operations
/// </summary>
public static class SwapOperationStateMachine
{
    /// <summary>
    /// Predicts the swap operation type based on current state
    /// </summary>
    public static SwapAction GetPredictedSwapAction(SlotSnapshot targetSlot, bool isDragEmpty, string dragDescription, XUiC_ItemStack.StackLocationTypes dragPickupLocation)
    {
        if (isDragEmpty && targetSlot.IsStackPresent)
        {
            return SwapAction.PickupFromSource;
        }
        else if (!isDragEmpty && !targetSlot.IsStackPresent)
        {
            return SwapAction.PlaceInEmptyTarget;
        }
        else if (!isDragEmpty && targetSlot.IsStackPresent)
        {
            return SwapAction.SwapOrMergeOperation;
        }
        else
        {
            return SwapAction.NoOperation;
        }
    }

    /// <summary>
    /// Analyzes what operation actually occurred based on before/after snapshots
    /// </summary>
    public static SwapAction GetActualSwapAction(SlotSnapshot before, SlotSnapshot after)
    {
        bool hadItem = before.IsStackPresent;
        bool hasItem = after.IsStackPresent;

        if (!hadItem && hasItem)
        {
            return SwapAction.PlaceInEmptyTarget;
        }
        else if (hadItem && !hasItem)
        {
            return SwapAction.PickupFromSource;
        }
        else if (hadItem && hasItem)
        {
            if (before.ItemType != after.ItemType)
            {
                return SwapAction.SwapDifferentItems;
            }
            else if (before.ItemCount != after.ItemCount)
            {
                return SwapAction.StackMergeOrSplit;
            }
            else
            {
                return SwapAction.SwapSameItem;
            }
        }
        else
        {
            return SwapAction.NoOperation;
        }
    }
}