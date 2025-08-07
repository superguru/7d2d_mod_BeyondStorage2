using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using static XUiC_CraftingInfoWindow;

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

        if (WorldTools.IsServer())
        {
            return;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        if (!context.Config.PullFromWorkstationOutputs)
        {
            return;
        }

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

        if (WorldTools.IsServer())
        {
            return;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        if (!context.Config.PullFromWorkstationOutputs)
        {
            // If we don't pull from outputs, we don't need to update the workstation windows, as nothing that
            // was crafted will be available or affect the UI.
            return;
        }

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
        if (WorldTools.IsServer())
        {
            return;
        }

        string d_MethodName = $"{callType}.{nameof(Update_OpenWorkstations)}";

        // Start timing this internal method
        s_callStats.StartTiming(d_MethodName);

        try
        {
            var worldPlayerContext = WorldPlayerContext.TryCreate(d_MethodName);
            if (worldPlayerContext == null)
            {
                ModLogger.Error($"{d_MethodName}: Failed to create WorldPlayerContext in call {callCount}.");
                return;
            }

            var player = worldPlayerContext.Player;
            var xui = player.PlayerUI.xui;
            if (xui == null)
            {
                ModLogger.Error($"{d_MethodName}: xui is null in call {callCount}");
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

                if (!hasCraftingWindow)
                {
                    ModLogger.Warning($"{d_MethodName}: No crafting window found for workstation {workstation} in call {callCount}");
                    continue; // No crafting window means no recipes to check
                }

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
                    bool craftingValidNow = craftingWindow.CraftingRequirementsValid(recipeInfo.recipe);
                    bool hadIngredientsBefore = recipeEntry.HasIngredients;
                    bool hasIngredientsNow = XUiM_Recipes.HasIngredientsForRecipe(availableItems, recipe, player);
                    bool shouldHasIngredientsBeEnabled = craftingValidNow && hasIngredientsNow;

                    if (hadIngredientsBefore != hasIngredientsNow)
                    {
                        // This will cause the crafting info page to reset to Ingredients tab
                        recipeEntry.HasIngredients = shouldHasIngredientsBeEnabled;

                        // This recipe has been enabled or disabled now, and we need to resort the recipes
                        recipeList.resortRecipes = true;

                        recipeEntry.isDirty = true;
                        refreshCount++;
                    }
                }

                // Update UI only if changes were made
                if (refreshCount > 0)
                {
#if DEBUG
                    ModLogger.DebugLog($"{d_MethodName}: refreshed {refreshCount} recipe controls in call {callCount}");
#endif
                    recipeList.IsDirty = true;
                    recipeList.CraftCount.IsDirty = true;

                    // Sync UI with tile entity
                    workstation.syncUIfromTE();

                    // Restore UI state
                    RestoreWorkstationState(workstation, recipeList, stateInfo);
                    recipeList.PlayerInventory_OnBackpackItemsChanged();
                }

                workstation.craftInfoWindow.ingredientList.PlayerInventory_OnBackpackItemsChanged();
                ModLogger.DebugLog($"{d_MethodName}: Refreshed ingredients for workstation {workstation} in call {callCount}");
            }
        }
        finally
        {
            // Finally we do nothing
        }
    }

    // Helper class to store workstation state
    private class WorkstationState
    {
        public int CurrentPage { get; set; }
        public XUiC_RecipeEntry SelectedEntry { get; set; }
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
            CraftInfoTabType = (craftInfoWindow != null) ? craftInfoWindow.TabType : TabTypes.Ingredients
        };
    }

    // Helper method to restore workstation state
    private static void RestoreWorkstationState(XUiC_WorkstationWindowGroup workstation, XUiC_RecipeList recipeList, WorkstationState stateInfo)
    {
        if (GameManager.IsDedicatedServer)
        {
            return;
        }
        var craftInfoWindow = workstation.craftInfoWindow;

        if (recipeList.Page != stateInfo.CurrentPage)
        {
            recipeList.Page = stateInfo.CurrentPage;

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
