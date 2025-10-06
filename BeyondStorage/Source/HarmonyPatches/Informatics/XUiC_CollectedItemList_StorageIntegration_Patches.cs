using BeyondStorage.HarmonyPatches.Functions;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Source.Game.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_CollectedItemList))]
internal static class XUiC_CollectedItemList_StorageIntegration_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_CollectedItemList.AddItemStack))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool Intercept_AddItemStack_Prefix(XUiC_CollectedItemList __instance, ItemStack _is, bool _bAddOnlyIfNotExisting)
    {
#if DEBUG
#endif
        var itemInfo = ItemX.Info(_is);

        // Check if we should show the notification and get the reason
        if (ShouldShowItemStackNotification(_is, out string reason))
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Proceeding with AddItemStack for stack ({itemInfo}). Reason: {reason}");
#endif
            return true; // Proceed with original method execution
        }

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Skipping AddItemStack (notification) for stack ({itemInfo}). Reason: {reason}");
#endif
        return false; // Skip original method execution
    }

    /// <summary>
    /// Determines whether the Add/RemoveItemStack operation should be shown and provides the reason.
    /// This method centralizes all show conditions to make it easy to add new conditions and debug issues.
    /// </summary>
    /// <param name="itemStack">The ItemStack being processed</param>
    /// <param name="reason">Output parameter containing the reason for showing or not showing, if applicable</param>
    /// <returns>True if the operation should be shown, false otherwise</returns>
    private static bool ShouldShowItemStackNotification(ItemStack itemStack, out string reason)
    {
        // Condition 1: Check if this stack matches a current shift operation
        // This prevents duplicate notifications when items are being moved via Shift+Click
        // c:STACK_SHIFT
        var isStackShiftOperation = Stack_Shift_Patch.StackMatchesCurrentOp(itemStack);
        if (isStackShiftOperation)
        {
            reason = "Stack matches current shift operation";
            return false;
        }

        // Condition 2: Check if this is currency being received outside of a sale transaction
        // This prevents duplicate currency notifications when currency is added from storage operations
        // c:CURRENCY_ITEM
        var isCurrencyItem = CurrencyCache.IsCurrencyItem(itemStack);
        if (isCurrencyItem)
        {
            // c:MID_SALE
            var isMidSale = Item_Action_Entry_Sell_Patches.IsPlayerMidSale();
            if (isMidSale)
            {
                reason = "Currency item notification during sale transaction";
                return true;
            }
        }

        // c:CRATE_OPEN
        if (WindowStateManager.IsStorageContainerOpen())
        {
            // If a storage container is open, we don't want to show notifications
            // This prevents cluttering the UI with notifications while interacting with storage
            reason = "Storage container is currently open";
            return false;
        }

        // c:VEHICLE_OPEN
        if (WindowStateManager.IsVehicleStorageWindowOpen())
        {
            // If the vehicle storage window is open, we don't want to show notifications
            // This prevents cluttering the UI with notifications while interacting with vehicle storage
            reason = "Vehicle storage window is currently open";
            return false;
        }

        // c:WORKSTATION_OPEN
        if (WindowStateManager.IsWorkstationWindowOpen())
        {
            // If the workstation window is open, we don't want to show notifications
            // This prevents cluttering the UI with notifications while interacting with workstations
            reason = "Workstation window is currently open";
            return false;
        }

        // No hide conditions met, show the notification
        reason = "No conditions preventing notification display";
        return true;
    }

    /// <summary>
    /// Truth Table for ShouldShowItemStackNotification Logic
    /// 
    /// Conditions:
    /// - STACK_SHIFT: Stack matches current shift operation
    /// - CURRENCY_ITEM: Item is a currency item  
    /// - MID_SALE: Currently in middle of a sale transaction
    /// - CRATE_OPEN: Storage container window is open
    /// - VEHICLE_OPEN: Vehicle storage window is open
    /// - WORKSTATION_OPEN: Workstation window is open
    /// 
    /// Logic Flow (early exit on first match):
    /// 
    /// | STACK_SHIFT | CURRENCY_ITEM | MID_SALE | CRATE_OPEN | VEHICLE_OPEN | WORKSTATION_OPEN | RESULT | REASON                                    |
    /// |-------------|---------------|----------|------------|--------------|------------------|--------|-------------------------------------------|
    /// | T           | -             | -        | -          | -            | -                | FALSE  | Stack matches current shift operation    |
    /// | F           | T             | T        | -          | -            | -                | TRUE   | Currency item received during sale       |
    /// | F           | T             | F        | T          | -            | -                | FALSE  | Storage container is currently open      |
    /// | F           | T             | F        | F          | T            | -                | FALSE  | Vehicle storage window is currently open |
    /// | F           | T             | F        | F          | F            | T                | FALSE  | Workstation window is currently open     |
    /// | F           | T             | F        | F          | F            | F                | TRUE   | No conditions preventing notification    |
    /// | F           | F             | -        | T          | -            | -                | FALSE  | Storage container is currently open      |
    /// | F           | F             | -        | F          | T            | -                | FALSE  | Vehicle storage window is currently open |
    /// | F           | F             | -        | F          | F            | T                | FALSE  | Workstation window is currently open     |
    /// | F           | F             | -        | F          | F            | F                | TRUE   | No conditions preventing notification    |
    /// 
    /// Notes:
    /// - STACK_SHIFT=T always returns FALSE immediately (highest priority)
    /// - CURRENCY_ITEM=T + MID_SALE=T returns TRUE immediately (bypasses storage checks)
    /// - CURRENCY_ITEM=T + MID_SALE=F continues to storage window checks
    /// - Storage window checks are evaluated in order: CRATE_OPEN, VEHICLE_OPEN, WORKSTATION_OPEN
    /// - Default case when no blocking conditions met: TRUE
    /// 
    /// </summary>

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_CollectedItemList.RemoveItemStack))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool Intercept_RemoveItemStack_Prefix(XUiC_CollectedItemList __instance, ItemStack _is)
    {
#if DEBUG
#endif
        var itemInfo = ItemX.Info(_is);

        // Check if we should show the notification and get the reason
        if (ShouldShowItemStackNotification(_is, out string reason))
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Proceeding with RemoveItemStack for stack ({itemInfo}). Reason: {reason}");
#endif
            return true; // Proceed with original method execution
        }

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Skipping RemoveItemStack (notification) for stack ({itemInfo}). Reason: {reason}");
#endif
        return false; // Skip original method execution
    }
}