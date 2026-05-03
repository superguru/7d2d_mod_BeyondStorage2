using System.Linq;

namespace BeyondStorage.Game.UI;

/// <summary>
/// Manages the state of various UI windows for tracking open containers and storage interfaces
/// </summary>
public static class WindowStateManager
{
    private static XUiC_VehicleStorageWindowGroup s_vehicleWindowInstance = null;
    private static readonly object s_vehicleLockObject = new();

    private static XUiC_LootWindow s_lootWindowInstance = null;
    private static bool s_isPlayerStorageWindowOpen = false;
    private static EntityDrone s_droneForWindow;
    private static XUiC_BackpackWindow s_backpackWindowInstance = null;
    private static readonly object s_lootLockObject = new();

    private static XUiC_WorkstationWindowGroup s_workstationWindowInstance = null;
    private static readonly object s_workstationLockObject = new();

    private static XUiC_DewCollectorWindowGroup s_collectorWindowInstance = null;
    private static readonly object s_collectorLockObject = new();

    #region Vehicle Storage Window

    /// <summary>
    /// Gets whether a vehicle storage window is currently open
    /// </summary>
    /// <returns>True if a vehicle storage window is open, false otherwise</returns>
    public static bool IsVehicleStorageWindowOpen()
    {
        lock (s_vehicleLockObject)
        {
            return s_vehicleWindowInstance != null;
        }
    }

    /// <summary>
    /// Gets the currently active vehicle storage window instance
    /// </summary>
    /// <returns>The active vehicle storage window instance, or null if none is open</returns>
    public static XUiC_VehicleStorageWindowGroup GetActiveVehicleStorageWindow()
    {
        lock (s_vehicleLockObject)
        {
            return s_vehicleWindowInstance;
        }
    }

    /// <summary>
    /// Checks if the specified vehicle storage window is the currently active one
    /// </summary>
    /// <param name="window">The window to check</param>
    /// <returns>True if the window is the currently active vehicle storage window</returns>
    public static bool IsCurrentlyActiveVehicleWindow(XUiC_VehicleStorageWindowGroup window)
    {
        lock (s_vehicleLockObject)
        {
            return s_vehicleWindowInstance != null && s_vehicleWindowInstance == window;
        }
    }

    /// <summary>
    /// Called when a vehicle storage window opens
    /// </summary>
    /// <param name="window">The vehicle storage window that opened</param>
    internal static void OnVehicleStorageWindowOpened(XUiC_VehicleStorageWindowGroup window)
    {
        lock (s_vehicleLockObject)
        {
            if (s_vehicleWindowInstance != null)
            {
                // Log warning and reset state to prevent confusion
                Log.Warning($"[WindowStateManager] Vehicle storage window opened while another was already tracked. Resetting state. Previous: {s_vehicleWindowInstance?.GetType().Name}, New: {window?.GetType().Name}");
                s_vehicleWindowInstance = null;
            }

            s_vehicleWindowInstance = window;
        }
    }

    /// <summary>
    /// Called when a vehicle storage window closes
    /// </summary>
    /// <param name="window">The vehicle storage window that closed</param>
    internal static void OnVehicleStorageWindowClosed(XUiC_VehicleStorageWindowGroup window)
    {
        lock (s_vehicleLockObject)
        {
            // Only clear state if the closing window is the currently tracked one
            if (s_vehicleWindowInstance == window)
            {
                s_vehicleWindowInstance = null;
            }
            else if (s_vehicleWindowInstance != null)
            {
                Log.Warning($"[WindowStateManager] Attempted to close vehicle storage window that doesn't match tracked instance.");
            }
        }
    }

    /// <summary>
    /// Gets the vehicle entity associated with the currently open vehicle storage window
    /// </summary>
    /// <returns>The active vehicle entity, or null if no vehicle storage window is open</returns>
    internal static EntityVehicle GetOpenVehicleTileEntity()
    {
        var vehicleWindow = GetActiveVehicleStorageWindow();
        return vehicleWindow?.CurrentVehicleEntity;
    }

    /// <summary>
    /// Marks the currently open vehicle entity's storage as modified and refreshes all related UI state.
    /// Notifies the vehicle window, container window, and triggers a bag-changed event.
    /// </summary>
    internal static void SetOpenVehicleEntityModified()
    {
        lock (s_vehicleLockObject)
        {
            var vehicle = s_vehicleWindowInstance?.CurrentVehicleEntity;
            if (vehicle != null)
            {
                s_vehicleWindowInstance.IsDirty = true;
                s_vehicleWindowInstance.SetAllChildrenDirty();

                var containerWindow = s_vehicleWindowInstance.containerWindow;
                if (containerWindow != null)
                {
                    containerWindow.IsDirty = true;
                    containerWindow.SetAllChildrenDirty();

                    containerWindow.OnBagItemChangedInternal();
                }
            }
        }
    }

