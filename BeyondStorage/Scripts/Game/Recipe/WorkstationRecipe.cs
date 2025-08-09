using System.Linq;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

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
            var callCount = stats?.callCount ?? 0;
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
            var callCount = stats?.callCount ?? 0;
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

    private static void Update_OpenWorkstations(string callType, int callCount)
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

        if (context.WorldPlayerContext?.Player?.playerUI?.xui == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Required UI components are null");
            return;
        }

        // Now completely safe to access without null-conditional operators
        context.WorldPlayerContext.Player.playerUI.xui.RefreshAllWindows(_includeViewComponents: true);
        RefreshOpenRecipeLists(context, d_MethodName, callCount);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Player inventory changed in call {callCount}");
#endif
    }

    private static void RefreshOpenRecipeLists(StorageContext context, string d_MethodName, int callCount)
    {
        var worldPlayerContext = context?.WorldPlayerContext;
        if (worldPlayerContext == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: WorldPlayerContext is null in call {callCount}.");
            return;
        }

        var player = worldPlayerContext.Player;
        var xui = player?.PlayerUI?.xui;
        if (xui == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: xui is null in call {callCount}");
            return;
        }

        // Get only open workstation windows (avoids unnecessary filtering later)
        var openWorkstations = xui.GetChildrenByType<XUiC_WorkstationWindowGroup>()
            .Where(w => w?.WindowGroup?.isShowing ?? false)
            .ToList();

        if (openWorkstations.Count == 0)
        {
            return; // Nothing to update - exit early
        }

        int refreshCount = 0;
        foreach (var workstation in openWorkstations)
        {
            var recipeList = workstation.recipeList;
            if (recipeList == null || recipeList.recipeControls == null)
            {
                continue;
            }

            workstation.syncUIfromTE();

            recipeList.PlayerInventory_OnBackpackItemsChanged();
            workstation.craftInfoWindow?.ingredientList?.PlayerInventory_OnBackpackItemsChanged();

            ModLogger.DebugLog($"{d_MethodName}: Refreshed workstation {workstation} (#{++refreshCount} in call {callCount}");
        }
    }
}