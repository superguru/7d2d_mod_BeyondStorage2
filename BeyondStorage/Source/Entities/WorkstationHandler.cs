using BeyondStorage.Source.Data;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.Storage;

namespace BeyondStorage.Source.Entities;

public static class WorkstationHandler
{
    public const int DEFAULT_WORKSTATION_LIST_CAPACITY = 32;

    public static ItemStack[] GetAllSlotItems(TileEntityWorkstation workstation)
    {
        var items = workstation?.output;
        if (items == null || items.Length == 0)
        {
            return [];
        }

        return items;
    }

    public static ItemStack[] GetConsumableItems(TileEntityWorkstation workstation)
    {
        var items = GetAllSlotItems(workstation);
        if (items.Length == 0)
        {
            return [];
        }

        return ItemX.GetFilteredItems(items, StorageFilter.AllItems, lockedSlots: null);
    }

    public static ItemStack[] GetPushableItems(TileEntityWorkstation workstation)
    {
        var items = GetAllSlotItems(workstation);
        if (items.Length == 0)
        {
            return [];
        }

        return ItemX.GetFilteredItems(items, StorageFilter.UnlockedOnly, lockedSlots: null);
    }

    public static string GetWorkstationName(TileEntityWorkstation workstation)
    {
#if DEBUG
        const string d_MethodName = nameof(GetWorkstationName);
#endif  
        string name = "Unknown Workstation";

        if (workstation == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: workstation is null, returning default name");
#endif
            return name;
        }

        // Check cache first
        if (EntityNameCache.TryGetName(workstation, out string cachedName))
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Returning cached name '{cachedName}' for workstation at {workstation.ToWorldPos()}");
#endif
            return cachedName;
        }

        name = workstation.block.GetLocalizedBlockName();

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Resolved and caching name '{name}' for workstation at {workstation.ToWorldPos()}");
#endif
        EntityNameCache.CacheName(workstation, name);

        return name;
    }

    /// <summary>
    /// Marks a workstation as modified when items are removed from its output, such as when pulling items from the workstation.
    /// </summary>
    public static void MarkWorkstationModified(TileEntityWorkstation workstation)
    {
        const string d_MethodName = nameof(MarkWorkstationModified);
        //ModLogger.DebugLog($"{d_MethodName}: Marking Workstation '{workstation?.GetType().Name}' as modified");

        if (workstation == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: workstation is null");
            return;
        }

        workstation.SetChunkModified();
        workstation.SetModified();

        string blockName = GameManager.Instance.World.GetBlock(workstation.ToWorldPos()).Block.GetBlockName();
        var workstationData = CraftingManager.GetWorkstationData(blockName);
        if (workstationData == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: No WorkstationData found for block '{blockName}'");
            return;
        }

        string windowName = !string.IsNullOrEmpty(workstationData.WorkstationWindow)
            ? workstationData.WorkstationWindow
            : $"workstation_{blockName}";

        //ModLogger.DebugLog($"{d_MethodName}: blockName '{blockName}', windowName '{windowName}'");

        var player = GameManager.Instance.World.GetPrimaryPlayer();

        if (player.windowManager.GetWindow(windowName) is not XUiWindowGroup windowGroup)
        {
            ModLogger.DebugLog($"{d_MethodName}: windowGroup is null for '{windowName}'");
            return;
        }

        if (!windowGroup.isShowing)
        {
            return;
        }

        if (windowGroup.Controller is not XUiC_WorkstationWindowGroup workstationWindowGroup)
        {
            ModLogger.DebugLog($"{d_MethodName}: WorkstationWindowGroup is null for '{windowName}'");
            return;
        }

        if (workstationWindowGroup.WorkstationData == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: WorkstationData is null for '{windowName}'");
            return;
        }

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Syncing UI from TE for '{windowName}'");
#endif
        workstationWindowGroup.syncUIfromTE();
    }
}