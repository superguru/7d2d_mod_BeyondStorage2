using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Block;

public class BlockRepair
{
    // Used By:
    //      ItemActionRepair.canRemoveRequiredItem
    //          Block Repair - Resources Available Check
    public static int BlockRepairGetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(BlockRepairGetItemCount);

        // return early if not enabled for block repair
        if (!ModConfig.EnableForBlockRepair())
        {
            return 0;
        }

        var itemName = itemValue.ItemClass.GetItemName();

        var result = ContainerUtils.GetItemCount(null, itemValue);
        LogUtil.DebugLog($"{d_MethodName} | item {itemName}; result {result}");

        return result;
    }

    // Used By:
    //      ItemActionRepair.removeRequiredItem
    //          Block Repair - remove items on repair
    public static int BlockRepairRemoveRemaining(int currentCount, ItemStack itemStack)
    {
        const string d_MethodName = nameof(BlockRepairRemoveRemaining);

        // return early if not enabled for block repair
        if (!ModConfig.EnableForBlockRepair())
        {
            return currentCount;
        }

        var itemName = itemStack.itemValue.ItemClass.GetItemName();
        var context = StorageAccessContext.Create(d_MethodName);

        // itemStack.count is total amount needed
        // currentCount is the amount removed previously in last DecItem
        var stillNeeded = itemStack.count - currentCount;
        LogUtil.DebugLog($"{d_MethodName} | itemStack {itemName}; currentCount {currentCount}; stillNeeded {stillNeeded} ");

        // Skip if already 0
        if (stillNeeded == 0)
        {
            return currentCount;
        }

        // Add amount removed from storage to last amount removed to update result
        var totalRemoved = currentCount + ContainerUtils.RemoveRemaining(itemStack.itemValue, stillNeeded);
        LogUtil.DebugLog($"{d_MethodName} | total removed {totalRemoved}; stillNeeded {stillNeeded}");

        return totalRemoved;
    }
}