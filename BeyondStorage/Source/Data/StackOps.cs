using System;
using System.Collections.Generic;
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
    ItemStack_Shift_Operation,
    MoveAll_Operation,
    Stack_LockStateChange_Operation,
}

/// <summary>
/// Utilities for stack operations
/// </summary>
public static class StackOperation
{
    private static readonly Dictionary<StackOps, string> s_stackOpNames = [];

    private static void EnsureStackOpsNameLookup()
    {
        if (s_stackOpNames.Count > 0)
        {
            return;
        }

        foreach (StackOps op in Enum.GetValues(typeof(StackOps)))
        {
            s_stackOpNames[op] = $"{op}";
        }
    }

    public static string GetStackOpName(StackOps operation)
    {
        EnsureStackOpsNameLookup();
        return s_stackOpNames[operation];
    }

    /// <summary>
    /// Determines whether the specified operation is a known stack operation.
    /// </summary>
    /// <param name="operation">The operation to validate</param>
    /// <returns>True if the operation is a defined enum value; otherwise, false</returns>
    public static bool IsValidOperation(StackOps operation)
    {
        EnsureStackOpsNameLookup();
        return s_stackOpNames.ContainsKey(operation);
    }

    /// <summary>
    /// Determines whether the specified operation name is a known stack operation constant.
    /// </summary>
    /// <param name="operationName">The operation name to validate</param>
    /// <returns>True if the operation name matches one of the defined enum values; otherwise, false</returns>
    public static bool IsValidOperation(string operationName)
    {
        EnsureStackOpsNameLookup();
        return s_stackOpNames.ContainsValue(operationName);
    }

    /// <summary>
    /// Gets all valid operation enum values.
    /// </summary>
    /// <returns>Array of all StackOperation enum values</returns>
    public static StackOps[] GetAllOperations()
    {
        EnsureStackOpsNameLookup();
        return s_stackOpNames.Keys.ToArray();
    }

    /// <summary>
    /// Gets all valid operation string representations.
    /// </summary>
    /// <returns>Array of all operation string representations</returns>
    public static string[] GetAllOperationStrings()
    {
        EnsureStackOpsNameLookup();
        return s_stackOpNames.Values.ToArray();
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