    #endregion

    #region Storage Container (Loot) Window

    /// <summary>
    /// Gets whether a storage container window is currently open
    /// </summary>
    /// <returns>True if a storage container window is open, false otherwise</returns>
    /// <remarks>
    /// This method only returns true for storage containers (chests, safes, etc.) and drones.
    /// Random loot containers in the world (abandoned cars, dumpsters, etc.) are not considered storage.
    /// </remarks>
    public static bool IsPlayerStorageOpen()
    {
        lock (s_lootLockObject)
        {
            // If it isn't storage, then it's some random loot container out in the world.
            // Maybe an abandoned car. Maybe a dumpster. Who knows?
            return s_isPlayerStorageWindowOpen;
        }
    }

    /// <summary>
    /// Gets the drone associated with the currently open storage container window, if any
    /// </summary>
    /// <returns>The drone for the open storage container, or null if none is open or not a drone</returns>
    public static EntityDrone GetDroneForOpenStorageContainer()
    {
        lock (s_lootLockObject)
        {
            return s_isPlayerStorageWindowOpen ? s_droneForWindow : null;
        }
    }

    /// <summary>
    /// Gets the currently active storage container window instance
    /// </summary>
    /// <returns>The active storage container window instance, or null if none is open</returns>
    public static XUiC_LootWindow GetActiveStorageContainerWindow()
    {
        lock (s_lootLockObject)
        {
            return s_isPlayerStorageWindowOpen ? s_lootWindowInstance : null;
        }
    }

    /// <summary>
    /// Gets the lootable tile entity associated with the currently open storage container window
    /// </summary>
    /// <returns>The active lootable tile entity, or null if no storage container window is open</returns>
    internal static ITileEntityLootable GetOpenWindowLootable()
    {
        var lootWindow = GetActiveStorageContainerWindow();
        return lootWindow?.te;
    }

    public static bool IsLootContainerWindowOpen()
    {
        lock (s_lootLockObject)
        {
            return s_lootWindowInstance != null;
        }
    }

    /// <summary>
    /// Called when a storage container window opens
    /// </summary>
    /// <param name="window">The storage container window that opened</param>
    /// <param name="isStorage">True if this is a storage container (not a random world loot container)</param>
    /// <param name="drone">The drone associated with the storage container, if any</param>
    internal static void OnStorageWindowOpened(XUiC_LootWindow window, bool isStorage, EntityDrone drone)
    {
        lock (s_lootLockObject)
        {
            if (s_isPlayerStorageWindowOpen || (s_lootWindowInstance != null))
            {
                // Log warning and reset state to prevent confusion - this can happen with multiple containers
                Log.Warning($"[WindowStateManager] Storage container window opened while another was already tracked. Resetting state. Previous: {s_lootWindowInstance?.GetType().Name}, New: {window?.GetType().Name}");
                s_isPlayerStorageWindowOpen = false;
                s_lootWindowInstance = null;
                s_droneForWindow = null;
            }

            s_lootWindowInstance = window;
            s_isPlayerStorageWindowOpen = isStorage;
            s_droneForWindow = drone;
        }
    }

    /// <summary>
    /// Called when a storage container window closes
    /// </summary>
    /// <param name="window">The storage container window that closed</param>
    internal static void OnStorageWindowClosed(XUiC_LootWindow window)
    {
        lock (s_lootLockObject)
        {
            // Only clear state if the closing window is the currently tracked one
            if (window == s_lootWindowInstance)
            {
                s_lootWindowInstance = null;
                s_isPlayerStorageWindowOpen = false;
                s_droneForWindow = null;
            }
            else if (s_lootWindowInstance != null)
            {
                Log.Warning($"[WindowStateManager] Attempted to close storage container window that doesn't match tracked instance.");
            }
        }
    }

    #endregion

    #region Backpack Window

    /// <summary>
    /// Gets the currently active backpack window instance
    /// </summary>
    /// <returns>The active backpack window instance, or null if none is open</returns>
    public static XUiC_BackpackWindow GetActiveBackpackWindow()
    {
        lock (s_lootLockObject)
        {
            return s_backpackWindowInstance;
        }
    }

