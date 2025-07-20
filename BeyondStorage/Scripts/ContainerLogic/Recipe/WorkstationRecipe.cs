using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.ContainerLogic.Item;
using BeyondStorage.Scripts.Utils;
using static XUiC_CraftingInfoWindow;

namespace BeyondStorage.Scripts.ContainerLogic.Recipe;
public static class WorkstationRecipe
{
    private static int s_bg_calls = 0;
    private static int s_curr_calls = 0;

    /// <summary>
    /// This is called when the recipe finishes crafting on a workstation TE that is NOT open on a player screen
    /// </summary>
    public static void BackgroundWorkstationCraftCompleted()
    {
        if (!ModConfig.PullFromWorkstationOutputs())
        {
            // If the config is set to not pull from workstation outputs, we don't need to refresh the windows
            return;
        }

        string d_MethodName = "BackgroundWorkstationCraftComplete";
        s_bg_calls++;

        LogUtil.DebugLog($"{d_MethodName} called {s_bg_calls} times");

        RefreshOpenWorkstationWindows(d_MethodName, s_bg_calls);
    }

    /// <summary>
    /// Called when the recipe finishes crafting on the currently opened workstation window
    /// </summary>
    public static void CurrentWorkstationCraftCompleted()
    {
        if (!ModConfig.PullFromWorkstationOutputs())
        {
            // If the config is set to not pull from workstation outputs, we don't need to refresh the windows
            return;
        }

        string d_MethodName = "CurrentWorkstationCraftCompleted";
        s_curr_calls++;

        LogUtil.DebugLog($"{d_MethodName} called {s_curr_calls} times");

        RefreshOpenWorkstationWindows(d_MethodName, s_curr_calls);
    }

    private static void RefreshOpenWorkstationWindows(string d_MethodName, int callCount)
    {
        var player = GameManager.Instance.World?.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.Error($"{d_MethodName}: primary player is null in call {callCount}");
            return;
        }

        var openWindows = player.windowManager?.windows.Where(w => w.isShowing);
        if (openWindows == null)
        {
            LogUtil.Error($"{d_MethodName}: openWindows is null in call {callCount}");
            return;
        }

        foreach (var win in openWindows)
        {
            if (win is not XUiWindowGroup wg || wg.Controller == null)
            {
                continue;
            }

            if (wg.Controller is not XUiC_WorkstationWindowGroup workstation)
            {
                continue;
            }

            var recipeList = workstation.recipeList;
            if (recipeList == null)
            {
                LogUtil.DebugLog($"{d_MethodName} recipeList is null in call {callCount}, so not updating recipe list");
                continue;
            }

            var availableItems = ItemCraft.ItemCraftGetAllAvailableItemStacks(recipeList.xui);

            LogUtil.DebugLog($"{d_MethodName} Refreshing the recipes for open workstation in call {callCount}");

            // Save state
            var currPage = recipeList.Page;
            var selectedRecipe = recipeList.SelectedEntry;
            var craftInfoWindow = workstation.craftInfoWindow;
            var craftInfoTabType = selectedRecipe != null && craftInfoWindow != null
                ? craftInfoWindow.TabType
                : TabTypes.Ingredients;

            // Refresh recipe controls
            int refreshCount = 0;
            foreach (var recipeEntry in recipeList.recipeControls)
            {
                if (recipeEntry.Recipe == null || recipeEntry.HasIngredients)
                {
                    continue;
                }

                LogUtil.DebugLog($"{d_MethodName} recipeControl.Recipe for {recipeEntry.recipe.GetName()} is doesn't have the ingredients, so re-checking them");

                var recipe = recipeEntry.Recipe;
                var recipeInfo = recipeList.recipeInfos.FirstOrDefault(r => r.recipe.GetName() == recipe.GetName());
                if (string.IsNullOrEmpty(recipeInfo.name))
                {
                    LogUtil.Error($"{d_MethodName} recipeInfo for {recipe.GetName()} not found in call {callCount}, so not refreshing it");
                    continue;
                }

                LogUtil.DebugLog($"{d_MethodName} re-checking ingredients for recipe {recipe.GetName()} in call {callCount}");
                var hasIngredients = XUiM_Recipes.HasIngredientsForRecipe(availableItems, recipeInfo.recipe, player)
                    && (recipeList.craftingWindow == null || recipeList.craftingWindow.CraftingRequirementsValid(recipeInfo.recipe));
                if (hasIngredients)
                {
                    LogUtil.DebugLog($"{d_MethodName} recipeInfo for {recipe.GetName()} has ingredients now in call {callCount}. Updating recipeEntry");
                    recipeEntry.HasIngredients = true;
                    refreshCount++;
                }
            }

            if (refreshCount > 0)
            {
                LogUtil.DebugLog($"{d_MethodName} refreshed {refreshCount} recipe controls in call {callCount}");
                recipeList.IsDirty = true;
                recipeList.CraftCount.IsDirty = true;
            }
            else
            {
                LogUtil.DebugLog($"{d_MethodName} no recipe controls needed refreshing in call {callCount}");
            }

            LogUtil.DebugLog($"{d_MethodName} syncing workstation UI from TE in call {callCount}");
            workstation.syncUIfromTE();

            // Restore state
            LogUtil.DebugLog($"{d_MethodName} restoring the current page {currPage} in call {callCount}");
            recipeList.Page = currPage;
            recipeList.PlayerInventory_OnBackpackItemsChanged();

            if (selectedRecipe != null)
            {
                var newSelectedRecipe = recipeList.recipeControls
                    .FirstOrDefault(r => r.Recipe.GetName() == selectedRecipe.Recipe.GetName());

                if (newSelectedRecipe != null)
                {
                    LogUtil.DebugLog($"{d_MethodName} restoring the selected recipe {selectedRecipe.Recipe.GetName()} in call {callCount}");
                    recipeList.SelectedEntry = newSelectedRecipe;
                    recipeList.CraftCount.IsDirty = true;
                }
                else
                {
                    LogUtil.DebugLog($"{d_MethodName} selected recipe {selectedRecipe.Recipe.GetName()} not found in call {callCount}, so not reselecting it");
                }

                LogUtil.DebugLog($"{d_MethodName} restoring previous craft info tab {craftInfoTabType} in call {callCount}");
                if (craftInfoWindow != null)
                {
                    craftInfoWindow.TabType = craftInfoTabType;
                    craftInfoWindow.SetSelectedButtonByType(craftInfoTabType);
                    craftInfoWindow.IsDirty = true;
                }
            }
            else
            {
                LogUtil.DebugLog($"{d_MethodName} selected recipe is null in call {callCount}, so not reselecting it");
            }
        }
    }
}
