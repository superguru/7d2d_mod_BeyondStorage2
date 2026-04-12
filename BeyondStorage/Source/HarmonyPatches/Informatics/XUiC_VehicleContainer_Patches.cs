using System.Linq;
using BeyondStorage.Source.Data;
using BeyondStorage.Source.Game.UI;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_VehicleContainer))]
internal static class XUiC_VehicleContainer_Patches
{
    // Store the previous LockedSlots state for comparison
    private static PackedBoolArray s_previousLockedSlots = null;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_VehicleContainer.Init))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_VehicleContainer_Init_Postfix(XUiC_VehicleContainer __instance)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiC_VehicleContainer_Init_Postfix);
#endif
        var btnBeyondSmartPushButton = UIControlHelpers.GetSmartVehiclePushButton(__instance);
        if (btnBeyondSmartPushButton != null)
        {
            btnBeyondSmartPushButton.OnPress += SmartSortingCommon.SmartVehiclePush_EventHandler;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Smart vehicle push button initialized");
#endif
        }

        var btnBeyondSmartPullButton = UIControlHelpers.GetSmartVehiclePullLoadoutButton(__instance);
        if (btnBeyondSmartPullButton != null)
        {
            btnBeyondSmartPullButton.OnPress += SmartSortingCommon.SmartVehiclePullLoadout_EventHandler;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Smart vehicle pull loadout button initialized");
#endif
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_VehicleContainer.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_VehicleContainer_UpdateLockedSlots_Prefix(XUiC_VehicleContainer __instance, XUiC_ContainerStandardControls _csc)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiC_VehicleContainer_UpdateLockedSlots_Prefix);
#endif
        if (_csc == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: _csc parameter is null");
#endif
            s_previousLockedSlots = null;
            return;
        }

        // Save the current LockedSlots state before the update
        s_previousLockedSlots = _csc.LockedSlots;

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Saved LockedSlots state: {(s_previousLockedSlots != null ? $"Count={s_previousLockedSlots.Length}" : "null")}");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_VehicleContainer.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_VehicleContainer_UpdateLockedSlots_Postfix(XUiC_VehicleContainer __instance, XUiC_ContainerStandardControls _csc)
    {
        if (_csc != null)
        {
            var currentLockedSlots = _csc.LockedSlots;
            if (currentLockedSlots == null)
            {
                return;
            }

            ItemStack itemStack = null;

            // For vehicle containers, we need to check the vehicle's bag items for currency
            var vehicleBag = __instance?.xui?.vehicle?.bag;
            if (vehicleBag?.items != null)
            {
                // Check if any of the vehicle bag items contain currency
                bool containsCurrency = vehicleBag.items.Any(item => CurrencyCache.IsCurrencyItem(item));
                if (containsCurrency)
                {
                    // Trigger a currency refresh after slot lock changes when currency is present
                    itemStack = CurrencyCache.GetEmptyCurrencyStack();
                }
            }

            UIRefreshHelper.LogAndRefreshUI(StackOps.Stack_LockStateChange_Operation, itemStack: itemStack, callCount: 0);
        }
    }

#if DEBUG
    /// <summary>
    /// Compares two PackedBoolArray instances for equality.
    /// Returns true if both are null, or if both have the same content.
    /// </summary>
    private static bool AreLockedSlotsEqual(PackedBoolArray previous, PackedBoolArray current)
    {
        // Both null - equal
        if (previous == null && current == null)
        {
            return true;
        }

        // One null, one not - not equal
        if (previous == null || current == null)
        {
            return false;
        }

        // Different lengths - not equal
        if (previous.Length != current.Length)
        {
            return false;
        }

        // Compare each element
        for (int i = 0; i < previous.Length; i++)
        {
            if (previous[i] != current[i])
            {
                return false;
            }
        }

        return true;
    }
#endif
}
