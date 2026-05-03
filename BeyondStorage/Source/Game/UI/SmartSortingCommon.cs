using BeyondStorage.Storage;

namespace BeyondStorage.Game.UI;

public class SmartSortingCommon
{
    public static void SmartCollectorPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartCollectorPush();
    }

    public static void SmartPlayerInventoryPullLoadout_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartPlayerInventoryLoadoutPull();
    }

    public static void SmartPlayerInventoryPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartPlayerInventoryPush();
    }

    public static void SmartDroneInventoryPullLoadout_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartDroneInventoryLoadoutPull();
    }

    public static void SmartLootWindowPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartLootWindowPush();
    }

    public static void SmartVehiclePullLoadout_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartVehicleLoadoutPull();
    }

    public static void SmartVehiclePush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartVehiclePush();
    }

    public static void SmartWorkstationOutputPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartWorkstationOutputPush();
    }
}