using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

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
                        if (wg?.Controller is XUiC_CraftingWindowGroup windowGroup)
                        {

                            var recipeList = windowGroup?.recipeList;
                            if (recipeList != null)
                            {
                                LogUtil.DebugLog($"{d_MethodName} Refreshing the recipes for open workstation in call {callCount}");

                                var selectedRecipe = recipeList.SelectedEntry;

                                var currPage = recipeList.Page;
                                recipeList.RefreshRecipes();
                                recipeList.Page = currPage;

                                if (selectedRecipe != null)
                                {
                                    var newSelectedRecipe = recipeList.recipeControls.Where(r => r.Recipe.GetName() == selectedRecipe.Recipe.GetName()).FirstOrDefault();
                                    if (newSelectedRecipe != null)
                                    {
                                        LogUtil.DebugLog($"{d_MethodName} reselecting recipe {selectedRecipe.Recipe.GetName()} in call {callCount}");
                                        recipeList.SelectedEntry = newSelectedRecipe;
                                    }
                                    else
                                    {
                                        // TODO: This might be an error in the game? Not sure.
                                        LogUtil.DebugLog($"{d_MethodName} selected recipe {selectedRecipe.Recipe.GetName()} not found in call {callCount}, so not reselecting it");
                                    }
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

                            if (windowGroup is XUiC_WorkstationWindowGroup workstation)
                            {
                                LogUtil.DebugLog($"{d_MethodName} Refreshing the open workstation window in call {callCount}");
                                workstation.syncUIfromTE();
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
}
