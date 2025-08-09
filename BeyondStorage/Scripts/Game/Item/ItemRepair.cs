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
        int DEFAULT_RETURN_VALUE = currentCount;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, config => config.EnableForItemRepair,
            out StorageContext context, out ItemClass itemClass, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var currentValue = currentCount * itemClass.RepairAmount.Value;
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; currentCount {currentCount}; currentValue {currentValue}");
#endif
        if (currentValue > 0)
        {
            return currentCount;
        }

        var storageCount = context.GetItemCount(itemValue);
        var newCount = currentCount + storageCount;

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; storageCount {storageCount}; newCount {newCount}");
#endif
        return newCount;
    }

    // Used By:
    //      ItemActionEntryRepair.RefreshEnabled
    //          FOR: Item Repair - Button Enabled
    public static int ItemRepairRefreshGetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(ItemRepairRefreshGetItemCount);
        const int DEFAULT_RETURN_VALUE = 0;

        if (!ValidationHelper.ValidateItemValue(itemValue, d_MethodName, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        // Early exit for UI state - check this before expensive context creation
        if (!ActionListVisible || !RepairActionShown)
        {
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateStorageContextWithFeature(d_MethodName, config => config.EnableForItemRepair, out StorageContext context))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var storageCount = context.GetItemCount(itemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: item {itemName}; storageCount {storageCount}");
#endif
        return storageCount;
    }
}