using System.Collections.Generic;
using System.Linq;

namespace BeyondStorage.Scripts.Utils;

public static class ItemUtil
{
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
}