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

        if (itemValue == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: itemValue is null, returning 0");
            return 0;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // skip if not enabled
        if (!context.Config.EnableForBlockUpgrade)
        {
            return 0;
        }

        var result = context.GetItemCount(itemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemValue.ItemClass.GetItemName()}; count {result}");
#endif
        return result;
    }

    // Used By:
    //      ItemActionRepair.RemoveRequiredResource
    //          Block Upgrade - ClearStacksForFilter items
    public static int BlockUpgradeRemoveRemaining(int currentCount, ItemValue itemValue, int requiredCount)
    {
        const string d_MethodName = nameof(BlockUpgradeRemoveRemaining);

        if (itemValue == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: itemValue is null, returning 0");
            return 0;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // skip if not enabled
        if (!context.Config.EnableForBlockUpgrade)
        {
            return currentCount;
        }
        // currentCount is previous amount removed by DecItem
        // requiredCount is total required (before last decItem)
        // return early if we already have enough
        if (currentCount == requiredCount)
        {
            return currentCount;
        }

        var itemName = itemValue.ItemClass.GetItemName();
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