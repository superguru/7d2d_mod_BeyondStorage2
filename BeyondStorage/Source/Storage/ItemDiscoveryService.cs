using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Service responsible for discovering storage sources (tile entities and entities) based on configuration and accessibility.
/// The goal is to find and register all available items from storage sources within range.
/// </summary>
public static class ItemDiscoveryService
{
    // Static call counter and lock object for diagnostic logging
    private static long s_callCounter = 0;
    private static readonly object s_lockObject = new();

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

        // Discover from tile entities (containers, workstations, dew collectors)
        TileEntityItemDiscovery.FindItems(context);

        // Discover from entities (vehicles and drones) via World.Entities.list iteration
        EntityItemDiscovery.FindItems(context);

        LogDiscoveryDiagnostics(context, d_MethodName);
    }

    private static void LogDiscoveryDiagnostics(StorageContext context, string methodName)
    {
        long currentCall;
        bool shouldLog;

        // Thread-safe increment of call counter and check for logging threshold
        lock (s_lockObject)
        {
            s_callCounter++;
            currentCall = s_callCounter;
            shouldLog = currentCall == 1 || currentCall % 100 == 0;
        }

        // Log call #1 and every 100th call
        if (shouldLog)
        {
            var info = context?.Sources?.DataStore?.GetDiagnosticInfo() ?? "null in context param chain";
            ModLogger.DebugLog($"{methodName}: Call #{currentCall}: {info}");
        }
    }

    private static bool ValidateParameters(string methodName, StorageContext context)
    {
        const string d_MethodName = nameof(ValidateParameters);

        if (!StorageContextFactory.EnsureValidContext(context, methodName))
        {
            ModLogger.DebugLog($"{d_MethodName}.{methodName}: context is not valid");
            return false;
        }

        return true;
    }
}