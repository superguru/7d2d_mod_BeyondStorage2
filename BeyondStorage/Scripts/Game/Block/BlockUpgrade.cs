using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Block;

public class BlockUpgrade
{
    // Used By:
    //      ItemActionRepair.CanRemoveRequiredResource
    //          Block Upgrade - Resources Available Check (called by ItemActionRepair: .ExecuteAction() and .RemoveRequiredResource())
    public static int BlockUpgradeGetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(BlockUpgradeGetItemCount);
        const int DEFAULT_RETURN_VALUE = 0;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, config => config.EnableForBlockUpgrade,
            out StorageContext context, out _, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var result = context.GetItemCount(itemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; count {result}");
#endif
        return result;
    }

    // Used By:
    //      ItemActionRepair.RemoveRequiredResource
    //          Block Upgrade - ClearStacksForFilter items
    public static int BlockUpgradeRemoveRemaining(int currentCount, ItemValue itemValue, int requiredCount)
    {
        const string d_MethodName = nameof(BlockUpgradeRemoveRemaining);
        int DEFAULT_RETURN_VALUE = currentCount;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, config => config.EnableForBlockUpgrade,
            out StorageContext context, out _, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        // currentCount is previous amount removed by DecItem
        // requiredCount is total required (before last decItem)
        // return early if we already have enough
        if (currentCount == requiredCount)
        {
            return currentCount;
        }

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; currentCount {currentCount}; requiredCount {requiredCount}");
#endif
        var removedFromStorage = context.RemoveRemaining(itemValue, requiredCount - currentCount);

        // add amount removed from storage to previous removed count to update result
        var result = currentCount + removedFromStorage;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; removed {removedFromStorage}; new result {result}");
#endif
        return result;
    }
}