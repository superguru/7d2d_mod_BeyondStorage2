using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

/// <summary>
/// Utility class providing helper methods for ItemStack operations, validation, and formatting.
/// Contains methods for analyzing, comparing, and displaying ItemStack information.
/// </summary>
public static class ItemX
{
    #region ItemStack Information and Display

    /// <summary>
    /// Generates a formatted string representation of a collection of ItemStacks.
    /// </summary>
    /// <param name="stacks">The collection of ItemStacks to describe</param>
    /// <returns>A descriptive string showing count and details of all stacks</returns>
    public static string Info(IEnumerable<ItemStack> stacks)
    {
        if (stacks == null)
        {
            return "null stacks";
        }

        var stackList = new List<ItemStack>(stacks);

        var numStacks = stackList.Count;
        if (numStacks == 0)
        {
            return "empty stacks";
        }

        var stackDescr = $"{numStacks} stacks of ";
        var stackInfos = string.Join(", ", stackList.Select(stack => Info(stack)));

        return stackDescr + stackInfos;
    }

    /// <summary>
    /// Generates a formatted string representation of a single ItemStack.
    /// Format: "ItemName:Count" or "null:0" for invalid stacks.
    /// </summary>
    /// <param name="stack">The ItemStack to describe</param>
    /// <returns>A string in format "ItemName:Count" or "null:0" if invalid</returns>
    public static string Info(ItemStack stack)
    {
        if (stack == null)
        {
            return "null:0";
        }

        var itemName = NameOf(stack);
        return itemName != null ? $"{itemName}:{stack.count}" : "null:0";
    }

    #endregion

    #region ItemStack Comparison and Validation

