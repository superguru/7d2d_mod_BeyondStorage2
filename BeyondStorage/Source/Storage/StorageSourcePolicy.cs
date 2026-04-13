using System;
using System.Collections.Generic;

namespace BeyondStorage.Source.Storage;

/// <summary>
/// Provides immutable allowed source lists for each storage operation type.
/// Each list is built once at first access and never changes.
/// </summary>
internal static class StorageSourcePolicy
{

    /// <summary>Gets the allowed source types for smart push operations.</summary>
    internal static AllowedSourcesList SmartPushSources { get; } = BuildSmartPushSources();

    /// <summary>Gets the allowed source types for smart loadout pull operations.</summary>
    internal static AllowedSourcesList SmartLoadoutPullSources { get; } = BuildSmartLoadoutPullSources();

    private static AllowedSourcesList BuildSmartPushSources()
    {
        var types = new List<Type>
        {
            typeof(ITileEntityLootable),
        };

        return new AllowedSourcesList(types);
    }

    private static AllowedSourcesList BuildSmartLoadoutPullSources()
    {
        var types = new List<Type>
        {
            typeof(TileEntityWorkstation),
            typeof(ITileEntityLootable),
        };

        return new AllowedSourcesList(types);
    }
}