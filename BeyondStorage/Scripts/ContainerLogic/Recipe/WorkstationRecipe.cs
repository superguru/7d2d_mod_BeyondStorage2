using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Recipe;
public static class WorkstationRecipe
{
    static int s_bg_calls = 0;
    static int s_curr_calls = 0;

    public static void BackgroundWorkstationCraftCompleted()
    {
        if (!ModConfig.PullFromWorkstationOutputs())
        {
            // If the config is set to not pull from workstation outputs, we don't need to refresh the windows
            return;
        }

        // This is called when the recipe finishes crafting on a workstation TE that is NOT open on a player screen
        string d_MethodName = "BackgroundWorkstationCraftComplete";
        s_bg_calls++;

        LogUtil.DebugLog($"{d_MethodName} called {s_bg_calls} times");

        RefreshOpenWorkstationWindows(d_MethodName, s_bg_calls);
    }
    public static void CurrentWorkstationCraftCompleted()
    {
        if (!ModConfig.PullFromWorkstationOutputs())
        {
            // If the config is set to not pull from workstation outputs, we don't need to refresh the windows
            return;
        }

        // This is called when the recipe finishes crafting on the currently opened workstation window
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

        var shows = player.windowManager?.windows.Where(w => w.isShowing);
        if (shows != null)
        {
            foreach (GUIWindow win in shows)
            {
                if (win != null)
                {
                    if (win is XUiWindowGroup wg)
                    {
                        if (wg?.Controller is XUiC_WorkstationWindowGroup workstationWindowGroup)
                        {
#if DEBUG
                            LogUtil.DebugLog($"{d_MethodName} Refreshing the recipes for open workstation in call {callCount}");
#endif
                            var recipeList = workstationWindowGroup?.recipeList;
                            recipeList?.RefreshRecipes();

#if DEBUG
                            LogUtil.DebugLog($"{d_MethodName} Refreshing the action list for open workstation in call {callCount}");
#endif
                            var craftInfoWindow = workstationWindowGroup?.craftInfoWindow;
                            craftInfoWindow?.actionItemList?.RefreshActionList();
                        }
                    }
                }
            }
        }
    }
}
