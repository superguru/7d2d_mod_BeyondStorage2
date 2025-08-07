using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Service responsible for discovering storage sources (tile entities and vehicles) based on configuration and accessibility.
/// The goal is to find and register all available items from storage sources within range.
/// </summary>
public static class ItemDiscoveryService
{
    /// <summary>
    /// Discovers all available storage sources within range and registers them with the context.
    /// </summary>
    /// <param name="context">The storage context containing configuration and data store</param>
    public static void DiscoverItems(StorageContext context)
    {
        const string d_MethodName = nameof(DiscoverItems);

        if (!ValidateParameters(d_MethodName, context))
        {
            return;
        }

        TileEntityItemDiscovery.FindItems(context);
        VehicleItemDiscovery.FindItems(context);
        DroneItemDiscovery.FindItems(context);

        LogDiscoveryDiagnostics(context, d_MethodName);
    }

    private static void LogDiscoveryDiagnostics(StorageContext context, string methodName)
    {
        var info = context.Sources.DataStore.GetDiagnosticInfo();
        ModLogger.DebugLog($"{methodName}: {info}");
    }

    private static bool ValidateParameters(string methodName, StorageContext context)
    {
        if (context == null)
        {
            ModLogger.Error($"{methodName}: context is null");
            return false;
        }

        return true;
    }
}