    /// <summary>
    /// Compares two ItemStacks for content equality, including item type, name, and count.
    /// Does not compare by reference - compares actual content values.
    /// </summary>
    /// <param name="stack1">First ItemStack to compare</param>
    /// <param name="stack2">Second ItemStack to compare</param>
    /// <returns>True if both stacks have the same item type, name, and count; otherwise false</returns>
    public static bool EqualContents(ItemStack stack1, ItemStack stack2)
    {
        // Handle null cases
        if (ReferenceEquals(stack1, stack2))
        {
            return true;
        }

        if (stack1 == null || stack2 == null)
        {
            return false;
        }

        // Compare counts first (fast comparison)
        if (stack1.count != stack2.count)
        {
            return false;
        }

        // Extract item names using helper method
        var name1 = NameOf(stack1);
        var name2 = NameOf(stack2);

        // Compare names (handles all null/empty cases)
        return string.Equals(name1, name2, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Safely extracts the item name from an ItemStack, handling all null cases.
    /// </summary>
    /// <param name="stack">The ItemStack to extract name from</param>
    /// <returns>Item name or null if any part of the hierarchy is null</returns>
    private static string NameOf(ItemStack stack)
    {
        return stack?.itemValue?.ItemClass?.Name;
    }

    /// <summary>
    /// Determines if an ItemStack contains valid item data and has a positive count.
    /// </summary>
    /// <param name="stack">The ItemStack to check for presence</param>
    /// <returns>True if the stack is not null, not empty, and has valid item data; otherwise false</returns>
    internal static bool IsStackPresent(ItemStack stack)
    {
        return stack != null && !stack.IsEmpty();
    }

    /// <summary>
    /// Determines if an ItemStack is completely valid with all required data.
    /// </summary>
    /// <param name="stack">The ItemStack to validate</param>
    /// <returns>True if stack has valid data and positive count; otherwise false</returns>
    internal static bool IsCompletelyValid(ItemStack stack)
    {
        return stack != null &&
               stack.count > 0 &&
               !string.IsNullOrEmpty(NameOf(stack));
    }

    #endregion

    #region ItemStack Collection Management

    /// <summary>
    /// Removes invalid ItemStacks from the provided list in-place.
    /// Invalid stacks are those with zero/negative count, null itemValue, or missing ItemClass.
    /// </summary>
    /// <param name="stacks">The list of ItemStacks to purge (modified in-place)</param>
    public static void PurgeInvalidItemStacks(List<ItemStack> stacks)
    {
        if (stacks == null || stacks.Count == 0)
        {
            return;
        }

        stacks.RemoveAll(stack => !IsValidItemStack(stack));
    }

    /// <summary>
    /// Determines if an ItemStack is valid for operations.
    /// </summary>
    /// <param name="stack">The ItemStack to validate</param>
    /// <returns>True if the stack is valid; otherwise false</returns>
    private static bool IsValidItemStack(ItemStack stack)
    {
        return stack?.count > 0 &&
               stack.itemValue?.ItemClass != null &&
               !stack.itemValue.IsEmpty() &&
               !string.IsNullOrEmpty(NameOf(stack));
    }

    /// <summary>
    /// Extracts unique item types from a list of ItemStacks.
    /// Returns -1 for null/empty stacks or when type=0 (empty item type).
    /// If no valid items are found, returns a list containing only -1 (unfiltered).
    /// </summary>
    /// <param name="stacks">List of ItemStacks to extract types from</param>
    /// <returns>Read-only list of unique item types, using -1 for unfiltered/empty</returns>
    public static IReadOnlyList<int> GetUniqueItemTypes(List<ItemStack> stacks)
    {
        const string d_MethodName = nameof(GetUniqueItemTypes);
        const int UnfilteredType = UniqueItemTypes.WILDCARD;

        if (stacks == null || stacks.Count == 0)
        {
            return new int[] { UnfilteredType };
        }

        var validTypes = ExtractValidItemTypes(stacks);

        if (validTypes.Count == 0)
        {
            return new int[] { UnfilteredType };
        }

        var result = validTypes.ToArray();
        System.Array.Sort(result);

        ModLogger.DebugLog($"{d_MethodName}: Found {result.Length} unique types from {stacks.Count} stacks: [{string.Join(", ", result)}]");

        return result;
    }

    /// <summary>
    /// Extracts valid item types from ItemStacks, normalizing empty types to -1.
    /// </summary>
    /// <param name="stacks">List of ItemStacks to process</param>
    /// <returns>HashSet of valid item types</returns>
    private static HashSet<int> ExtractValidItemTypes(List<ItemStack> stacks)
    {
        const int UnfilteredType = -1;
        var uniqueTypes = new HashSet<int>();

        foreach (var stack in stacks)
        {
            if (stack?.count <= 0 || stack.itemValue?.ItemClass == null)
            {
                continue;
            }

            int itemType = stack.itemValue.type <= 0 ? UnfilteredType : stack.itemValue.type;
            uniqueTypes.Add(itemType);
        }

        return uniqueTypes;
    }

    #endregion

    #region Slot Location and Type Utilities

    /// <summary>
    /// Determines if the specified slot location represents a player inventory slot.
    /// Player inventory includes both backpack and toolbelt slots.
    /// </summary>
    /// <param name="location">The slot location type to check</param>
    /// <returns>True if the location is Backpack or ToolBelt; otherwise false</returns>
    internal static bool IsPlayerInventory(XUiC_ItemStack.StackLocationTypes location)
    {
        return location is XUiC_ItemStack.StackLocationTypes.Backpack or XUiC_ItemStack.StackLocationTypes.ToolBelt;
    }

    /// <summary>
    /// Determines if the specified slot location represents a storage container slot.
    /// This is the inverse of IsPlayerInventory - any slot that is not player inventory.
    /// </summary>
    /// <param name="location">The slot location type to check</param>
    /// <returns>True if the location is not a player inventory slot; otherwise false</returns>
    internal static bool IsStorageInventory(XUiC_ItemStack.StackLocationTypes location)
    {
        return !IsPlayerInventory(location);
    }

    /// <summary>
    /// Determines if the specified slot has any type of lock applied to it.
    /// Checks all possible lock types including user locks, quest locks, assembly locks, etc.
    /// </summary>
    /// <param name="slot">The XUiC_ItemStack slot to check for locks</param>
    /// <returns>True if any lock type is active on the slot; otherwise false</returns>
    internal static bool IsSlotLocked(XUiC_ItemStack slot)
    {
        if (slot == null)
        {
            return false;
        }

        var lockProperties = new[]
        {
            slot.IsLocked, slot.StackLock, slot.AssembleLock, slot.QuestLock,
            slot.ToolLock, slot.HiddenLock, slot.AttributeLock, slot.UserLockedSlot
        };

        return lockProperties.Any(locked => locked);
    }

    #endregion

    #region Display and Formatting Helpers

    /// <summary>
    /// Converts a boolean presence value to a single character display format.
    /// Used for compact logging and debugging output.
    /// </summary>
    /// <param name="isPresent">Whether the item/stack is present</param>
    /// <returns>"1" if present, "0" if not present</returns>
    internal static string E(bool isPresent) => P(!isPresent);

    internal static string P(bool isPresent) => isPresent ? "1" : "0";

    /// <summary>
    /// Converts a boolean lock state to an emoji display format.
    /// Used for visual indication of lock status in logs and debugging.
    /// </summary>
    /// <param name="isLocked">Whether the slot/item is locked</param>
    /// <returns>🔒 if locked, 📂 if not locked</returns>
    internal static string L(bool isLocked) => isLocked ? "🔒" : "📂";

    /// <summary>
    /// Gets a human-readable inventory name based on whether the slot is in player inventory.
    /// Used for consistent naming in logs and user interface elements.
    /// </summary>
    /// <param name="isCurrentSlotPlayerInventory">True if the slot is in player inventory</param>
    /// <returns>"PLAYER" for player inventory slots, "STORAGE" for container slots</returns>
    internal static string GetInventoryName(bool isCurrentSlotPlayerInventory) =>
        isCurrentSlotPlayerInventory ? "PLAYER" : "STORAGE";

    #endregion
}