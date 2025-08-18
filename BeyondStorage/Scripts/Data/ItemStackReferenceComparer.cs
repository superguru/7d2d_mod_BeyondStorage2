using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BeyondStorage.Scripts.Data;

/// <summary>
/// Reference-based equality comparer for ItemStack objects.
/// Uses reference equality and RuntimeHelpers.GetHashCode for consistent hashing.
/// This ensures we track the exact ItemStack instances from storage sources,
/// even when their values (like count) are modified during removal operations.
/// </summary>
/// <remarks>
/// This comparer is essential for tracking ItemStack instances through operations that modify
/// their properties. Unlike value-based comparison, this comparer identifies the same physical
/// object in memory, regardless of content changes.
/// 
/// Example usage:
/// - Tracking ItemStacks during removal operations where counts change
/// - Using ItemStacks as keys in collections where reference identity matters
/// - Comparing snapshots to determine if operations affect the same stack instance
/// </remarks>
internal sealed class ItemStackReferenceComparer : IEqualityComparer<ItemStack>
{
    #region Static Members

    /// <summary>
    /// Singleton instance for efficient reuse across the application.
    /// Use this instance instead of creating new instances for better performance.
    /// </summary>
    public static readonly ItemStackReferenceComparer Instance = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Private constructor to enforce singleton pattern.
    /// Use <see cref="Instance"/> to access the comparer.
    /// </summary>
    private ItemStackReferenceComparer() { }

    #endregion

    #region IEqualityComparer<ItemStack> Implementation

    /// <summary>
    /// Determines whether two ItemStack references are equal using reference equality.
    /// This method compares object identity, not content equality.
    /// </summary>
    /// <param name="x">The first ItemStack to compare</param>
    /// <param name="y">The second ItemStack to compare</param>
    /// <returns>
    /// True if both parameters refer to the same object instance or both are null;
    /// otherwise, false
    /// </returns>
    /// <example>
    /// <code>
    /// ItemStack stack1 = new ItemStack(someItem, 5);
    /// ItemStack stack2 = stack1; // Same reference
    /// ItemStack stack3 = new ItemStack(someItem, 5); // Different instance, same content
    /// 
    /// bool result1 = Instance.Equals(stack1, stack2); // True - same reference
    /// bool result2 = Instance.Equals(stack1, stack3); // False - different instances
    /// </code>
    /// </example>
    public bool Equals(ItemStack x, ItemStack y)
    {
        return ReferenceEquals(x, y);
    }

    /// <summary>
    /// Returns a hash code for the specified ItemStack using its reference identity.
    /// Uses RuntimeHelpers.GetHashCode to ensure consistent hashing based on object identity
    /// rather than content, which is crucial for reference-based comparisons.
    /// </summary>
    /// <param name="obj">The ItemStack for which to get a hash code</param>
    /// <returns>
    /// A hash code based on the object's reference identity, or 0 if the object is null
    /// </returns>
    /// <remarks>
    /// This method uses RuntimeHelpers.GetHashCode which provides a stable hash code
    /// based on the object's identity, not its contents. This ensures that the hash code
    /// remains consistent even if the ItemStack's properties (like count) are modified.
    /// </remarks>
    public int GetHashCode(ItemStack obj)
    {
        return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }

    #endregion
}