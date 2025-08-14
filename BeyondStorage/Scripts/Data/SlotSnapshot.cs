namespace BeyondStorage.Scripts.Data;

/// <summary>
/// Immutable snapshot of an XUiC_ItemStack slot state at a specific point in time.
/// Captures all relevant information about a slot including its contents, location, and properties.
/// Used for tracking slot state changes and debugging inventory operations.
/// Handles null input gracefully by using default values and recording the null state.
/// </summary>
public sealed class SlotSnapshot
{
    #region Meta State Tracking

    /// <summary>
    /// Whether the slot instance passed to the constructor was null.
    /// </summary>
    public bool IsNullInstance { get; }

    public bool IsValid { get; set; } = true;

    #endregion

    #region Core Slot Information

    /// <summary>
    /// The slot number within the inventory container.
    /// </summary>
    public int SlotNumber { get; }

    /// <summary>
    /// The type of location where this slot exists (Backpack, ToolBelt, Container, etc.).
    /// </summary>
    public XUiC_ItemStack.StackLocationTypes SlotLocation { get; }

    /// <summary>
    /// Whether this slot is currently selected in the UI.
    /// </summary>
    public bool IsSelected { get; }

    #endregion

    #region ItemStack Information

    /// <summary>
    /// Whether the ItemStack contained in this slot is null.
    /// </summary>
    public bool IsStackNull { get; }

    /// <summary>
    /// Human-readable description of the item stack contents.
    /// Format: "ItemName:Count" or "null:0" for empty slots.
    /// </summary>
    public string ItemDescription { get; }

    /// <summary>
    /// Whether the slot contains a valid, non-empty ItemStack.
    /// </summary>
    public bool IsStackPresent { get; }

    /// <summary>
    /// The count of items in the stack (0 if stack is null or empty).
    /// </summary>
    public int ItemCount { get; }

    /// <summary>
    /// The item type ID (0 if stack is null or empty).
    /// </summary>
    public int ItemType { get; }

    /// <summary>
    /// The item quality level (0 if stack is null or has no quality).
    /// </summary>
    public int ItemQuality { get; }

    /// <summary>
    /// Whether the item stack is empty (null or has no items).
    /// </summary>
    public bool IsEmpty { get; }

    #endregion

    #region Slot State Properties

    /// <summary>
    /// Whether the slot has any type of lock applied (user lock, quest lock, etc.).
    /// </summary>
    public bool IsSlotLocked { get; }

    /// <summary>
    /// Whether this slot is currently involved in a drag and drop operation.
    /// </summary>
    public bool IsDragAndDrop { get; }

    /// <summary>
    /// Whether items can be dropped into this slot.
    /// Controls whether drag and drop operations are permitted.
    /// </summary>
    public bool AllowDropping { get; }

    #endregion

    #region Location Classification

    /// <summary>
    /// Whether this slot is located in the player's inventory (backpack or toolbelt).
    /// </summary>
    public bool IsPlayerInventory { get; }

    /// <summary>
    /// Whether this slot is located in a storage container (not player inventory).
    /// </summary>
    public bool IsStorageInventory { get; }

    /// <summary>
    /// Human-readable name of the inventory type ("PLAYER" or "STORAGE").
    /// </summary>
    public string InventoryName { get; }

    #endregion

    #region Display Helpers

    /// <summary>
    /// Single character representation of stack presence ("1" if present, "0" if empty).
    /// </summary>
    public string PresenceIndicator { get; }

    /// <summary>
    /// Emoji representation of lock state (🔒 if locked, 📂 if unlocked).
    /// </summary>
    public string LockIndicator { get; }

    private long _originalCallCount;
    public long OriginalCallCount
    {
        get
        {
            return _originalCallCount;
        }
        set
        {
            if (_originalCallCount <= 0) { _originalCallCount = value; }
        }
    }
    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new immutable snapshot of the specified XUiC_ItemStack slot.
    /// If slotInstance is null, creates a snapshot with default values and records the null state.
    /// </summary>
    /// <param name="slotInstance">The XUiC_ItemStack instance to capture (may be null)</param>
    public SlotSnapshot(XUiC_ItemStack slotInstance)
    {
        IsNullInstance = slotInstance == null;

        if (IsNullInstance)
        {
            // Use default values for null slot instance
            SlotNumber = -1;
            SlotLocation = default(XUiC_ItemStack.StackLocationTypes);
            IsSelected = false;
            IsStackNull = true;
            IsSlotLocked = false;
            IsDragAndDrop = false;
            AllowDropping = false; // Default to false for null instances

            // Default ItemStack properties
            ItemCount = 0;
            ItemType = 0;
            ItemQuality = 0;
            IsEmpty = true;
        }
        else
        {
            // Core slot information (null-safe)
            SlotNumber = slotInstance.SlotNumber;
            SlotLocation = slotInstance.StackLocation;
            IsSelected = slotInstance.Selected;

            // ItemStack information derived from slot
            var stack = slotInstance.ItemStack;
            IsStackNull = stack == null;
            IsSlotLocked = ItemX.IsSlotLocked(slotInstance);
            IsDragAndDrop = slotInstance.IsDragAndDrop;
            AllowDropping = slotInstance.AllowDropping;

            // Extract ItemStack properties without storing the stack itself
            if (IsStackNull)
            {
                ItemCount = 0;
                ItemType = 0;
                ItemQuality = 0;
                IsEmpty = true;
            }
            else
            {
                ItemCount = stack.count;
                ItemType = stack.itemValue?.type ?? 0;
                ItemQuality = stack.itemValue?.Quality ?? 0;
                IsEmpty = stack.IsEmpty();
            }
        }

        // ItemStack derived information (always null-safe)
        // Use a temporary stack reference for ItemX methods if not null
        var tempStack = IsNullInstance ? null : slotInstance?.ItemStack;
        ItemDescription = ItemX.Info(tempStack);
        IsStackPresent = ItemX.IsStackPresent(tempStack);

        // Location classification (always null-safe)
        IsPlayerInventory = ItemX.IsPlayerInventory(SlotLocation);
        IsStorageInventory = ItemX.IsStorageInventory(SlotLocation);
        InventoryName = ItemX.GetInventoryName(IsPlayerInventory);

        // Display helpers (always null-safe)
        PresenceIndicator = ItemX.P(IsStackPresent);
        LockIndicator = ItemX.L(IsSlotLocked);
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a comprehensive string representation of this slot snapshot.
    /// </summary>
    /// <returns>Formatted string with key slot information</returns>
    public override string ToString()
    {
        if (IsNullInstance)
        {
            return "NULL_SLOT: No slot instance provided";
        }

        return $"[slot {SlotNumber}{LockIndicator}@{InventoryName},{ItemDescription} " +
               $"(present={PresenceIndicator}, valid={IsValid}, drag={IsDragAndDrop}, drop={AllowDropping}, loc={SlotLocation})],(call={OriginalCallCount})";
    }

    /// <summary>
    /// Returns a compact string representation suitable for logging.
    /// </summary>
    /// <returns>Compact formatted string with essential slot information</returns>
    public string ToCompactString()
    {
        if (IsNullInstance)
        {
            return "NULL_SLOT";
        }

        return $"{ItemDescription} in slot {SlotNumber}{LockIndicator}@{InventoryName} (drag={IsDragAndDrop}, drop={AllowDropping})";
    }

    #endregion
}