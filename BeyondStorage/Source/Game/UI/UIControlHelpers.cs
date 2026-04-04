namespace BeyondStorage.Source.Game.UI;

/// <summary>
/// Helper class for finding and working with UI controls in the game's XUI system.
/// Provides utility methods for locating specific controls and performing common operations.
/// </summary>
public static class UIControlHelpers
{
    /// <summary>
    /// The ID of the smart loot sort button defined in windows.xml
    /// </summary>
    public const string SMART_PLAYER_INVENTORY_PUSH_BUTTON_ID = "btnBeyondSmartPlayerInventoryPush";
    public const string SMART_COLLECTOR_PUSH_BUTTON_ID = "btnBeyondSmartCollectorPush";

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

    public static XUiController GetSmartPlayerInventoryPushButton(XUiController instance)
    {
        var btnBeyondSmartPlayerInventoryPush = GetSmartButtonByID(instance, SMART_PLAYER_INVENTORY_PUSH_BUTTON_ID);
        return btnBeyondSmartPlayerInventoryPush;
    }
}