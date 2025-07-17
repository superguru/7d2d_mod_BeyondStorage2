using System.Linq;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Recipe;
public static class WorkstationRecipe
{
#if DEBUG
    static int s_bg_calls = 0;
#endif
    public static void BackgroundWorkstationCraftCompleted()
    {
        // This is called when the recipe finishes crafting on a workstation TE that is NOT open on a player screen
        string d_MethodName = "BackgroundWorkstationCraftComplete";

#if DEBUG
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog($"{d_MethodName} called {++s_bg_calls} times");
        }
#endif
        var player = GameManager.Instance.World?.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.Error($"{d_MethodName}: primary player is null");
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
                            LogUtil.DebugLog($"{d_MethodName} found open workstation window in call {s_bg_calls}. Refreshing the recipes.");
#endif
                            var recipeList = workstationWindowGroup.recipeList;
                            recipeList?.RefreshRecipes();
                        }
                    }
                }
            }
        }
    }
}
