using BeyondStorage.Source.Storage;

namespace BeyondStorage.Source.Game.UI;

public class SmartSortingCommon
{
    public static void SmartCollectorPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartCollectorPush();
    }

    public static void SmartPlayerInventoryPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartPlayerInventoryPush();
    }

    internal static void SmartLootWindowPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartLootWindowPush();
    }

    public static void SmartVehiclePush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartVehiclePush();
    }

    internal static void SmartWorkstationOutputPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartWorkstationOutputPush();
    }
}