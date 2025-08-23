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

        s_callStats.StartTiming(d_MethodName);
        try
        {
            var stats = s_callStats.GetMethodStats(d_MethodName);
            long callCount = stats?.callCount ?? 0;
            Update_OpenWorkstations(d_MethodName, callCount + 1);
        }
        finally
        {
            var elapsedUs = s_callStats.StopAndRecordCall(d_MethodName);
            var stats = s_callStats.GetMethodStats(d_MethodName);

            if (stats.HasValue)
            {
                var (callCount, totalTimeUs, avgTimeUs) = stats.Value;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: completed call {callCount} in {PerformanceProfiler.FormatMicroseconds(elapsedUs)} (avg: {PerformanceProfiler.FormatMicroseconds(avgTimeUs)})");
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

        s_callStats.StartTiming(d_MethodName);
        try
        {
            var stats = s_callStats.GetMethodStats(d_MethodName);
            long callCount = stats?.callCount ?? 0;

            Update_OpenWorkstations(d_MethodName, callCount + 1);
        }
        finally
        {
            // Stop timing and record the call
            var elapsedUs = s_callStats.StopAndRecordCall(d_MethodName);
            var stats = s_callStats.GetMethodStats(d_MethodName);

            if (stats.HasValue)
            {
                var (callCount, totalTimeUs, avgTimeUs) = stats.Value;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: completed call {callCount} in {PerformanceProfiler.FormatMicroseconds(elapsedUs)} (avg: {PerformanceProfiler.FormatMicroseconds(avgTimeUs)})");
#endif
            }
        }
    }

    internal static void Update_OpenWorkstations(string callType, long callCount)
    {
        string methodName = $"{callType}.{nameof(Update_OpenWorkstations)}";
        s
        // This check HAS to be done first, as StorageContextFactory.Create will return null if the world does not exist.
        if (!WorldTools.IsWorldExists())
        {
            return;
        }

        if (!ValidationHelper.ValidateStorageContextWithFeature(methodName, config => config.PullFromWorkstationOutputs, out StorageContext context))
        {
            // If we don't pull from outputs, we don't need to update the workstation windows,
            // because nothing that was crafted will be available or affect the UI.
            return;
        }

        // Use the new UIRefreshHelper to validate and refresh UI
        if (!UIRefreshHelper.ValidateAndRefreshUI(context, methodName))
        {
            return;
        }

        RefreshOpenWorkstationRecipeLists(context, methodName, callCount);
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
    }
}