    /// <summary>
    /// Gets whether the player backpack window is currently open
    /// </summary>
    /// <returns>True if the backpack window is open, false otherwise</returns>
    public static bool IsBackpackWindowOpen()
    {
        lock (s_lootLockObject)
        {
            return s_backpackWindowInstance != null;
        }
    }

    public static string IsOnlyPlayerBackpackOpen()
    {
        // Player backpack window has not actually opened yet when this is called from GetBindingValue. It is about to open.
        // A workstation/collector does not have a "loot window" that opens for it, there is just the fuel and output, so by this logic,
        // a workstation or collector falls under the "only backpack open" category, which is what we want.

        bool result =
            !IsDroneWindowOpen() &&
            !IsVehicleStorageWindowOpen() &&
            !IsPlayerStorageOpen() &&
            !IsLootContainerWindowOpen();

#if DEBUG
        //ModLogger.DebugLog($"IsPlayerBackpackOpenOnly: {result} (Drone: {IsDroneWindowOpen()}, Vehicle: {IsVehicleStorageWindowOpen()}, Workstation: {IsWorkstationWindowOpen()}, Collector: {IsCollectorWindowOpen()}, PlayerStorage: {IsPlayerStorageOpen()}, LootContainer: {IsLootContainerWindowOpen()})");
#endif

        return result.ToString();
    }


    /// <summary>
    /// Called when a backpack window opens
    /// </summary>
    /// <param name="backpackWindow">The backpack window that opened</param>
    internal static void OnBackpackWindowOpened(XUiC_BackpackWindow backpackWindow)
    {
        lock (s_lootLockObject)
        {
            if (s_backpackWindowInstance != null)
            {
                // Log warning and reset state to prevent confusion - this can happen with multiple containers
                Log.Warning($"[WindowStateManager] Backpack window opened while another was already tracked. Resetting state. Previous: {s_backpackWindowInstance?.GetType().Name}, New: {backpackWindow?.GetType().Name}");
            }

            s_backpackWindowInstance = backpackWindow;
        }
    }

    /// <summary>
    /// Called when a backpack window closes
    /// </summary>
    /// <param name="backpackWindow">The backpack window that closed</param>
    internal static void OnBackpackWindowClosed(XUiC_BackpackWindow backpackWindow)
    {
        lock (s_lootLockObject)
        {
            // Only clear state if the closing window is the currently tracked one
            if (backpackWindow != s_backpackWindowInstance && s_backpackWindowInstance != null)
            {
                Log.Warning($"[WindowStateManager] Attempted to close backpack window that doesn't match tracked instance.");
            }

            s_backpackWindowInstance = null;
        }
    }

    #endregion

    #region Drone Detection

    /// <summary>
    /// Gets whether a drone storage window is currently open
    /// </summary>
    /// <returns>True if a storage container window is open and it belongs to a drone, false otherwise</returns>
    public static bool IsDroneWindowOpen()
    {
        lock (s_lootLockObject)
        {
            return s_droneForWindow != null;
        }
    }

    /// <summary>
    /// Determines if the specified tile entity represents a drone loot window.
    /// </summary>
    /// <param name="tileEntity">The tile entity to check</param>
    /// <returns>True if the tile entity is a drone; otherwise, false</returns>
    public static bool IsDroneWindow(ITileEntity tileEntity)
    {
        return IsDroneWindow(tileEntity, out _, out _, out _);
    }

    /// <summary>
    /// Determines if the specified tile entity represents a drone loot window,
    /// providing additional diagnostic information about the match.
    /// </summary>
    /// <param name="tileEntity">The tile entity to check</param>
    /// <param name="drone">Output parameter for the matched drone entity, if found</param>
    /// <param name="matchedTypeName">Output parameter for the matched type name (currently unused)</param>
    /// <param name="matchReason">Output parameter describing why the check succeeded or failed</param>
    /// <returns>True if the tile entity is a drone; otherwise, false</returns>
    public static bool IsDroneWindow(ITileEntity tileEntity, out EntityDrone drone, out string matchedTypeName, out string matchReason)
    {
        matchedTypeName = string.Empty;
        matchReason = string.Empty;
        drone = null;

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
        drone = drones.FirstOrDefault(d => d.EntityId == entityId);
        if (drone != null)
        {
            matchReason = "Matching entity id in active drone list";
            return true;
        }

        matchReason = $"No match found for {tileEntity}";
        return false;
    }

    #endregion

    #region Workstation Window

    /// <summary>
    /// Gets whether a workstation window is currently open
    /// </summary>
    /// <returns>True if a workstation window is open, false otherwise</returns>
    public static bool IsWorkstationWindowOpen()
    {
        lock (s_workstationLockObject)
        {
            return s_workstationWindowInstance != null;
        }
    }

