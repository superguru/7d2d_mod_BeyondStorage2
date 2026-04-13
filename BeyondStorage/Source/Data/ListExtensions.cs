using System.Collections.Generic;

namespace BeyondStorage.Source.Data;

public static class ListExtensions
{
    public static int IndexOfReference<T>(this List<T> list, T target) where T : class
    {
        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            if (object.ReferenceEquals(list[i], target))
                return i;
        }

        return -1;
    }
}
