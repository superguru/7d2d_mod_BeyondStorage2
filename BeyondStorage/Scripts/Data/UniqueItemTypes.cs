using System;
using System.Collections.Generic;

namespace BeyondStorage.Scripts.Data;

public sealed class UniqueItemTypes
{
    private readonly int[] _itemTypes;

    private static readonly int[] s_unfiltered_itemTypes = new int[] { -1 };
    private static readonly UniqueItemTypes s_unfiltered = new UniqueItemTypes(s_unfiltered_itemTypes);

    public int Count => _itemTypes.Length;

    public bool IsUnfiltered => _itemTypes.Length == 1 && _itemTypes[0] == -1;

    public bool IsFiltered => !IsUnfiltered;

    public UniqueItemTypes(IEnumerable<int> itemTypes)
    {
        if (itemTypes == null)
        {
            _itemTypes = s_unfiltered_itemTypes;
            return;
        }

        // Process input according to rules
        var validTypes = new HashSet<int>();
        bool hasWildcard = false;

        foreach (int itemType in itemTypes)
        {
            if (itemType == 0)
            {
                // Rule: Skip itemType == 0 (empty items)
                continue;
            }
            else if (itemType == -1)
            {
                hasWildcard = true;
                validTypes.Add(-1);
            }
            else if (itemType > 0)
            {
                validTypes.Add(itemType);
            }
            // Skip any other invalid values (< -1)
        }

        // Apply rules for -1 (wildcard)
        if (hasWildcard)
        {
            // Rule: If -1 is present, ALL input types must be -1
            if (validTypes.Count > 1)
            {
                throw new ArgumentException("When -1 (wildcard) is provided, all item types must be -1. Mixed wildcard and specific types are not allowed.");
            }
            _itemTypes = s_unfiltered_itemTypes;
        }
        else if (validTypes.Count == 0)
        {
            throw new ArgumentException("No valid item types provided. UniqueItemTypes must contain at least one valid item type.");
        }
        else
        {
            // Valid specific item types
            _itemTypes = [.. validTypes];
        }

        ValidateInvariants();
    }

    public UniqueItemTypes(params int[] itemTypes) : this((IEnumerable<int>)itemTypes) { }

    private void ValidateInvariants()
    {
        // Invariant: Always has at least one element
        if (_itemTypes.Length == 0)
        {
            throw new InvalidOperationException("UniqueItemTypes must always contain at least one element");
        }

        // Invariant: If -1 is present, it must be the only element
        if (_itemTypes.Length == 1)
        {
            if (_itemTypes[0] == -1 || _itemTypes[0] > 0)
            {
                return; // Valid
            }
        }

        Array.Sort(_itemTypes);

        // Invariant: No element can be 0
        // Invariant: If -1 is present, it must be the only element
        for (int i = 0; i < _itemTypes.Length; i++)
        {
            int itemType = _itemTypes[i];

            if (itemType == 0)
            {
                throw new InvalidOperationException("UniqueItemTypes cannot contain 0 (empty item type)");
            }
            else if (itemType < -1)
            {
                throw new InvalidOperationException($"UniqueItemTypes cannot contain invalid item types (< -1). Found {itemType} at index {i}");
            }
            else if (itemType == -1 && i != 0)
            {
                throw new InvalidOperationException("When -1 (wildcard) is present, it must be the only element");
            }

            // No need to check for duplicates here, as we use a HashSet in the constructor

            if (itemType > 0)
            {
                break; // Valid item type found, no need to check further
            }
        }
    }

