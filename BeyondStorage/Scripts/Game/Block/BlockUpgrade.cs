using BeyondStorage.Scripts.Configuration;
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

        // skip if not enabled
        if (!ModConfig.EnableForBlockUpgrade())
        {
            return 0;
        }

        var context = StorageContextFactory.Create(d_MethodName);
        var result = context?.GetItemCount(itemValue) ?? 0;

        ModLogger.DebugLog($"{d_MethodName} | item {itemValue.ItemClass.GetItemName()}; count {result}");
        return result;
    }

    // Used By:
    //      ItemActionRepair.RemoveRequiredResource
    //          Block Upgrade - ClearStacksForFilter items
    public static int BlockUpgradeRemoveRemaining(int currentCount, ItemValue itemValue, int requiredCount)
    {
        const string d_MethodName = nameof(BlockUpgradeRemoveRemaining);
        var itemName = itemValue.ItemClass.GetItemName();

        // skip if not enabled
        if (!ModConfig.EnableForBlockUpgrade())
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

        ModLogger.DebugLog($"{d_MethodName} | item {itemName}; currentCount {currentCount}; requiredCount {requiredCount}");

        var context = StorageContextFactory.Create(d_MethodName);
        var removedFromStorage = context?.RemoveRemaining(itemValue, requiredCount - currentCount) ?? 0;

        // add amount removed from storage to previous removed count to update result
        var result = currentCount + removedFromStorage;
        ModLogger.DebugLog($"{d_MethodName} | item {itemName}; removed {removedFromStorage}; new result {result}");

        return result;
    }
}