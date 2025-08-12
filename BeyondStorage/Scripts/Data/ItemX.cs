using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

public static class ItemX
{
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

    public static string Info(ItemStack stack)
    {
        var result = "null:0";

        if (stack != null)
        {
            var itemValue = stack.itemValue;
            if (itemValue != null)
            {
                var itemClass = itemValue.ItemClass;
                if (itemClass != null)
                {
                    return $"{itemClass.Name}:{stack.count}";
                }
            }
        }

        return result;
    }

    public static bool EqualContents(ItemStack stack1, ItemStack stack2)
    {
        if (stack1 == null && stack2 == null)
        {
            return true;
        }

        if (stack1 == null || stack2 == null)
        {
            return false;
        }

        if (stack1.count != stack2.count)
        {
            return false;
        }

        if (stack1.itemValue == null && stack2.itemValue == null)
        {
            return true;
        }

        if (stack1.itemValue == null || stack2.itemValue == null)
        {
            return false;
        }

        var stack1Class = stack1.itemValue.ItemClass;
        var stack2Class = stack2.itemValue.ItemClass;

        if (stack1Class == null && stack2Class == null)
        {
            return true;
        }

        if (stack1Class == null || stack2Class == null)
        {
            return false;
        }

        var stack1Name = stack1Class.Name;
        var stack2Name = stack2Class.Name;

        if (string.IsNullOrEmpty(stack1Name) && string.IsNullOrEmpty(stack2Name))
        {
            return true;
        }

        if (string.IsNullOrEmpty(stack1Name) || string.IsNullOrEmpty(stack2Name))
        {
            return false;
        }

        return stack1.itemValue.ItemClass.Name == stack2.itemValue.ItemClass.Name;
    }

    public static void PurgeInvalidItemStacks(List<ItemStack> stacks)
    {
        if (stacks == null || stacks.Count == 0)
        {
            return;
        }

        // Create temporary list with exact capacity needed
        var validItems = new List<ItemStack>(stacks.Count);

        foreach (var stack in stacks)
        {
            if (stack?.count > 0 &&
                stack.itemValue?.ItemClass != null &&
                !stack.itemValue.IsEmpty() &&
                !string.IsNullOrEmpty(stack.itemValue.ItemClass?.Name))
            {
                validItems.Add(stack);
            }
        }

        stacks.Clear();
        stacks.AddRange(validItems);
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

        if (stacks == null || stacks.Count == 0)
        {
            return new int[] { -1 }; // Return unfiltered for null/empty lists
        }

        var uniqueTypes = new HashSet<int>();
        bool hasValidItems = false;

        foreach (var stack in stacks)
        {
            if (stack?.count <= 0)
            {
                continue; // Skip empty stacks
            }

            var itemValue = stack.itemValue;
            if (itemValue?.ItemClass == null)
            {
                continue; // Skip invalid items
            }

            int itemType = itemValue.type;

            // Convert type=0 (empty type) to -1 (unfiltered convention)
            if (itemType <= 0)
            {
                itemType = -1;
            }

            uniqueTypes.Add(itemType);
            hasValidItems = true;
        }

        // If no valid items found, return unfiltered
        if (!hasValidItems || uniqueTypes.Count == 0)
        {
            return new int[] { -1 };
        }

        // Convert to sorted array for optimal performance
        var result = uniqueTypes.ToArray();
        System.Array.Sort(result);

        ModLogger.DebugLog($"{d_MethodName}: Found {result.Length} unique types from {stacks.Count} stacks: [{string.Join(", ", result)}]");

        return result;
    }
}