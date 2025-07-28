using System.Collections.Generic;
using System.Linq;

namespace BeyondStorage.Scripts.Utils;

public static class ItemUtil
{
    public const int DEFAULT_ITEMSTACK_LIST_CAPACITY = 1024;

    public static string InfoItemStackToString(IEnumerable<ItemStack> stacks)
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
        var stackInfos = string.Join(", ", stackList.Select(stack => InfoItemStackToString(stack)));

        return stackDescr + stackInfos;
    }

    public static string InfoItemStackToString(ItemStack stack)
    {
        var result = "null=0";

        if (stack != null)
        {
            var itemValue = stack.itemValue;
            if (itemValue != null)
            {
                var itemClass = itemValue.ItemClass;
                if (itemClass != null)
                {
                    return $"{itemClass.Name}={stack.count}";
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Efficiently removes null and empty item stacks from the input list.
    /// Optimized for performance with minimal allocations and early exits.
    /// Ensures the input list has adequate capacity for future operations.
    /// </summary>
    /// <param name="stacks">The list of item stacks to filter</param>
    public static void StripNullAndEmptyItemStacks(List<ItemStack> stacks)
    {
        const string d_methodName = nameof(StripNullAndEmptyItemStacks);

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
                !string.IsNullOrEmpty(stack.itemValue.ItemClass.GetItemName()))
            {
                validItems.Add(stack);
            }
        }

        // Replace contents efficiently
        stacks.Clear();

        // Ensure input has adequate capacity before adding items
        if (stacks.Capacity < DEFAULT_ITEMSTACK_LIST_CAPACITY)
        {
            LogUtil.DebugLog($"{d_methodName}: Resizing stacks capacity from {stacks.Capacity} to {DEFAULT_ITEMSTACK_LIST_CAPACITY}");
            stacks.Capacity = DEFAULT_ITEMSTACK_LIST_CAPACITY;
        }

        stacks.AddRange(validItems);
    }
}