using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

public static class ItemRepair
{
    internal static bool ActionListVisible { get; set; }
    internal static bool RepairActionShown { get; set; }

    // Used By:
    //      ItemActionEntryRepair.OnActivated
    //          FOR: Item Repair - Allows Repair
    public static int ItemRepairOnActivatedGetItemCount(ItemValue itemValue, int currentCount)
    {
        const string d_MethodName = nameof(ItemRepairOnActivatedGetItemCount);

        // skip if not enabled
        if (!ModConfig.EnableForItemRepair())
        {
            return currentCount;
        }

        var currentValue = currentCount * itemValue.ItemClass.RepairAmount.Value;
        Logger.DebugLog($"{d_MethodName} | item {itemValue.ItemClass.GetItemName()}; currentCount {currentCount}; currentValue {currentValue}");

        if (currentValue > 0)
        {
            return currentCount;
        }

        var context = StorageAccessContext.Create(d_MethodName);
        var storageCount = context?.GetItemCount(itemValue) ?? 0;
        var newCount = currentCount + storageCount;

        Logger.DebugLog($"{d_MethodName} | item {itemValue.ItemClass.GetItemName()}; storageCount {storageCount}; newCount {newCount}");
        return newCount;
    }

    // Used By:
    //      ItemActionEntryRepair.RefreshEnabled
    //          FOR: Item Repair - Button Enabled
    public static int ItemRepairRefreshGetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(ItemRepairRefreshGetItemCount);

        // skip if not enabled
        if (!ModConfig.EnableForItemRepair())
        {
            return 0;
        }
        // skip if not showing repair action or action list
        if (!ActionListVisible || !RepairActionShown)
        {
            return 0;
        }

        var context = StorageAccessContext.Create(d_MethodName);
        var storageCount = context?.GetItemCount(itemValue) ?? 0;
        Logger.DebugLog($"{d_MethodName} | item {itemValue.ItemClass.GetItemName()}; storageCount {storageCount}");

        return storageCount;
    }
}