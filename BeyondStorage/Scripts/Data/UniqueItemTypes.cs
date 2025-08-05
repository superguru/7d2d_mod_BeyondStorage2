using System;
using System.Collections.Generic;
using System.Linq;

namespace BeyondStorage.Scripts.Data;

public sealed class UniqueItemTypes : IEquatable<UniqueItemTypes>
{
    public const int EMPTY = 0;
    public const int WILDCARD = -1;

    private readonly int[] _itemTypes;

    private static int[] SUnfilteredItemTypes { get; } = [WILDCARD];
    private static readonly Lazy<UniqueItemTypes> s_unfiltered = new(() =>
    {
        var instance = new UniqueItemTypes([WILDCARD]);
        // Lazy so logging can happen here, after mod is fully initialized
        return instance;
    });

    public static UniqueItemTypes Unfiltered => s_unfiltered.Value;

    public int Count => _itemTypes.Length;

    public bool IsSingleItemMatch(UniqueItemTypes filter)
    {
        if (filter == null)
        {
            return false;
        }

        // Both this filter and the targer filter have exactly one item, and they're the same
        return Count == 1 && filter.Count == 1 && _itemTypes[0] == filter._itemTypes[0]; ;
    }

    public bool IsUnfiltered => _itemTypes.Length == 1 && _itemTypes[0] == WILDCARD;

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
            if (itemType == EMPTY)
            {
                // Rule: Skip itemType == 0 (empty items)
                continue;
            }
            else if (itemType == WILDCARD)
            {
                hasWildcard = true;
                validTypes.Add(WILDCARD);
            }
            else if (itemType > EMPTY)
            {
                validTypes.Add(itemType);
            }
            // Skip any other invalid values (< WILDCARD)
        }

        // Apply rules for WILDCARD (wildcard)
        if (hasWildcard)
        {
            // Rule: If WILDCARD is present, ALL input types must be WILDCARD
            if (validTypes.Count > 1)
            {
                throw new ArgumentException("When WILDCARD (wildcard) is provided, all item types must be WILDCARD. Mixed wildcard and specific types are not allowed.");
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

        // Invariant: If WILDCARD is present, it must be the only element
        if (_itemTypes.Length == 1)
        {
            if (_itemTypes[0] == WILDCARD || _itemTypes[0] > EMPTY)
            {
                return; // Valid
            }
        }

        // Invariant: No element can be 0
        // Invariant: If WILDCARD is present, it must be the only element
        for (int i = 0; i < _itemTypes.Length; i++)
        {
            int itemType = _itemTypes[i];

            if (itemType == EMPTY)
            {
                throw new InvalidOperationException("UniqueItemTypes cannot contain 0 (empty item type)");
            }
            else if (itemType < WILDCARD)
            {
                throw new InvalidOperationException($"UniqueItemTypes cannot contain invalid item types (< WILDCARD). Found {itemType} at index {i}");
            }
            else if (itemType == WILDCARD && i != EMPTY)
            {
                throw new InvalidOperationException("When WILDCARD (wildcard) is present, it must be the only element");
            }

            // No need to check for duplicates here, as we use a HashSet in the constructor

            if (itemType > EMPTY)
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

    public int GetSingleType()
    {
        return Count == 1 ? _itemTypes[0] : EMPTY;
    }

    public bool Contains(int itemType)
    {
        if (itemType == EMPTY)
        {
            // Rule: itemType 0 is never contained
            return false;
        }

        if (itemType == WILDCARD)
        {
            // Rule: WILDCARD is contained only if it's the sole element
            return _itemTypes.Length == 1 && _itemTypes[0] == WILDCARD;
        }

        if (itemType < EMPTY)
        {
            // Invalid item types (< WILDCARD) are never contained
            return false;
        }

        // Check for wildcard first (must be sole element and at index 0 after sorting)
        if (_itemTypes.Length == 1 && _itemTypes[0] == WILDCARD)
        {
            return true; // Wildcard matches any valid item type > 0
        }

        // Since _itemTypes is sorted and contains only positive integers (no WILDCARD case here),
        // we can use binary search for O(log n) performance
        return Array.BinarySearch(_itemTypes, itemType) >= 0;
    }

    /// <summary>
    /// Determines if the haystack filter can satisfy the needle filter.
    /// Returns true if the haystack data contains all item types needed by the needle filter.
    /// </summary>
    public static bool CanSatisfy(UniqueItemTypes haystack, UniqueItemTypes needle)
    {
        // Null filters cannot satisfy anything or be satisfied
        if (haystack == null || needle == null)
        {
            return false;
        }

        // If haystack is unfiltered [-1] or [*], it can satisfy any request
        if (haystack.IsUnfiltered)
        {
            return true; // Unfiltered cache has everything
        }

        // haystack is filtered, for example [1, 2, 3]
        // If needle is unfiltered [-1] or [*], it cannot be satisfied, because only unfiltered can provide "everything"
        if (needle.IsUnfiltered)
        {
            return false; // Filtered cache cannot provide "everything"
        }


        // Both are filtered - check if haystack contains all needle types
        // Since arrays are sorted, we can use a two-pointer approach
        int haystackIndex = 0;
        var haystackLength = haystack._itemTypes.Length;

        int needleIndex = 0;
        var needleLength = needle._itemTypes.Length;

        while (needleIndex < needleLength)
        {
            // If we've exhausted haystack types, we can't satisfy the rest
            if (haystackIndex >= haystackLength)
            {
                return false;
            }

            int haystackType = haystack._itemTypes[haystackIndex];
            int needleType = needle._itemTypes[needleIndex];

            if (haystackType == needleType)
            {
                // Found a match, advance both pointers
                haystackIndex++;
                needleIndex++;
            }
            else if (haystackType < needleType)
            {
                // Haystack type is smaller, advance haystack pointer
                haystackIndex++;
            }
            else
            {
                // Needle type is smaller and not in haystack, cannot satisfy
                return false;
            }
        }

        // All needle types were found in haystack
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

            if (itemType > EMPTY)
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

        // This has to be an instantiated itemValue, so it cannot be WILDCARD, and if it does, it's not valid
        if (itemValue.type <= EMPTY)
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

    public override string ToString() => GetDiagnosticInfo();

    public string GetDiagnosticInfo()
    {
        var info = $"Filter({_itemTypes.Length})";
        var details = string.Join(", ", _itemTypes.Select(itemType => ItemTypeNameLookupCache.GetItemTypeName(itemType)));

        return $"{info}:[{details}]";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current UniqueItemTypes instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance</param>
    /// <returns>True if the specified object is equal to the current instance; otherwise, false</returns>
    public override bool Equals(object obj)
    {
        return Equals(obj as UniqueItemTypes);
    }

    /// <summary>
    /// Determines whether the specified UniqueItemTypes instance is equal to the current instance.
    /// Two UniqueItemTypes instances are equal if they contain the same set of item types.
    /// </summary>
    /// <param name="other">The UniqueItemTypes instance to compare with the current instance</param>
    /// <returns>True if the instances are equal; otherwise, false</returns>
    public bool Equals(UniqueItemTypes other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Quick check: if lengths differ, they can't be equal
        if (_itemTypes.Length != other._itemTypes.Length)
        {
            return false;
        }

        // Since both arrays are guaranteed to be sorted (from constructor),
        // we can do element-by-element comparison for efficiency
        for (int i = 0; i < _itemTypes.Length; i++)
        {
            if (_itemTypes[i] != other._itemTypes[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a hash code for the current UniqueItemTypes instance.
    /// </summary>
    /// <returns>A hash code for the current instance</returns>
    public override int GetHashCode()
    {
        if (_itemTypes == null || _itemTypes.Length == 0)
        {
            return 0;
        }

        // Use a simple but effective hash combining algorithm
        // Since arrays are sorted, order matters for the hash
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < _itemTypes.Length; i++)
            {
                hash = hash * 31 + _itemTypes[i];
            }
            return hash;
        }
    }

    /// <summary>
    /// Determines whether two UniqueItemTypes instances are equal.
    /// </summary>
    /// <param name="left">The first instance to compare</param>
    /// <param name="right">The second instance to compare</param>
    /// <returns>True if the instances are equal; otherwise, false</returns>
    public static bool operator ==(UniqueItemTypes left, UniqueItemTypes right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two UniqueItemTypes instances are not equal.
    /// </summary>
    /// <param name="left">The first instance to compare</param>
    /// <param name="right">The second instance to compare</param>
    /// <returns>True if the instances are not equal; otherwise, false</returns>
    public static bool operator !=(UniqueItemTypes left, UniqueItemTypes right)
    {
        return !(left == right);
    }
}