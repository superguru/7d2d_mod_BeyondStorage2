using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Source.Game.UI;

public class SmartSortingCommon
{
    public static void SmartPlayerInventoryPush_EventHandler(XUiController _sender, int _mouseButton)
    {
        SmartSortingFunctions.SmartPlayerInventoryPush();
    }
}