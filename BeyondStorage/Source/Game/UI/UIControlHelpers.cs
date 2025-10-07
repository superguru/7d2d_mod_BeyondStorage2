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
    public const string SMART_LOOT_SORT_BUTTON_ID = "btnBeyondSmartLootSort";

    /// <summary>
    /// Gets the smart loot sort button from the specified controller instance.
    /// Searches for the button in the XUiC_ContainerStandardControls child.
    /// </summary>
    /// <param name="instance">The controller instance to search in</param>
    /// <returns>The button control if found, null otherwise</returns>
    public static XUiController GetSmartLootSortButton(XUiController instance)
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

        var btnBeyondSmartLootSort = stdControls.GetChildById(SMART_LOOT_SORT_BUTTON_ID);
        return btnBeyondSmartLootSort;
    }
}