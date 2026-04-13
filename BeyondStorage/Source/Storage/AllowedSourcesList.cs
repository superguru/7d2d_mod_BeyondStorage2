using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Source.Diagnostics;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.Storage;

/// <summary>
/// Captures the allowed storage source types for a given configuration snapshot.
/// </summary>
internal sealed class AllowedSourcesList
{
    private readonly List<Type> _allowSourceTypes;

    public AllowedSourcesList(IEnumerable<Type> allowedSourceTypes)
    {
        if (allowedSourceTypes == null)
        {
            throw new ArgumentNullException(nameof(allowedSourceTypes));
        }

        _allowSourceTypes = allowedSourceTypes.ToList();
    }

    public bool IsAllowedSource(Type sourceType)
    {
        return TypeMatchingHelper.IsMatchingType(sourceType, _allowSourceTypes);
    }

    public IReadOnlyList<Type> GetAllowedSourceTypes()
    {
        return _allowSourceTypes.AsReadOnly();
    }

    /// <summary>
    /// Gets diagnostic information about the allowed storage sources.
    /// </summary>
    /// <returns>String containing diagnostic information about allowed source types</returns>
    public string GetDiagnosticInfo()
    {
        var totalTypes = _allowSourceTypes.Count;
        var typeDetails = string.Join(", ", _allowSourceTypes.Select(type => TypeNames.GetAbbrev(type)));

        return $"[AllowedSources] Types: {totalTypes} [{typeDetails}]";
    }
}