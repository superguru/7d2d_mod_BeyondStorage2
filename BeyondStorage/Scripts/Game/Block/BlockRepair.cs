using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Block;

public class BlockRepair
{
    // Used By:
    //      ItemActionRepair.canRemoveRequiredItem
    //          Block Repair - Resources Available Check
    public static int BlockRepairGetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(BlockRepairGetItemCount);

        if (itemValue == null)
        {
            ModLogger.Warning($"{itemValue}: itemStack is null, returning 0");
            return 0;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // return early if not enabled for block repair
        if (!context.Config.EnableForBlockRepair)
        {
            return 0;
        }

        var itemName = itemValue.ItemClass.GetItemName();

        var result = context.GetItemCount(itemValue);

        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; result {result}");
        return result;
    }

    // Used By:
    //      ItemActionRepair.removeRequiredItem
    //          Block Repair - remove items on repair
    public static int BlockRepairRemoveRemaining(int currentCount, ItemStack itemStack)
    {
        const string d_MethodName = nameof(BlockRepairRemoveRemaining);

        if (itemStack == null)
        {
            ModLogger.Warning($"{d_MethodName}: itemStack is null, returning currentCount {currentCount}");
            return currentCount;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // return early if not enabled for block repair
        if (!context.Config.EnableForBlockRepair)
        {
            return currentCount;
        }

        var itemName = itemStack.itemValue.ItemClass.GetItemName();

        // itemStack.count is total amount needed
        // currentCount is the amount removed previously in last DecItem
        var stillNeeded = itemStack.count - currentCount;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: itemStack {itemName}; currentCount {currentCount}; stillNeeded {stillNeeded} ");
#endif
        // Skip if already 0
        if (stillNeeded == 0)
        {
            return currentCount;
        }

        // AddStackRangeForFilter amount removed from storage to last amount removed to update result
        var removedFromStorage = context.RemoveRemaining(itemStack.itemValue, stillNeeded);

        var totalRemoved = currentCount + removedFromStorage;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: total removed {totalRemoved}; removedFromStorage {removedFromStorage}; stillNeeded {stillNeeded}");
#endif
        return totalRemoved;
    }
}