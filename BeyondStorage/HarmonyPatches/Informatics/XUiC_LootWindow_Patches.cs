using System.Linq;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_LootWindow))]
public class XUiC_LootWindow_Patches
{
    private static XUiC_LootWindow s_windowInstance = null;
    private static bool s_isStorageLootWindowOpen = false;
    private static readonly object s_lockObject = new();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_UpdateLockedSlots_Postfix(XUiC_ContainerStandardControls _csc)
    {
        const string d_MethodName = nameof(XUiC_LootWindow_UpdateLockedSlots_Postfix);

        UIRefreshHelper.LogAndRefreshUI(d_MethodName);
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
                ModLogger.DebugLog($"{d_MethodName}: LootWindow opened for storage: {storage}");
#endif
            }

            if (!s_isStorageLootWindowOpen)
            {
                if (IsDroneWindow(tileEntity, out string matchedTypeName, out string reason))
                {
                    s_isStorageLootWindowOpen = true;
#if DEBUG
                    ModLogger.DebugLog($"{d_MethodName}: LootWindow opened for drone. Reason: {reason}");
#endif
                }
            }

#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: LootWindow opened for player storage: {s_isStorageLootWindowOpen}, te {tileEntity}");
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
        const string d_MethodName = nameof(XUiC_LootWindow_OnClose_Postfix);

        lock (s_lockObject)
        {
            ModLogger.DebugLog($"{d_MethodName}: LootWindow closed");
            s_windowInstance = null;
            s_isStorageLootWindowOpen = false;
        }
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
