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
        const int DEFAULT_RETURN_VALUE = 0;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out _, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

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
        int DEFAULT_RETURN_VALUE = currentCount;

        if (itemStack == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: itemStack is null, returning currentCount {currentCount}");
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateItemAndContext(itemStack.itemValue, d_MethodName, out StorageContext context, out _, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        // itemStack.count is total amount needed
        // currentCount is the amount removed previously in last DecItem
        var stillNeeded = itemStack.count - currentCount;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: itemStack {itemName}; currentCount {currentCount}; stillNeeded {stillNeeded} ");
#endif
        // Skip if already 0
        if (stillNeeded == 0)
        {
            return DEFAULT_RETURN_VALUE;
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