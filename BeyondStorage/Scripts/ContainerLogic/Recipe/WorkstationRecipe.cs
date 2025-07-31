using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.ContainerLogic.Item;
using BeyondStorage.Scripts.Utils;
using static XUiC_CraftingInfoWindow;

namespace BeyondStorage.Scripts.ContainerLogic.Recipe;
public static class WorkstationRecipe
{
    // Use MethodCallStatistics for tracking call performance
    private static readonly MethodCallStatistics s_callStats = new("WorkstationRecipe");

    /// <summary>
    /// This is called when the recipe finishes crafting on a workstation TE that is NOT open on a player screen
    /// </summary>
    public static void BackgroundWorkstation_CraftCompleted()
    {
        if (!ModConfig.PullFromWorkstationOutputs())
        {
            return;
        }

        const string d_MethodName = nameof(BackgroundWorkstation_CraftCompleted);

        // Start timing the method call
        s_callStats.StartTiming(d_MethodName);

        try
        {
            var stats = s_callStats.GetMethodStats(d_MethodName);
            var callCount = stats?.callCount ?? 0;
            LogUtil.DebugLog($"{d_MethodName} starting call {callCount + 1}");

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
                LogUtil.DebugLog($"{d_MethodName} completed call {callCount} in {MethodCallStatistics.FormatNanoseconds(elapsedNs)} (avg: {MethodCallStatistics.FormatNanoseconds(avgTimeNs)})");
            }
        }
    }

    /// <summary>
    /// Called when the recipe finishes crafting on the currently opened workstation window
    /// </summary>
    public static void ForegroundWorkstation_CraftCompleted()
    {
        if (!ModConfig.PullFromWorkstationOutputs())
        {
            return;
        }

        const string d_MethodName = nameof(ForegroundWorkstation_CraftCompleted);

        // Start timing the method call
        s_callStats.StartTiming(d_MethodName);

        try
        {
            var stats = s_callStats.GetMethodStats(d_MethodName);
            var callCount = stats?.callCount ?? 0;
            LogUtil.DebugLog($"{d_MethodName} starting call {callCount + 1}");

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
                LogUtil.DebugLog($"{d_MethodName} completed call {callCount} in {MethodCallStatistics.FormatNanoseconds(elapsedNs)} (avg: {MethodCallStatistics.FormatNanoseconds(avgTimeNs)})");
            }
        }
    }

    private static void Update_OpenWorkstations(string callType, int callCount)
    {
        string d_MethodName = string.Concat(callType, ".", nameof(Update_OpenWorkstations));

        // Start timing this internal method
        s_callStats.StartTiming(d_MethodName);

        try
        {
            var worldPlayerContext = WorldPlayerContext.TryCreate(d_MethodName);
            if (worldPlayerContext == null)
            {
                LogUtil.Error($"{d_MethodName}: Failed to create WorldPlayerContext in call {callCount}");
                return;
            }

            var player = worldPlayerContext.Player;
            var xui = player.PlayerUI.xui;
            if (xui == null)
            {
                LogUtil.Error($"{d_MethodName}: xui is null in call {callCount}");
                return;
            }

            // Get only open workstation windows (avoids unnecessary filtering later)
            var openWorkstations = xui.GetChildrenByType<XUiC_WorkstationWindowGroup>()
                .Where(w => w.WindowGroup?.isShowing ?? false)
                .ToList();

            if (openWorkstations.Count == 0)
            {
                return; // Nothing to update - exit early
            }

            // Get all available items once (shared across all workstations)
            var availableItems = ItemCommon.ItemCommon_GetAllAvailableItemStacksFromXui(xui);
            if (availableItems.Count == 0)
            {
                return;  // Interesting...
            }

            // Process each workstation
            foreach (var workstation in openWorkstations)
            {
                var recipeList = workstation.recipeList;
                if (recipeList == null || recipeList.recipeControls == null)
                {
                    continue;
                }

                // Store UI state
                var stateInfo = CaptureWorkstationState(workstation, recipeList);

                // Dictionary to quickly look up recipe infos by recipe
                var recipeInfoLookup = recipeList.recipeInfos?.ToDictionary(
                    ri => ri.recipe,
                    ri => ri,
                    new RecipeEqualityComparer()) ?? new Dictionary<global::Recipe, XUiC_RecipeList.RecipeInfo>(new RecipeEqualityComparer());

                int refreshCount = 0;
                var craftingWindow = recipeList.craftingWindow;
                bool hasCraftingWindow = (craftingWindow != null);

                // Only process recipe entries that could potentially change (even those with HasIngredients == true can change because more ingredients might be available)
                var recipesToCheck = recipeList.recipeControls
                    .Where(entry => entry?.Recipe != null)
                    .ToList();

                if (recipesToCheck.Count == 0)
                {
                    continue; // No recipes need updating in this workstation
                }

                foreach (var recipeEntry in recipesToCheck)
                {
                    var recipe = recipeEntry.Recipe;

                    // Skip validation if recipe info is missing
                    if (!recipeInfoLookup.TryGetValue(recipe, out var recipeInfo) || string.IsNullOrEmpty(recipeInfo.name))
                    {
                        continue;
                    }

                    // Check if crafting requirements are valid
                    bool craftingValid = hasCraftingWindow && craftingWindow.CraftingRequirementsValid(recipeInfo.recipe);
                    bool hasIngredientsNow = XUiM_Recipes.HasIngredientsForRecipe(availableItems, recipe, player);

                    // Only check ingredients if crafting is valid
                    if (craftingValid && hasIngredientsNow)
                    {
                        if (recipeEntry.HasIngredients ^ hasIngredientsNow)
                        {
                            recipeEntry.HasIngredients = hasIngredientsNow;  // This will cause the crafting info page to reset to Ingredients tab

                            if (hasIngredientsNow)
                            {
                                stateInfo.SelectedEntryBecameEnabled = true;
                                recipeList.resortRecipes = true; // This recipe has been enabled now, and we need to resort the recipes
                            }

                            recipeEntry.isDirty = true;
                            refreshCount++;
                        }
                    }
                }

                // Update UI only if changes were made
                if (refreshCount > 0)
                {
                    LogUtil.DebugLog($"{d_MethodName} refreshed {refreshCount} recipe controls in call {callCount} for workstation");
                    recipeList.IsDirty = true;
                    recipeList.CraftCount.IsDirty = true;

                    // Sync UI with tile entity
                    workstation.syncUIfromTE();

                    // Restore UI state
                    RestoreWorkstationState(workstation, recipeList, stateInfo);
                    recipeList.PlayerInventory_OnBackpackItemsChanged();
                }
            }
        }
        finally
        {
            // Stop timing and record the call
            var elapsedNs = s_callStats.StopAndRecordCall(d_MethodName);
            var stats = s_callStats.GetMethodStats(d_MethodName);

            if (stats.HasValue)
            {
                var (totalCallCount, totalTimeNs, avgTimeNs) = stats.Value;
                LogUtil.DebugLog($"{d_MethodName} completed in {MethodCallStatistics.FormatNanoseconds(elapsedNs)} (call {totalCallCount}, avg: {MethodCallStatistics.FormatNanoseconds(avgTimeNs)})");
            }
        }
    }

    /// <summary>
    /// Gets comprehensive call statistics for debugging and monitoring.
    /// </summary>
    /// <returns>Formatted string with all call statistics</returns>
    public static string GetCallStatistics()
    {
        return s_callStats.GetFormattedStatistics();
    }

    /// <summary>
    /// Gets detailed statistics for a specific method.
    /// </summary>
    /// <param name="methodName">Name of the method to get stats for</param>
    /// <returns>Method statistics or null if not found</returns>
    public static (int callCount, long totalTimeNs, double avgTimeNs)? GetMethodStats(string methodName)
    {
        return s_callStats.GetMethodStats(methodName);
    }

    /// <summary>
    /// Clears all call statistics. Useful for testing or resetting metrics.
    /// </summary>
    public static void ClearStatistics()
    {
        s_callStats.Clear();
        LogUtil.DebugLog("WorkstationRecipe: Call statistics cleared");
    }

    // Helper class to store workstation state
    private class WorkstationState
    {
        public int CurrentPage { get; set; }
        public XUiC_RecipeEntry SelectedEntry { get; set; }
        public bool SelectedEntryBecameEnabled { get; set; }
        public TabTypes CraftInfoTabType { get; set; }
    }

    // Helper method to capture workstation state
    private static WorkstationState CaptureWorkstationState(XUiC_WorkstationWindowGroup workstation, XUiC_RecipeList recipeList)
    {
        var craftInfoWindow = workstation?.craftInfoWindow;

        return new WorkstationState
        {
            CurrentPage = recipeList.Page,
            SelectedEntry = recipeList.SelectedEntry,
            SelectedEntryBecameEnabled = false,
            CraftInfoTabType = (craftInfoWindow != null) ? craftInfoWindow.TabType : TabTypes.Ingredients
        };
    }

    // Helper method to restore workstation state
    private static void RestoreWorkstationState(XUiC_WorkstationWindowGroup workstation, XUiC_RecipeList recipeList, WorkstationState stateInfo)
    {
        if (stateInfo.SelectedEntryBecameEnabled)
        {
            recipeList.SelectedEntry = stateInfo.SelectedEntry;
        }
        //else
        {
            if (recipeList.Page != stateInfo.CurrentPage)
            {
                recipeList.Page = stateInfo.CurrentPage;

                var craftInfoWindow = workstation.craftInfoWindow;
                if (stateInfo.SelectedEntry != null && craftInfoWindow != null)
                {
                    if (craftInfoWindow.TabType != stateInfo.CraftInfoTabType)
                    {
                        craftInfoWindow.TabType = stateInfo.CraftInfoTabType;
                        craftInfoWindow.SetSelectedButtonByType(stateInfo.CraftInfoTabType);
                        craftInfoWindow.IsDirty = true;
                    }
                }
            }
        }
    }

    // Custom equality comparer for Recipe objects
    private class RecipeEqualityComparer : IEqualityComparer<global::Recipe>
    {
        public bool Equals(global::Recipe x, global::Recipe y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(global::Recipe obj)
        {
            return obj.GetHashCode();
        }
    }
}
