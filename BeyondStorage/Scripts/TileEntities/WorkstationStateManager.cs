using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.TileEntities;

public static class WorkstationStateManager
{
    public const int DEFAULT_WORKSTATION_LIST_CAPACITY = 32;

    /// <summary>
    /// Marks a workstation as modified when items are removed from its output, such as when pulling items from the workstation.
    /// </summary>
    public static void MarkWorkstationModified(TileEntityWorkstation workstation)
    {
        if (WorldTools.IsServer())
        {
            return;
        }

        const string d_method_name = "MarkModifiedWorkstationFunc";
        ModLogger.DebugLog($"{d_method_name} | Marking Workstation '{workstation?.GetType().Name}' as modified");

        if (workstation == null)
        {
            ModLogger.Error($"{d_method_name}: workstation is null");
            return;
        }

        workstation.SetChunkModified();
        workstation.SetModified();

        string blockName = GameManager.Instance.World.GetBlock(workstation.ToWorldPos()).Block.GetBlockName();
        var workstationData = CraftingManager.GetWorkstationData(blockName);
        if (workstationData == null)
        {
            ModLogger.Error($"{d_method_name}: No WorkstationData found for block '{blockName}'");
            return;
        }

        string windowName = !string.IsNullOrEmpty(workstationData.WorkstationWindow)
            ? workstationData.WorkstationWindow
            : $"workstation_{blockName}";

        ModLogger.DebugLog($"{d_method_name}: blockName '{blockName}', windowName '{windowName}'");

        var player = GameManager.Instance.World.GetPrimaryPlayer();

        var windowGroup = player.windowManager.GetWindow(windowName) as XUiWindowGroup;
        if (windowGroup == null)
        {
            ModLogger.DebugLog($"{d_method_name}: windowGroup is null for '{windowName}'");
            return;
        }

        if (!windowGroup.isShowing)
        {
            return;
        }

        var workstationWindowGroup = windowGroup.Controller as XUiC_WorkstationWindowGroup;
        if (workstationWindowGroup == null)
        {
            ModLogger.DebugLog($"{d_method_name}: WorkstationWindowGroup is null for '{windowName}'");
            return;
        }

        if (workstationWindowGroup.WorkstationData == null)
        {
            ModLogger.Error($"{d_method_name}: WorkstationData is null for '{windowName}'");
            return;
        }

        ModLogger.DebugLog($"{d_method_name}: Syncing UI from TE for '{windowName}'");
        workstationWindowGroup.syncUIfromTE();
    }
}