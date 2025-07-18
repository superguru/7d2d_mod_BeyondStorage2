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
        var itemName = itemValue.ItemClass.GetItemName();

        // return early if not enabled for block repair
        if (!ModConfig.EnableForBlockRepair())
        {
            return 0;
        }

        var result = ContainerUtils.GetItemCount(itemValue);
        LogUtil.DebugLog($"BlockRepairGetItemCount | item {itemName}; result {result}");

        return result;
    }

    // Used By:
    //      ItemActionRepair.removeRequiredItem
    //          Block Repair - remove items on repair
    public static int BlockRepairRemoveRemaining(int currentCount, ItemStack itemStack)
    {
        var itemName = itemStack.itemValue.ItemClass.GetItemName();

        // return early if not enabled for block repair
        if (!ModConfig.EnableForBlockRepair())
        {
            return currentCount;
        }

        // itemStack.count is total amount needed
        // currentCount is the amount removed previously in last DecItem
        var stillNeeded = itemStack.count - currentCount;
        LogUtil.DebugLog($"BlockRepairRemoveRemaining | itemStack {itemName}; currentCount {currentCount}; stillNeeded {stillNeeded} ");

        // Skip if already 0
        if (stillNeeded == 0)
        {
            return currentCount;
        }

        // Add amount removed from storage to last amount removed to update result
        var totalRemoved = currentCount + ContainerUtils.RemoveRemaining(itemStack.itemValue, stillNeeded);
        LogUtil.DebugLog($"BlockRepairRemoveRemaining | total removed {totalRemoved}; stillNeeded {stillNeeded}");

        return totalRemoved;
    }
}