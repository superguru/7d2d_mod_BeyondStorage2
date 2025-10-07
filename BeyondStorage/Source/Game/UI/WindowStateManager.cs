namespace BeyondStorage.Source.Game.UI;

/// <summary>
/// Manages the state of various UI windows for tracking open containers and storage interfaces
/// </summary>
public static class WindowStateManager
{
    private static XUiC_VehicleStorageWindowGroup s_vehicleWindowInstance = null;
    private static bool s_isVehicleStorageWindowOpen = false;
    private static readonly object s_vehicleLockObject = new();

    private static XUiC_LootWindow s_lootWindowInstance = null;
    private static bool s_isStorageLootWindowOpen = false;
    private static readonly object s_lootLockObject = new();

    private static XUiC_WorkstationWindowGroup s_workstationWindowInstance = null;
    private static bool s_isWorkstationWindowOpen = false;
    private static readonly object s_workstationLockObject = new();

    #region Vehicle Storage Window

    /// <summary>
    /// Gets whether a vehicle storage window is currently open
    /// </summary>
    /// <returns>True if a vehicle storage window is open, false otherwise</returns>
    public static bool IsVehicleStorageWindowOpen()
    {
        lock (s_vehicleLockObject)
        {
            return s_isVehicleStorageWindowOpen;
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
            return s_isVehicleStorageWindowOpen ? s_vehicleWindowInstance : null;
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
            return s_isVehicleStorageWindowOpen && (s_vehicleWindowInstance == window);
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
            if (s_isVehicleStorageWindowOpen || (s_vehicleWindowInstance != null))
            {
                // Log error and reset state to prevent confusion
                Log.Warning($"[WindowStateManager] Vehicle storage window opened while another was already tracked. Resetting state. Previous: {s_vehicleWindowInstance?.GetType().Name}, New: {window?.GetType().Name}");
                s_isVehicleStorageWindowOpen = false;
                s_vehicleWindowInstance = null;
            }

            s_vehicleWindowInstance = window;
            s_isVehicleStorageWindowOpen = true;
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
                s_isVehicleStorageWindowOpen = false;
            }
            else if (s_vehicleWindowInstance != null)
            {
                Log.Warning($"[WindowStateManager] Attempted to close vehicle storage window that doesn't match tracked instance. Tracked: {s_vehicleWindowInstance?.GetType().Name}, Closing: {window?.GetType().Name}");
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
    public static bool IsStorageContainerOpen()
    {
        lock (s_lootLockObject)
        {
            // If it isn't storage, then it's some random loot container out in the world.
            // Maybe an abandoned car. Maybe a dumpster. Who knows?
            return s_isStorageLootWindowOpen;
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
            return s_isStorageLootWindowOpen ? s_lootWindowInstance : null;
        }
    }

    /// <summary>
    /// Checks if the specified storage container window is the currently active one
    /// </summary>
    /// <param name="window">The window to check</param>
    /// <returns>True if the window is the currently active storage container window</returns>
    public static bool IsCurrentlyActiveStorageContainerWindow(XUiC_LootWindow window)
    {
        lock (s_lootLockObject)
        {
            return s_isStorageLootWindowOpen && (s_lootWindowInstance == window);
        }
    }

    /// <summary>
    /// Called when a storage container window opens
    /// </summary>
    /// <param name="window">The storage container window that opened</param>
    /// <param name="isStorage">True if this is a storage container (not a random world loot container)</param>
    internal static void OnStorageContainerWindowOpened(XUiC_LootWindow window, bool isStorage)
    {
        lock (s_lootLockObject)
        {
            if (s_isStorageLootWindowOpen || (s_lootWindowInstance != null))
            {
                // Log warning and reset state to prevent confusion - this can happen with multiple containers
                Log.Warning($"[WindowStateManager] Storage container window opened while another was already tracked. Resetting state. Previous: {s_lootWindowInstance?.GetType().Name}, New: {window?.GetType().Name}");
                s_isStorageLootWindowOpen = false;
                s_lootWindowInstance = null;
            }

            s_lootWindowInstance = window;
            s_isStorageLootWindowOpen = isStorage;
        }
    }

    /// <summary>
    /// Called when a storage container window closes
    /// </summary>
    /// <param name="window">The storage container window that closed</param>
    internal static void OnStorageContainerWindowClosed(XUiC_LootWindow window)
    {
        lock (s_lootLockObject)
        {
            // Only clear state if the closing window is the currently tracked one
            if (s_lootWindowInstance == window)
            {
                s_lootWindowInstance = null;
                s_isStorageLootWindowOpen = false;
            }
            else if (s_lootWindowInstance != null)
            {
                Log.Warning($"[WindowStateManager] Attempted to close storage container window that doesn't match tracked instance. Tracked: {s_lootWindowInstance?.GetType().Name}, Closing: {window?.GetType().Name}");
            }
        }
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
            return s_isWorkstationWindowOpen;
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
            return s_isWorkstationWindowOpen ? s_workstationWindowInstance : null;
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
            return s_isWorkstationWindowOpen && (s_workstationWindowInstance == window);
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
            if (s_isWorkstationWindowOpen || (s_workstationWindowInstance != null))
            {
                // Log error and reset state to prevent confusion
                Log.Warning($"[WindowStateManager] Workstation window opened while another was already tracked. Resetting state. Previous: {s_workstationWindowInstance?.GetType().Name}, New: {window?.GetType().Name}");
                s_isWorkstationWindowOpen = false;
                s_workstationWindowInstance = null;
            }

            s_workstationWindowInstance = window;
            s_isWorkstationWindowOpen = true;
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
                s_isWorkstationWindowOpen = false;
            }
            else if (s_workstationWindowInstance != null)
            {
                Log.Warning($"[WindowStateManager] Attempted to close workstation window that doesn't match tracked instance. Tracked: {s_workstationWindowInstance?.GetType().Name}, Closing: {window?.GetType().Name}");
            }
        }
    }

    #endregion
}