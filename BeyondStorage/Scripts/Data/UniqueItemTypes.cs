using System;
using System.Collections.Generic;
using System.Linq;

namespace BeyondStorage.Scripts.Data;

public sealed class UniqueItemTypes
{
    private readonly int[] _itemTypes;

    private static int[] SUnfilteredItemTypes { get; } = [-1];
    private static readonly Lazy<UniqueItemTypes> s_unfiltered = new(() =>
    {
        var instance = new UniqueItemTypes([-1]);
        // Logging happens here, after mod is fully initialized
        return instance;
    });

    public static UniqueItemTypes Unfiltered => s_unfiltered.Value;

    public int Count => _itemTypes.Length;

    public bool IsUnfiltered => _itemTypes.Length == 1 && _itemTypes[0] == -1;

    public bool IsFiltered => !IsUnfiltered;

    public UniqueItemTypes(int itemType) : this([itemType]) { }

    public UniqueItemTypes(IEnumerable<int> itemTypes)
    {
        if (itemTypes == null)
        {
            _itemTypes = SUnfilteredItemTypes;
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
            _itemTypes = SUnfilteredItemTypes;
        }
        else if (validTypes.Count == 0)
        {
            throw new ArgumentException("No valid item types provided. UniqueItemTypes must contain at least one valid item type.");
        }
        else
        {
            // Valid specific item types
#pragma warning disable IDE0305 // Simplify collection initialization
            _itemTypes = validTypes.ToArray();  // Faster than collection expression, avoids unnecessary allocations
#pragma warning restore IDE0305
            Array.Sort(_itemTypes);
        }

        ValidateInvariants();
    }

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

    public static bool IsPopulatedStack(ItemStack stack)
    {
        var isValidStack = IsValidStack(stack);
        var isValidItemValue = isValidStack && IsValidItemValue(stack.itemValue);

        return isValidItemValue;
    }

    public bool Contains(ItemStack stack)
    {
        if (!IsValidStack(stack))
        {
            // This stack is invalid, skip it.
            return false;
        }

        var itemValue = stack.itemValue;
        var result = Contains(itemValue);

        return result;
    }

    public bool Contains(ItemValue itemValue)
    {
        if (!IsValidItemValue(itemValue))
        {
            // This item is invalid, skip it.
            return false;
        }

        var itemType = itemValue.type;

        var result = Contains(itemType);
        return result;
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

        // Check for wildcard first (must be sole element and at index 0 after sorting)
        if (_itemTypes.Length == 1 && _itemTypes[0] == -1)
        {
            return true; // Wildcard matches any valid item type > 0
        }

        // Since _itemTypes is sorted and contains only positive integers (no -1 case here),
        // we can use binary search for O(log n) performance
        return Array.BinarySearch(_itemTypes, itemType) >= 0;
    }

    /// <summary>
    /// Determines if the cached filter can satisfy the requested filter.
    /// Returns true if the cached data contains all item types needed by the requested filter.
    /// </summary>
    /// <param name="cached">The cached filter representing what data is available</param>
    /// <param name="requested">The requested filter representing what data is needed</param>
    /// <returns>True if cached can satisfy requested, false otherwise</returns>
    public static bool CanSatisfy(UniqueItemTypes cached, UniqueItemTypes requested)
    {
        // Null filters cannot satisfy anything or be satisfied
        if (cached == null || requested == null)
        {
            return false;
        }

        var cachedIsUnfiltered = cached.IsUnfiltered;
        var requestedIsUnfiltered = requested.IsUnfiltered;

        // If cached is unfiltered, it can satisfy any request
        if (cachedIsUnfiltered)
        {
            return true; // Unfiltered cache has everything
        }

        // If requested is unfiltered, only unfiltered cache can satisfy it
        if (requestedIsUnfiltered)
        {
            return false; // Filtered cache cannot provide "everything"
        }

        // Both are filtered - check if cached contains all requested types
        // Since arrays are sorted, we can use a two-pointer approach
        int cachedIndex = 0;
        int requestedIndex = 0;

        while (requestedIndex < requested._itemTypes.Length)
        {
            // If we've exhausted cached types, we can't satisfy the rest
            if (cachedIndex >= cached._itemTypes.Length)
            {
                return false;
            }

            int cachedType = cached._itemTypes[cachedIndex];
            int requestedType = requested._itemTypes[requestedIndex];

            if (cachedType == requestedType)
            {
                // Found a match, advance both pointers
                cachedIndex++;
                requestedIndex++;
            }
            else if (cachedType < requestedType)
            {
                // Cached type is smaller, advance cached pointer
                cachedIndex++;
            }
            else
            {
                // Requested type is smaller and not in cached, cannot satisfy
                return false;
            }
        }

        // All requested types were found in cached
        return true;
    }

    public static UniqueItemTypes FromItemStacks(List<ItemStack> stacks)
    {
        if (stacks == null || stacks.Count == 0)
        {
            return Unfiltered;
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
            return Unfiltered;
        }

        return new UniqueItemTypes(uniqueTypes);
    }

    public static bool IsValidStack(ItemStack stack)
    {
        if (stack?.count <= 0)
        {
            return false;
        }

        return true;
    }

    public static UniqueItemTypes FromItemStack(ItemStack stack)
    {
        if (IsValidStack(stack))
        {
            return FromItemValue(stack.itemValue);
        }

        return Unfiltered;
    }

    public static UniqueItemTypes FromItemValue(ItemValue itemValue)
    {
        if (IsValidItemValue(itemValue))
        {
            return new UniqueItemTypes(itemValue.type);
        }

        return Unfiltered;
    }

    private static bool IsValidItemValue(ItemValue itemValue)
    {
        if (itemValue?.ItemClass == null)
        {
            return false;
        }

        if (itemValue.type <= 0)
        {
            // Rule: itemType 0 is never valid
            return false;
        }

        return true;
    }

    public IEnumerator<int> GetEnumerator()
    {
        return ((IEnumerable<int>)_itemTypes).GetEnumerator();
    }

    public override string ToString()
    {
        return GetDiagnosticInfo();
    }

    public string GetDiagnosticInfo()
    {
        var info = $"Filters: {_itemTypes.Length}";
        var details = string.Join(", ", _itemTypes.Select(itemType => ItemTypeNameLookupCache.GetItemTypeName(itemType)));

        return info + " [" + details + "]";
    }
}