    public bool Contains(int itemType)
    {
        if (itemType == 0)
        {
            // Rule: itemType 0 is never contained
            return false;
        }

        if (itemType == -1)
        {
            // Rule: -1 is contained only if it's the sole element
            return _itemTypes.Length == 1 && _itemTypes[0] == -1;
        }

        if (itemType < 0)
        {
            // Invalid item types (< -1) are never contained
            return false;
        }

        // Check for wildcard first
        if (_itemTypes.Length == 1 && _itemTypes[0] == -1)
        {
            return true; // Wildcard matches any valid item type > 0
        }

        // Optimized search for small collections
        switch (_itemTypes.Length)
        {
            case 1:
                return _itemTypes[0] == itemType;
            case 2:
                return _itemTypes[0] == itemType || _itemTypes[1] == itemType;
            case 3:
                return _itemTypes[0] == itemType || _itemTypes[1] == itemType ||
                       _itemTypes[2] == itemType;
            case 4:
                return _itemTypes[0] == itemType || _itemTypes[1] == itemType ||
                       _itemTypes[2] == itemType || _itemTypes[3] == itemType;
            case 5:
                return _itemTypes[0] == itemType || _itemTypes[1] == itemType ||
                       _itemTypes[2] == itemType || _itemTypes[3] == itemType ||
                       _itemTypes[4] == itemType;
        }

        for (int i = 5; i < _itemTypes.Length; i++)
        {
            if (_itemTypes[i] == itemType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if two UniqueItemTypes instances are equivalent.
    /// Two instances are considered equivalent if they represent the same set of item types.
    /// </summary>
    /// <param name="cached">The first UniqueItemTypes to compare</param>
    /// <param name="requested">The second UniqueItemTypes to compare</param>
    /// <returns>True if both instances are equivalent, false otherwise</returns>
    public static bool IsEquivalent(UniqueItemTypes cached, UniqueItemTypes requested)
    {
        if (cached == null || requested == null)
        {
            return cached == requested;
        }

        if (cached.IsUnfiltered && requested.IsUnfiltered)
        {
            return true;
        }

        if (cached.IsUnfiltered != requested.IsUnfiltered)
        {
            return false;
        }

        if (cached.Count != requested.Count)
        {
            return false;
        }

        foreach (int type in requested)
        {
            if (!cached.Contains(type))
            {
                return false;
            }
        }

        return true;
    }

    public static UniqueItemTypes FromItemStacks(List<ItemStack> stacks)
    {
        if (stacks == null || stacks.Count == 0)
        {
            return s_unfiltered;
        }

        var uniqueTypes = new HashSet<int>();
        bool hasValidItems = false;

        for (int i = 0; i < stacks.Count; i++)
        {
            var stack = stacks[i];
            if (stack?.count <= 0)
            {
                continue;
            }

            var itemValue = stack.itemValue;
            if (itemValue?.ItemClass == null)
            {
                continue;
            }

            int itemType = itemValue.type;

            if (itemType > 0)
            {
                uniqueTypes.Add(itemType);
                hasValidItems = true;
            }
            // Skip itemType <= 0 (empty/invalid items)
        }

        if (!hasValidItems || uniqueTypes.Count == 0)
        {
            return s_unfiltered;
        }

        return new UniqueItemTypes(uniqueTypes);
    }

    public static UniqueItemTypes FromItemType(int itemType)
    {
        if (itemType <= 0)
        {
            return s_unfiltered;
        }

        return new UniqueItemTypes(itemType);
    }

    public static UniqueItemTypes FromItemStack(ItemStack stack)
    {
        if (stack?.count <= 0)
        {
            return s_unfiltered;
        }

        var itemValue = stack.itemValue;
        if (itemValue?.ItemClass == null)
        {
            return s_unfiltered;
        }

        int itemType = itemValue.type;
        if (itemType <= 0)
        {
            return s_unfiltered;
        }

        return new UniqueItemTypes(itemType);
    }

    public static UniqueItemTypes Unfiltered => s_unfiltered;

    public override string ToString()
    {
        if (IsUnfiltered)
        {
            return "UniqueItemTypes: unfiltered (wildcard)";
        }

        return $"UniqueItemTypes: {Count} filtered types";
    }

    public string GetStatistics()
    {
        if (IsUnfiltered)
        {
            return "UniqueItemTypes: unfiltered (wildcard), matches any valid item type";
        }

        var typeInfo = Count == 1 ? "single type" : $"{Count} types";
        return $"UniqueItemTypes: filtered, {typeInfo}";
    }

    public IEnumerator<int> GetEnumerator()
    {
        return ((IEnumerable<int>)_itemTypes).GetEnumerator();
    }
}