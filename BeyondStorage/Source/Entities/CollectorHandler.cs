using System.Linq;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Entities;

public static class CollectorHandler
{
    public static string GetCollectorName(TileEntityCollector collector)
    {
#if DEBUG
        const string d_MethodName = nameof(GetCollectorName);
#endif  

        string name = "Unknown Collector";

        if (collector == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: collector is null, returning default name");
#endif
            return name;
        }

        // Check cache first
        if (EntityNameCache.TryGetName(collector, out string cachedName))
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Returning cached name '{cachedName}' for collector");
#endif
            return cachedName;
        }

        var block = collector.block;
        if (block == null)
        {
#if DEBUG   
            ModLogger.DebugLog($"{d_MethodName}: collector.block is null, returning default name");
#endif
        }
        else
        {
            name = block.GetLocalizedBlockName();

#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Resolved and caching name '{name}' for collector");
#endif
        }

        EntityNameCache.CacheName(collector, name);
        return name;
    }

    /// <summary>
    /// Marks a collector as modified after items are removed from it
    /// </summary>
    public static void MarkCollectorModified(TileEntityCollector collector)
    {
        const string d_MethodName = nameof(MarkCollectorModified);
        ModLogger.DebugLog($"{d_MethodName}: Marking Collector '{collector?.GetType().Name}' as modified");

        if (collector == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: collector is null");
            return;
        }

        PackCollector(collector);

        collector.SetChunkModified();
        collector.SetModified();
    }

    private static void PackCollector(TileEntityCollector collector)
    {
        const string d_MethodName = nameof(PackCollector);

        if (collector == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: collector is null");
            return;
        }

        var s = "";

        s = string.Join(",", collector.FillValues.Select(f => f.ToString()));
        ModLogger.DebugLog($"{d_MethodName}: Fill values after item removal: {s}");

        s = string.Join(",", collector.Items.Select(stack => stack.count.ToString()));
        ModLogger.DebugLog($"{d_MethodName}: Slot counts after item removal: {s}");

        /* Scenario: 
         * - Collector has these items counts in the slots 1, 2, 0; slot 0 is partially filled, slot 1 is full, slot 2 is producing
         * - Why is slot 0 partially filled? 
         *   a) Maybe the player previously removed only 1 out of 2 already produced items out of it.
         *   b) Maybe this mod removed 1 item from it for crafting
         *   --> Either way, that slot is not filled completely, but it is also not producing anything
         *   --> Case a) is already how the game behaves unmodded, so for now not changing that behaviour
         * - In the future, we might want to change this behaviour to always remove full stacks from the collector
         * - "Compressing" the slots, where we change the slots counts to be 2,1,0 would not mean slot 1 is producing
         * - Alternatively, making slot 0 start producing at 50% would mean destroying the already produced water in it
         * - We can consolidate all the producted items into the available slots, up to max stack size of the item, but that
         *   seems like too much work with not much gain, and probably not very predictable.
         *   Also this would change the behaviour a lot, meaning collectors could produce many more items than
         *   usual, which might not be what players expect, and might be too powerful.
        */
    }
}