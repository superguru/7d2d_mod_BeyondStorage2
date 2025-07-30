using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.ContainerLogic.Item;
using BeyondStorage.Scripts.Utils;
using static XUiC_CraftingInfoWindow;

namespace BeyondStorage.Scripts.ContainerLogic.Recipe;
public static class WorkstationRecipe
{
    // Tracks call count and last call time (ms) for each call type
    private static readonly Dictionary<string, (int callCount, int callTime)> s_callStats = new();

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

        // Increment call count and update stats
        if (!s_callStats.TryGetValue(d_MethodName, out var stats))
        {
            stats = (0, 0);
        }

        stats.callCount++;
        s_callStats[d_MethodName] = stats;

        LogUtil.DebugLog($"{d_MethodName} called {stats.callCount} times");

        Update_OpenWorkstations(d_MethodName, stats.callCount);
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

        // Increment call count and update stats
        if (!s_callStats.TryGetValue(d_MethodName, out var stats))
        {
            stats = (0, 0);
        }

        stats.callCount++;
        s_callStats[d_MethodName] = stats;

        LogUtil.DebugLog($"{d_MethodName} Starting call {stats.callCount}");

        Update_OpenWorkstations(d_MethodName, stats.callCount);
    }

    private static void Update_OpenWorkstations(string callType, int callCount)
    {
        // Start timing for total execution
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        string d_MethodName = string.Concat(callType, ".", nameof(Update_OpenWorkstations));

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
            stopwatch.Stop();
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

        // Stop timing and log the total execution time
        stopwatch.Stop();
        int elapsedMs = (int)stopwatch.ElapsedMilliseconds;
        LogUtil.DebugLog($"{d_MethodName} Total execution time in call {callCount}: {elapsedMs}ms");

        // Update call time in stats dictionary
        if (s_callStats.TryGetValue(callType, out var stats))
        {
            s_callStats[callType] = (stats.callCount, elapsedMs);
        }
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
