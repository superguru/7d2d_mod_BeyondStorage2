using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;
internal class UniqueItemTypeCache
{
    private readonly Dictionary<int, UniqueItemTypes> _filterCache = new();

    public void Clear()
    {
        _filterCache.Clear();
    }

    public UniqueItemTypes GetOrCreateFilter(ItemStack stack)
    {
        const string d_MethodName = nameof(GetOrCreateFilter);

        var itemType = stack?.itemValue?.type ?? 0;
        if (itemType <= 0)
        {
            var error = $"{d_MethodName}: {nameof(stack)} is null or has an invalid item type.";
            ModLogger.Error(error);
            throw new ArgumentException(error, nameof(stack));
        }

        var filter = GetOrCreateFilter(itemType);
        return filter;
    }

    public UniqueItemTypes GetOrCreateFilter(int itemType)
    {
        const string d_MethodName = nameof(GetOrCreateFilter);

        if (itemType <= 0)
        {
            var error = $"{d_MethodName}: {nameof(itemType)} must be greater than zero, but received {itemType}";
            ModLogger.Error(error);
            throw new ArgumentOutOfRangeException(nameof(itemType), error);
        }

        if (_filterCache.TryGetValue(itemType, out var filter))
        {
            return filter;
        }

        filter = new UniqueItemTypes(itemType);
        _filterCache[itemType] = filter;

        return filter;
    }

}
