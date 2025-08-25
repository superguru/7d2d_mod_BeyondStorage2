using System.Linq;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_LootWindow))]
internal static class XUiC_LootWindow_Patches
{
    private static XUiC_LootWindow s_windowInstance = null;
    private static bool s_isStorageLootWindowOpen = false;
    private static readonly object s_lockObject = new();

    // Store the previous LockedSlots state for comparison
    private static PackedBoolArray s_previousLockedSlots = null;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_LootWindow.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_UpdateLockedSlots_Prefix(XUiC_LootWindow __instance, XUiC_ContainerStandardControls _csc)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiC_LootWindow_UpdateLockedSlots_Prefix);
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
        //ModLogger.DebugLog($"{d_MethodName}: Saved LockedSlots state: {(s_previousLockedSlots != null ? $"Count={s_previousLockedSlots.Length}" : "null")}");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_UpdateLockedSlots_Postfix(XUiC_LootWindow __instance, XUiC_ContainerStandardControls _csc)
    {
        if (_csc != null)
        {
            var currentLockedSlots = _csc.LockedSlots;
            if (currentLockedSlots == null)
            {
                return;
            }

            ItemStack itemStack = null;

            var slots = __instance?.lootContainer?.GetSlots();
            if (slots != null)
            {
                // Check if any of the slots contain currency items
                bool containsCurrency = slots.Any(slot => CurrencyCache.IsCurrencyItem(slot));
                if (containsCurrency)
                {
                    // Trigger a currency refresh after slot lock changes when currency is present
                    itemStack = CurrencyCache.GetEmptyCurrencyStack();
                }
            }

            UIRefreshHelper.LogAndRefreshUI(StackOps.Stack_LockStateChange_Operation, itemStack: itemStack, callCount: 0);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.OnOpen))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_OnOpen_Postfix(XUiC_LootWindow __instance)
    {
        const string d_MethodName = nameof(XUiC_LootWindow_OnOpen_Postfix);

        lock (s_lockObject)
        {
            if (s_isStorageLootWindowOpen || (s_windowInstance != null))
            {
                ModLogger.DebugLog($"{d_MethodName}: LootWindow is already open for storage. This should not happen!");

                s_isStorageLootWindowOpen = false; // Reset the flag to prevent confusion
                s_windowInstance = null;
            }

            var tileEntity = __instance?.te;
            if (tileEntity == null)
            {
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: TileEntity is null, cannot determine if this is a storage container.");
#endif
                return;
            }

            s_windowInstance = __instance;

            // Check for TEFeatureStorage using comprehensive feature detection
            if (tileEntity.TryGetSelfOrFeature(out TEFeatureStorage storage) && storage != null)
            {
                s_isStorageLootWindowOpen = true;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: LootWindow opened for storage: {storage}");
#endif
            }

            if (!s_isStorageLootWindowOpen)
            {
                if (IsDroneWindow(tileEntity, out string matchedTypeName, out string reason))
                {
                    s_isStorageLootWindowOpen = true;
#if DEBUG
                    //ModLogger.DebugLog($"{d_MethodName}: LootWindow opened for drone. Reason: {reason}");
#endif
                }
            }

#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: LootWindow opened for player storage: {s_isStorageLootWindowOpen}, te {tileEntity}");
#endif
        }
    }

    private static bool IsDroneWindow(ITileEntity tileEntity, out string matchedTypeName, out string matchReason)
    {
        matchedTypeName = string.Empty;
        matchReason = string.Empty;

        if (tileEntity == null)
        {
            matchReason = "TileEntity is null";
            return false;
        }

        var drones = DroneManager.Instance?.dronesActive;
        if (drones == null)
        {
            matchReason = "No drones, cannot determine if this is a drone loot window";
            return false;
        }

        var entityId = tileEntity.EntityId;
        if (drones.Any(drone => drone.EntityId == entityId))
        {
            matchReason = "Matching entity id in active drone list";
            return true;
        }

        matchReason = $"No match found for {tileEntity}";
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.OnClose))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_OnClose_Postfix(XUiC_LootWindow __instance)
    {
        lock (s_lockObject)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: LootWindow closed");
#endif
            s_windowInstance = null;
            s_isStorageLootWindowOpen = false;
        }

        // Clear the saved locked slots state when the window closes
        s_previousLockedSlots = null;
    }

    public static bool IsStorageContainerOpen()
    {
        lock (s_lockObject)
        {
            // If it isn't storage, then it's some random loot container out in the world.
            // Maybe an abandoned car. Maybe a dumpster. Who knows?
            return s_isStorageLootWindowOpen;
        }
    }
}