    /// <summary>
    /// Gets the currently active workstation window instance
    /// </summary>
    /// <returns>The active workstation window instance, or null if none is open</returns>
    public static XUiC_WorkstationWindowGroup GetActiveWorkstationWindow()
    {
        lock (s_workstationLockObject)
        {
            return s_workstationWindowInstance;
        }
    }

    /// <summary>
    /// Checks if the specified workstation window is the currently active one
    /// </summary>
    /// <param name="window">The window to check</param>
    /// <returns>True if the window is the currently active workstation window</returns>
    public static bool IsCurrentlyActiveWorkstationWindow(XUiC_WorkstationWindowGroup window)
    {
        lock (s_workstationLockObject)
        {
            return s_workstationWindowInstance != null && s_workstationWindowInstance == window;
        }
    }

    /// <summary>
    /// Called when a workstation window opens
    /// </summary>
    /// <param name="window">The workstation window that opened</param>
    internal static void OnWorkstationWindowOpened(XUiC_WorkstationWindowGroup window)
    {
        lock (s_workstationLockObject)
        {
            if (s_workstationWindowInstance != null)
            {
                // Log warning and reset state to prevent confusion
                Log.Warning($"[WindowStateManager] Workstation window opened while another was already tracked. Resetting state. Previous: {s_workstationWindowInstance?.GetType().Name}, New: {window?.GetType().Name}");
                s_workstationWindowInstance = null;
            }

            s_workstationWindowInstance = window;
        }
    }

    /// <summary>
    /// Called when a workstation window closes
    /// </summary>
    /// <param name="window">The workstation window that closed</param>
    internal static void OnWorkstationWindowClosed(XUiC_WorkstationWindowGroup window)
    {
        lock (s_workstationLockObject)
        {
            // Only clear state if the closing window is the currently tracked one
            if (s_workstationWindowInstance == window)
            {
                s_workstationWindowInstance = null;
            }
            else if (s_workstationWindowInstance != null)
            {
                Log.Warning($"[WindowStateManager] Attempted to close workstation window that doesn't match tracked instance.");
            }
        }
    }

    internal static TileEntityWorkstation GetOpenWorkstationTileEntity()
    {
        var workstationWindow = GetActiveWorkstationWindow();
        return workstationWindow?.WorkstationData?.TileEntity;
    }

    #endregion

    #region Collector Window

    /// <summary>
    /// Gets the currently active dew collector window instance
    /// </summary>
    /// <returns>The active collector window instance, or null if none is open</returns>
    public static XUiC_DewCollectorWindowGroup GetActiveCollectorWindow()
    {
        lock (s_collectorLockObject)
        {
            return s_collectorWindowInstance;
        }
    }

    /// <summary>
    /// Gets whether a dew collector window is currently open
    /// </summary>
    /// <returns>True if a collector window is open, false otherwise</returns>
    public static bool IsCollectorWindowOpen()
    {
        lock (s_collectorLockObject)
        {
            return s_collectorWindowInstance != null;
        }
    }

    /// <summary>
    /// Called when a dew collector window opens
    /// </summary>
    /// <param name="window">The dew collector window that opened</param>
    internal static void OnCollectorWindowOpened(XUiC_DewCollectorWindowGroup window)
    {
        lock (s_collectorLockObject)
        {
            if (s_collectorWindowInstance != null)
            {
                // Log warning and reset state to prevent confusion
                Log.Warning($"[WindowStateManager] Collector window opened while another was already tracked. Resetting state. Previous: {s_collectorWindowInstance?.GetType().Name}, New: {window?.GetType().Name}");
                s_collectorWindowInstance = null;
            }

            s_collectorWindowInstance = window;
        }
    }

    /// <summary>
    /// Called when a dew collector window closes
    /// </summary>
    /// <param name="window">The dew collector window that closed</param>
    internal static void OnCollectorWindowClosed(XUiC_DewCollectorWindowGroup window)
    {
        lock (s_collectorLockObject)
        {
            // Only clear state if the closing window is the currently tracked one
            if (s_collectorWindowInstance == window)
            {
                s_collectorWindowInstance = null;
            }
            else if (s_collectorWindowInstance != null)
            {
                Log.Warning($"[WindowStateManager] Attempted to close collector window that doesn't match tracked instance.");
            }
        }
    }

    public static TileEntityCollector GetOpenCollectorTileEntity()
    {
        var collectorWindow = GetActiveCollectorWindow();
        return collectorWindow?.te;
    }

    #endregion
}