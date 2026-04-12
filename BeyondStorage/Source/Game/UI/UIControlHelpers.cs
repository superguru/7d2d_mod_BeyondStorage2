namespace BeyondStorage.Source.Game.UI;

/// <summary>
/// Helper class for finding and working with UI controls in the game's XUI system.
/// Provides utility methods for locating specific controls and performing common operations.
/// </summary>
public static class UIControlHelpers
{
    /// <summary>
    /// The IDs of the smart storage buttons defined in windows.xml
    /// </summary>
    /// === Push ===
    public const string SMART_COLLECTOR_PUSH_BUTTON_ID = "btnBeyondSmartCollectorPush";
    public const string SMART_LOOT_WINDOW_PUSH_BUTTON_ID = "btnBeyondSmartLootWindowPush";
    public const string SMART_PLAYER_INVENTORY_PUSH_BUTTON_ID = "btnBeyondSmartPlayerInventoryPush";
    public const string SMART_VEHICLE_PUSH_BUTTON_ID = "btnBeyondSmartVehiclePush";
    public const string SMART_WORKSTATION_OUTPUT_PUSH_BUTTON_ID = "btnBeyondSmartWorkstationOutputPush";

    /// === Pull ===
    public const string SMART_VEHICLE_PULL_LOADOUT_BUTTON_ID = "btnBeyondSmartVehiclePullLoadout";

    private static XUiController GetSmartButtonByID(XUiController instance, string buttonId)
    {
        if (instance == null)
        {
            return null;
        }

        var stdControls = instance.GetChildByType<XUiC_ContainerStandardControls>();
        if (stdControls == null)
        {
            return null;
        }

        var btnSmartButton = stdControls.GetChildById(buttonId);
        return btnSmartButton;
    }

    public static XUiController GetSmartCollectorPushButton(XUiController instance)
    {
        var btnBeyondSmartCollectorPush = GetSmartButtonByID(instance, SMART_COLLECTOR_PUSH_BUTTON_ID);
        return btnBeyondSmartCollectorPush;
    }
    public static XUiController GetSmartLootWindowPushButton(XUiController instance)
    {
        var btnBeyondSmartLootWindowPush = GetSmartButtonByID(instance, SMART_LOOT_WINDOW_PUSH_BUTTON_ID);
        return btnBeyondSmartLootWindowPush;
    }

    public static XUiController GetSmartPlayerInventoryPushButton(XUiController instance)
    {
        var btnBeyondSmartPlayerInventoryPush = GetSmartButtonByID(instance, SMART_PLAYER_INVENTORY_PUSH_BUTTON_ID);
        return btnBeyondSmartPlayerInventoryPush;
    }

    public static XUiController GetSmartVehiclePullLoadoutButton(XUiController instance)
    {
        var btnBeyondSmartVehiclePullLoadout = GetSmartButtonByID(instance, SMART_VEHICLE_PULL_LOADOUT_BUTTON_ID);
        return btnBeyondSmartVehiclePullLoadout;
    }

    public static XUiController GetSmartVehiclePushButton(XUiController instance)
    {
        var btnBeyondSmartVehiclePush = GetSmartButtonByID(instance, SMART_VEHICLE_PUSH_BUTTON_ID);
        return btnBeyondSmartVehiclePush;
    }
    public static XUiController GetSmartWorkstationOutputPushButton(XUiController instance)
    {
        var btnBeyondSmartWorkstationOutputPush = GetSmartButtonByID(instance, SMART_WORKSTATION_OUTPUT_PUSH_BUTTON_ID);
        return btnBeyondSmartWorkstationOutputPush;
    }
}