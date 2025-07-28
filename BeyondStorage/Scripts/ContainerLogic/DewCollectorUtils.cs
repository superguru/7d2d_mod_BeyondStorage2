using System.Linq;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class DewCollectorUtils
{

    /// <summary>
    /// Marks a dew collector as modified after items are removed from it
    /// </summary>
    public static void MarkDewCollectorModified(TileEntityDewCollector dewCollector)
    {
        const string d_method_name = "MarkDewCollectorModified";
        LogUtil.DebugLog($"{d_method_name} | Marking Dew Collector '{dewCollector?.GetType().Name}' as modified");

        if (dewCollector == null)
        {
            LogUtil.Error($"{d_method_name}: dew collector is null");
            return;
        }

        PackDewCollector(dewCollector);

        dewCollector.SetChunkModified();
        dewCollector.SetModified();
    }

    private static void PackDewCollector(TileEntityDewCollector dewCollector)
    {
        const string d_method_name = "MarkDewCollectorModified.PackDewCollector";

        if (dewCollector == null)
        {
            LogUtil.Error($"{d_method_name}: dew collector is null");
            return;
        }

        var s = "";

        s = string.Join(",", dewCollector.fillValuesArr.Select(f => f.ToString()));
        LogUtil.DebugLog($"{d_method_name} | Fill values after item removal: {s}");

        s = string.Join(",", dewCollector.items.Select(stack => stack.count.ToString()));
        LogUtil.DebugLog($"{d_method_name} | Slot counts after item removal: {s}");

        /* Scenario: 
         * - Dew Collector has these items counts in the slots 1, 2, 0; slot 0 is partially filled, slot 1 is full, slot 2 is producing
         * - Why is slot 0 partially filled? 
         *   a) Maybe the player previously removed only 1 out of 2 already produced items out of it.
         *   b) Maybe this mod removed 1 item from it for crafting
         *   --> Either way, that slot is not filled completely, but it is also not producing anything
         *   --> Case a) is already how the game behaves unmodded, so for now not changing that behaviour
         * - In the future, we might want to change this behaviour to always remove full stacks from the dew collector
         * - "Compressing" the slots, where we change the slots counts to be 2,1,0 would not mean slot 1 is producing
         * - Alternatively, making slot 0 start producing at 50% would mean destroying the already produced water in it
         * - We can consolidate all the producted items into the available slots, up to max stack size of the item, but that
         *   seems like too much work with not much gain, and probably not very predictable.
         *   Also this would change the behaviour a lot, meaning dew collectors could produce many more items than
         *   usual, which might not be what players expect, and might be too powerful.
        */
    }
}