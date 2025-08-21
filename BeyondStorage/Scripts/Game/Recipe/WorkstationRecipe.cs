using BeyondStorage.HarmonyPatches.Informatics;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using BeyondStorage.Scripts.UI;

namespace BeyondStorage.Scripts.Game.Recipe;

public static class WorkstationRecipe
{
    // Use PerformanceProfiler for tracking call performance
    private static readonly PerformanceProfiler s_callStats = new("WorkstationRecipe");

    /// <summary>
    /// This is called when the recipe finishes crafting on a workstation TE that is NOT open on a player screen
    /// </summary>
    public static void BackgroundWorkstation_CraftCompleted()
    {
        const string d_MethodName = nameof(BackgroundWorkstation_CraftCompleted);

        // Start timing the method call
        s_callStats.StartTiming(d_MethodName);

        try
        {
            var stats = s_callStats.GetMethodStats(d_MethodName);
            long callCount = stats?.callCount ?? 0;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: starting call {callCount + 1}");
#endif
            Update_OpenWorkstations(d_MethodName, callCount + 1);
        }
        finally
        {
            // Stop timing and record the call
            var elapsedNs = s_callStats.StopAndRecordCall(d_MethodName);
            var stats = s_callStats.GetMethodStats(d_MethodName);

            if (stats.HasValue)
            {
                var (callCount, totalTimeNs, avgTimeNs) = stats.Value;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: completed call {callCount} in {PerformanceProfiler.FormatNanoseconds(elapsedNs)} (avg: {PerformanceProfiler.FormatNanoseconds(avgTimeNs)})");
#endif
            }
        }
    }

    /// <summary>
    /// Called when the recipe finishes crafting on the currently opened workstation window
    /// </summary>
    public static void ForegroundWorkstation_CraftCompleted()
    {
        const string d_MethodName = nameof(ForegroundWorkstation_CraftCompleted);

        // Start timing the method call
        s_callStats.StartTiming(d_MethodName);

        try
        {
            var stats = s_callStats.GetMethodStats(d_MethodName);
            long callCount = stats?.callCount ?? 0;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: starting call {callCount + 1}");
#endif
            Update_OpenWorkstations(d_MethodName, callCount + 1);
        }
        finally
        {
            // Stop timing and record the call
            var elapsedNs = s_callStats.StopAndRecordCall(d_MethodName);
            var stats = s_callStats.GetMethodStats(d_MethodName);

            if (stats.HasValue)
            {
                var (callCount, totalTimeNs, avgTimeNs) = stats.Value;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: completed call {callCount} in {PerformanceProfiler.FormatNanoseconds(elapsedNs)} (avg: {PerformanceProfiler.FormatNanoseconds(avgTimeNs)})");
#endif
            }
        }
    }

    internal static void Update_OpenWorkstations(string callType, long callCount)
    {
        string d_MethodName = $"{callType}.{nameof(Update_OpenWorkstations)}";
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: starting call {callCount}");
#endif

        // This check HAS to be done first, as StorageContextFactory.Create will return null if the world does not exist.
        if (!WorldTools.IsWorldExists())
        {
            return;
        }

        if (!ValidationHelper.ValidateStorageContextWithFeature(d_MethodName, config => config.PullFromWorkstationOutputs, out StorageContext context))
        {
            // If we don't pull from outputs, we don't need to update the workstation windows,
            // because nothing that was crafted will be available or affect the UI.
            return;
        }

        // Use the new UIRefreshHelper to validate and refresh UI
        if (!UIRefreshHelper.ValidateAndRefreshUI(context, d_MethodName))
        {
            return;
        }

        RefreshOpenWorkstationRecipeLists(context, d_MethodName, callCount);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Player inventory changed in call {callCount}");
#endif
    }

    private static void RefreshOpenWorkstationRecipeLists(StorageContext context, string methodName, long callCount)
    {
        var worldPlayerContext = context?.WorldPlayerContext;
        if (worldPlayerContext == null)
        {
            ModLogger.DebugLog($"{methodName}: WorldPlayerContext is null in call {callCount}.");
            return;
        }

        // Get the currently active workstation instead of querying all open windows
        var activeWorkstation = XUiC_WorkstationWindowGroup_Patches.GetCurrentlyActiveWorkstation();

        if (activeWorkstation == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: No active workstation window in call {callCount}");
#endif
            return; // Nothing to update - exit early
        }

        var recipeList = activeWorkstation.recipeList;
        if (recipeList == null || recipeList.recipeControls == null)
        {
            ModLogger.DebugLog($"{methodName}: Recipe list or controls are null for active workstation in call {callCount}. Skipping updates.");
            return;
        }

        activeWorkstation.syncUIfromTE();

        recipeList.PlayerInventory_OnBackpackItemsChanged();
        activeWorkstation.craftInfoWindow?.ingredientList?.PlayerInventory_OnBackpackItemsChanged();

#if DEBUG
        ModLogger.DebugLog($"{methodName}: Refreshed active workstation in call {callCount}");
#endif
    }
}