using System.Linq;
using BeyondStorage.Scripts.Configuration;
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

        //if (player is EntityPlayerLocal localPlayer && localPlayer.IsAlive())
        //{
        //    LogUtil.DebugLog($"{d_MethodName} Refreshing the local player's inventory in call {callCount}");
        //    localPlayer.callInventoryChanged();
        //}

        var shows = player.windowManager?.windows.Where(w => w.isShowing);
        if (shows != null)
        {
            foreach (GUIWindow win in shows)
            {
                if (win != null)
                {
                    if (win is XUiWindowGroup wg)
                    {
                        if (wg?.Controller is XUiC_WorkstationWindowGroup workstation)
                        {

                            var recipeList = workstation?.recipeList;
                            if (recipeList != null)
                            {
                                LogUtil.DebugLog($"{d_MethodName} Refreshing the recipes for open workstation in call {callCount}");

                                // Save the state of things
                                var currPage = recipeList.Page;
                                var selectedRecipe = recipeList.SelectedEntry;
                                XUiC_CraftingInfoWindow craftInfoWindow = null;
                                TabTypes craftInfoTabType = TabTypes.Ingredients;

                                if (selectedRecipe != null)
                                {
                                    craftInfoWindow = workstation.craftInfoWindow;
                                    craftInfoTabType = craftInfoWindow.TabType;
                                }

                                // Refresh the things
                                LogUtil.DebugLog($"{d_MethodName} refreshing the workstation in call {callCount}");
                                recipeList.RefreshRecipes();
                                workstation.syncUIfromTE();

                                // Restore the state of things
                                recipeList.Page = currPage;

                                if (selectedRecipe != null)
                                {
                                    var newSelectedRecipe = recipeList.recipeControls.Where(r => r.Recipe.GetName() == selectedRecipe.Recipe.GetName()).FirstOrDefault();
                                    if (newSelectedRecipe != null)
                                    {
                                        LogUtil.DebugLog($"{d_MethodName} restoring the selected recipe {selectedRecipe.Recipe.GetName()} in call {callCount}");
                                        recipeList.SelectedEntry = newSelectedRecipe;
                                    }
                                    else
                                    {
                                        // TODO: This might be an error in the game? Not sure.
                                        LogUtil.DebugLog($"{d_MethodName} selected recipe {selectedRecipe.Recipe.GetName()} not found in call {callCount}, so not reselecting it");
                                    }

                                    LogUtil.DebugLog($"{d_MethodName} restoring previous craft info tab {craftInfoTabType} in call {callCount}");
                                    craftInfoWindow.TabType = craftInfoTabType;
                                    craftInfoWindow.SetSelectedButtonByType(craftInfoTabType);
                                    craftInfoWindow.IsDirty = true;
                                }
                                else
                                {
                                    LogUtil.DebugLog($"{d_MethodName} selected recipe is null in call {callCount}, so not reselecting it");
                                }
                            }
                            else
                            {
                                LogUtil.DebugLog($"{d_MethodName} recipeList is null in call {callCount}, so not updating recipe list");
                            }
                        }
                        else
                        {
                            LogUtil.DebugLog($"{d_MethodName} windowGroup is not a workstation in call {callCount}, so just setting as dirty");
                            wg.Controller.IsDirty = true;
                            wg.Controller.SetAllChildrenDirty();
                        }
                    }
                }
            }
        }
    }
}
