using System.Collections.Generic;

namespace BeyondStorage.Source.Data;

public static class ListExtensions
{
    public static int IndexOfReference<T>(this List<T> list, T target) where T : class
    {
        if (list == null || target == null)
        {
            return -1;
        }

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            if (ReferenceEquals(list[i], target))
                return i;
        }

        return -1;
    }

    public static int LastIndexOfReference<T>(this List<T> list, T target) where T : class
    {
        if (list == null || target == null)
        {
            return -1;
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(list[i], target))
                return i;
        }

        return -1;
    }

}