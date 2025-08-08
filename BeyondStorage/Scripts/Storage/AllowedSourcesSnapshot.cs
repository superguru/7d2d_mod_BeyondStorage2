using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Diagnostics;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Captures the allowed storage source types for a given configuration snapshot.
/// </summary>
internal sealed class AllowedSourcesSnapshot
{
    private readonly List<Type> _allowSourceTypes = [];

    private AllowedSourcesSnapshot(ConfigSnapshot config)
    {
        if (config == null)
        {
            var error = $"{nameof(AllowedSourcesSnapshot)}: {nameof(config)} cannot be null.";
            ModLogger.DebugLog(error);

            throw new ArgumentNullException(nameof(config), error);
        }

        // The order is important

        // Drones
        if (config.PullFromDrones)
        {
            _allowSourceTypes.Add(typeof(EntityDrone));
        }

        // Dew Collectors
        if (config.PullFromDewCollectors)
        {
            _allowSourceTypes.Add(typeof(TileEntityDewCollector));
        }

        // Workstations
        if (config.PullFromWorkstationOutputs)
        {
            _allowSourceTypes.Add(typeof(TileEntityWorkstation));
        }

        // Lootables: Always allowed
        _allowSourceTypes.Add(typeof(ITileEntityLootable));

        // Vehicles
        if (config.PullFromVehicleStorage)
        {
            _allowSourceTypes.Add(typeof(EntityVehicle));
        }
    }

    public bool IsAllowedSource(Type sourceType)
    {
        return TypeMatchingHelper.IsMatchingType(sourceType, _allowSourceTypes);
    }

    public IReadOnlyList<Type> GetAllowedSourceTypes()
    {
        return _allowSourceTypes.AsReadOnly();
    }

    public static AllowedSourcesSnapshot FromConfig(ConfigSnapshot config)
    {
        var snap = new AllowedSourcesSnapshot(config);
        return snap;
    }

    /// <summary>
    /// Gets diagnostic information about the allowed storage sources.
    /// </summary>
    /// <returns>String containing diagnostic information about allowed source types</returns>
    public string GetDiagnosticInfo()
    {
        var totalTypes = _allowSourceTypes.Count;
        var typeDetails = string.Join(", ", _allowSourceTypes.Select(type => NameLookups.GetAbbrev(type)));

        return $"[AllowedSources] Types: {totalTypes} [{typeDetails}]";
    }
}