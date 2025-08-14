using System.Collections.Generic;

namespace BeyondStorage.Scripts.Data;

/// <summary>
/// Analyzes drag and drop system state for XUiC_ItemStack operations.
/// Encapsulates the logic for determining drag stack information, swap compatibility, and operation validation.
/// </summary>
public class DragDropAnalyzer
{
    public string DragStackInfo { get; private set; }
    public bool IsDragEmpty { get; private set; }
    public XUiC_ItemStack.StackLocationTypes DragPickupLocation { get; private set; }
    public bool CanSwap { get; private set; }
    public SlotSnapshot SlotSnapshot { get; private set; }

    /// <summary>
    /// Creates a new DragDropAnalyzer for the given slot snapshot and XUiC_ItemStack instance.
    /// Analyzes the drag and drop system state and determines swap compatibility.
    /// </summary>
    /// <param name="slotSnapshot">The slot snapshot to analyze</param>
    /// <param name="instance">The XUiC_ItemStack instance containing drag and drop system</param>
    public DragDropAnalyzer(SlotSnapshot slotSnapshot, XUiC_ItemStack instance)
    {
        SlotSnapshot = slotSnapshot;
        AnalyzeDragAndDropState(instance);
    }

    /// <summary>
    /// Analyzes the drag and drop system state for the given XUiC_ItemStack instance.
    /// Sets all relevant properties based on the current drag and drop context.
    /// </summary>
    /// <param name="instance">The XUiC_ItemStack instance to analyze</param>
    private void AnalyzeDragAndDropState(XUiC_ItemStack instance)
    {
        // Initialize default values
        DragStackInfo = "null:0";
        IsDragEmpty = true;
        DragPickupLocation = default;
        CanSwap = false;

        var dragAndDrop = instance.xui?.dragAndDrop;
        if (dragAndDrop != null)
        {
            var dragStack = dragAndDrop.CurrentStack;
            DragStackInfo = ItemX.Info(dragStack);
            IsDragEmpty = dragStack?.IsEmpty() ?? true;
            DragPickupLocation = dragAndDrop.PickUpType;

            CanSwap = !IsDragEmpty && instance.CanSwap(dragStack);
            SlotSnapshot.IsValid = SlotSnapshot.IsValid || CanSwap;
        }
    }

    /// <summary>
    /// Gets a formatted string containing all drag and drop analysis information.
    /// Useful for logging and debugging purposes.
    /// </summary>
    /// <returns>Formatted string with drag stack info, pickup location, and swap capability</returns>
    public string GetAnalysisInfo()
    {
        return $"DragStack:{DragStackInfo}, PickupLocation:{DragPickupLocation}, " +
               $"IsDragEmpty:{IsDragEmpty}, CanSwap:{CanSwap}";
    }

    /// <summary>
    /// Determines if the drag operation represents a cross-inventory move.
    /// </summary>
    /// <returns>True if the drag pickup location differs from the slot location</returns>
    public bool IsCrossInventoryMove()
    {
        return SlotSnapshot.SlotLocation != DragPickupLocation;
    }

    /// <summary>
    /// Determines if the current state allows for a valid swap operation.
    /// </summary>
    /// <returns>True if conditions are met for a valid swap</returns>
    public bool IsValidSwapCondition()
    {
        return !IsDragEmpty && CanSwap && SlotSnapshot.IsValid;
    }

    /// <summary>
    /// Gets detailed validation information about why a swap might be invalid.
    /// </summary>
    /// <returns>String describing validation status and reasons</returns>
    public string GetValidationInfo()
    {
        if (IsValidSwapCondition())
        {
            return "Valid swap condition";
        }

        var reasons = new List<string>();
        if (IsDragEmpty)
        {
            reasons.Add("drag is empty");
        }

        if (!CanSwap)
        {
            reasons.Add("cannot swap");
        }

        if (!SlotSnapshot.IsValid)
        {
            reasons.Add("slot invalid");
        }

        return $"Invalid swap: {string.Join(", ", reasons)}";
    }

    /// <summary>
    /// Returns a comprehensive string representation of the drag and drop analysis.
    /// Includes slot information, drag state, and operation validation details.
    /// </summary>
    /// <returns>Formatted string with complete drag and drop analysis information</returns>
    public override string ToString()
    {
        var crossInventory = IsCrossInventoryMove() ? "CROSS" : "SAME";
        var validSwap = IsValidSwapCondition() ? "VALID" : "INVALID";

        // DDA = Drag Drop Analyzer
        return $"DDA[" +
               $"Drag:{DragStackInfo}➡️{DragPickupLocation}, " +
               $"Empty:{IsDragEmpty}, Swap:{CanSwap}, " +
               $"Inv:{crossInventory}, Valid:{validSwap}]";
    }
}