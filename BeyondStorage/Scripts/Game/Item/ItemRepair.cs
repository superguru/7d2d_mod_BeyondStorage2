using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Item;

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

        var context = StorageContextFactory.Create(d_MethodName);

        // skip if not enabled
        if (!context.Config.EnableForItemRepair)
        {
            return currentCount;
        }

        var currentValue = currentCount * itemValue.ItemClass.RepairAmount.Value;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemValue.ItemClass.GetItemName()}; currentCount {currentCount}; currentValue {currentValue}");
#endif
        if (currentValue > 0)
        {
            return currentCount;
        }

        var storageCount = context.GetItemCount(itemValue);
        var newCount = currentCount + storageCount;

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemValue.ItemClass.GetItemName()}; storageCount {storageCount}; newCount {newCount}");
#endif
        return newCount;
    }

    // Used By:
    //      ItemActionEntryRepair.RefreshEnabled
    //          FOR: Item Repair - Button Enabled
    public static int ItemRepairRefreshGetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(ItemRepairRefreshGetItemCount);

        if (itemValue == null)
        {
            ModLogger.Warning($"{d_MethodName}: itemValue is null, returning 0");
            return 0;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // skip if not enabled
        if (!context.Config.EnableForItemRepair)
        {
            return 0;
        }
        // skip if not showing repair action or action list
        if (!ActionListVisible || !RepairActionShown)
        {
            return 0;
        }

        var storageCount = context.GetItemCount(itemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemValue.ItemClass.GetItemName()}; storageCount {storageCount}");
#endif
        return storageCount;
    }
}