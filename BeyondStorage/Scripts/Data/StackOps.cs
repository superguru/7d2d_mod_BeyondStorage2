namespace BeyondStorage.Scripts.Data;

/// <summary>
/// Constants and simple enums for stack operations
/// </summary>
public static class StackOps
{
    // Operation constants for UI refresh triggers
    public const string ItemStack_DropMerge_Operation = "ItemStack_DropMerge_Operation";
    public const string ItemStack_Drop_Operation = "ItemStack_Drop_Operation";
    public const string ItemStack_DropSingleItem_Operation = "ItemStack_DropSingleItem_Operation";
    public const string ItemStack_Pickup_Operation = "ItemStack_Pickup_Operation";
    public const string ItemStack_Pickup_Half_Stack_Operation = "ItemStack_Pickup_Half_Stack_Operation";
    public const string ItemStack_Shift_Operation = "ItemStack_Shift_Operation";
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