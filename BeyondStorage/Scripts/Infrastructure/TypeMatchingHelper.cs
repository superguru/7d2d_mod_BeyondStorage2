using System;
using System.Collections.Generic;

namespace BeyondStorage.Scripts.Infrastructure;

/// <summary>
/// Helper class for inheritance-aware type matching operations.
/// Provides reusable logic for matching types against collections with interface/inheritance support.
/// </summary>
public static class TypeMatchingHelper
{
    /// <summary>
    /// Finds the first matching type from a collection that is assignable from the target type.
    /// Uses exact match first (fast path), then inheritance/interface matching.
    /// </summary>
    /// <typeparam name="T">The type of values in the lookup collection</typeparam>
    /// <param name="targetType">The type to find a match for</param>
    /// <param name="typeLookup">Dictionary/collection to search in</param>
    /// <param name="exactMatch">The exact match result (if found)</param>
    /// <param name="inheritanceMatch">The first inheritance match result (if found)</param>
    /// <returns>True if any match was found</returns>
    public static bool TryFindMatch<T>(Type targetType, IReadOnlyDictionary<Type, T> typeLookup,
        out T exactMatch, out T inheritanceMatch)
    {
        exactMatch = default;
        inheritanceMatch = default;

        if (targetType == null || typeLookup == null)
        {
            return false;
        }

        // First check for exact type match (most common case)
        if (typeLookup.TryGetValue(targetType, out exactMatch))
        {
            return true;
        }

        // Check if the targetType implements any of the known interfaces or inherits from known classes
        foreach (var kvp in typeLookup)
        {
            var knownType = kvp.Key;
            if (knownType.IsAssignableFrom(targetType))
            {
                inheritanceMatch = kvp.Value;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds all matching values from a dictionary where the search type is assignable from the stored types.
    /// This is the inverse of TryFindMatch - useful when searching for interfaces/base classes
    /// that should match concrete implementations stored in the dictionary.
    /// </summary>
    /// <typeparam name="T">The type of values in the lookup collection</typeparam>
    /// <param name="searchType">The type to search for (often an interface or base class)</param>
    /// <param name="typeLookup">Dictionary containing concrete types as keys</param>
    /// <returns>List of all matching values</returns>
    public static List<T> FindAllAssignableMatches<T>(Type searchType, IReadOnlyDictionary<Type, T> typeLookup)
    {
        var results = new List<T>();

        if (searchType == null || typeLookup == null)
        {
            return results;
        }

        // First check for exact match
        if (typeLookup.TryGetValue(searchType, out var exactMatch))
        {
            results.Add(exactMatch);
        }

        // Check if any stored types implement the search interface or inherit from the search class
        foreach (var kvp in typeLookup)
        {
            var storedType = kvp.Key;
            var value = kvp.Value;

            // Skip if this is the exact match we already processed
            if (storedType == searchType)
            {
                continue;
            }

            // Check if the search type is assignable from the stored type
            // (i.e., stored type implements search interface or inherits from search class)
            if (searchType.IsAssignableFrom(storedType))
            {
                results.Add(value);
            }
        }

        return results;
    }

    /// <summary>
    /// Checks if a target type matches any type in a collection (exact or inheritance).
    /// </summary>
    public static bool IsMatchingType(Type targetType, IEnumerable<Type> typeCollection)
    {
        if (targetType == null || typeCollection == null)
        {
            return false;
        }

        foreach (var knownType in typeCollection)
        {
            // Exact match or inheritance match
            if (knownType == targetType || knownType.IsAssignableFrom(targetType))
            {
                return true;
            }
        }

        return false;
    }
}