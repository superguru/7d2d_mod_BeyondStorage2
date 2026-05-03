namespace BeyondStorage.Game.UI;

/// <summary>
/// Helper class for finding and working with UI controls in the game's XUI system.
/// Provides utility methods for locating specific controls and performing common operations.
/// </summary>
public static class UIControlHelpers
{
    /// <summary>
    /// The IDs of the smart storage buttons defined in windows.xml
    /// </summary>
    /// === Player ===
    public const string SMART_PLAYER_INVENTORY_PULL_LOADOUT_BUTTON_ID = "btnBeyondSmartPlayerInventoryPullLoadout"; // Visible when ONLY player inventory is open
    public const string SMART_PLAYER_INVENTORY_PUSH_BUTTON_ID = "btnBeyondSmartPlayerInventoryPush"; // Visible when ONLY player inventory is open

    public const string SMART_PLAYER_LOOTING_PULL_LOADOUT_BUTTON_ID = "btnBeyondSmartPlayerLootingPullLoadout"; // Visible when player has something else open as well
    public const string SMART_PLAYER_LOOTING_PUSH_BUTTON_ID = "btnBeyondSmartPlayerLootingPush"; // Visible when player has something else open as well

    /// === Collector ===
    public const string SMART_COLLECTOR_PUSH_BUTTON_ID = "btnBeyondSmartCollectorPush";

    /// === Loot Window ===
    public const string SMART_LOOT_WINDOW_PUSH_BUTTON_ID = "btnBeyondSmartStoragePush";  // For crates AND drones since they use the same loot window

    /// === Drone ===
    public const string SMART_DRONE_INVENTORY_PULL_LOADOUT_BUTTON_ID = "btnBeyondSmartDronePullLoadout";

    /// === Vehicle ===
    public const string SMART_VEHICLE_PULL_LOADOUT_BUTTON_ID = "btnBeyondSmartVehiclePullLoadout";
    public const string SMART_VEHICLE_PUSH_BUTTON_ID = "btnBeyondSmartVehiclePush";

    /// === Workstation ===
    public const string SMART_WORKSTATION_OUTPUT_PUSH_BUTTON_ID = "btnBeyondSmartWorkstationOutputPush";

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

    public static XUiController GetSmartDroneInventoryPullLoadoutButton(XUiController instance)
    {
        var btnBeyondSmartDronePullLoadout = GetSmartButtonByID(instance, SMART_DRONE_INVENTORY_PULL_LOADOUT_BUTTON_ID);
        return btnBeyondSmartDronePullLoadout;
    }

    public static XUiController GetSmartLootWindowPushButton(XUiController instance)
    {
        var btnBeyondSmartLootWindowPush = GetSmartButtonByID(instance, SMART_LOOT_WINDOW_PUSH_BUTTON_ID);
        return btnBeyondSmartLootWindowPush;
    }

    public static XUiController GetSmartPlayerInventoryPullLoadoutButton(XUiController instance)
    {
        var btnBeyondSmartPlayerInventoryPullLoadout = GetSmartButtonByID(instance, SMART_PLAYER_INVENTORY_PULL_LOADOUT_BUTTON_ID);
        return btnBeyondSmartPlayerInventoryPullLoadout;
    }

    public static XUiController GetSmartPlayerLootingPullLoadoutButton(XUiController instance)
    {
        var btnBeyondSmartPlayerLootingPullLoadout = GetSmartButtonByID(instance, SMART_PLAYER_LOOTING_PULL_LOADOUT_BUTTON_ID);
        return btnBeyondSmartPlayerLootingPullLoadout;
    }

    public static XUiController GetSmartPlayerInventoryPushButton(XUiController instance)
    {
        var btnBeyondSmartPlayerInventoryPush = GetSmartButtonByID(instance, SMART_PLAYER_INVENTORY_PUSH_BUTTON_ID);
        return btnBeyondSmartPlayerInventoryPush;
    }

    public static XUiController GetSmartPlayerLootingPushButton(XUiController instance)
    {
        var btnBeyondSmartPlayerLootingPush = GetSmartButtonByID(instance, SMART_PLAYER_LOOTING_PUSH_BUTTON_ID);
        return btnBeyondSmartPlayerLootingPush;
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