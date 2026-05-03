using System.Collections.Generic;

namespace BeyondStorage.Data;

public static class CollectionHelper
{
    /// <summary>
    /// Removes the element at <paramref name="index"/> in O(1) by overwriting it with the last
    /// element, then removing the last slot. Does not preserve list order.
    /// Returns <see langword="false"/> if <paramref name="list"/> is null or <paramref name="index"/> is out of range.
    /// </summary>
    public static bool FastRemove<T>(IList<T> list, int index)
    {
        if (list == null)
        {
            return false;
        }

        int lastIndex = list.Count - 1;

        if (index < 0 || index > lastIndex)
        {
            return false;
        }

        if (index != lastIndex)
        {
            list[index] = list[lastIndex];
        }

        list.RemoveAt(lastIndex);
        return true;
    }
}
