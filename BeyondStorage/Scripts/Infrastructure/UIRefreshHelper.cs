using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Infrastructure;

/// <summary>
/// Utility class for refreshing UI components when storage changes affect game state.
/// Provides common functionality for validating UI contexts and refreshing all windows.
/// </summary>
public static class UIRefreshHelper
{
    /// <summary>
    /// Validates UI components are available and refreshes all windows if valid.
    /// This is commonly needed when storage operations affect the game state and UI needs to be updated.
    /// </summary>
    /// <param name="context">The storage context containing world and player information</param>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <returns>True if UI components were valid and refresh was performed, false otherwise</returns>
    public static bool ValidateAndRefreshUI(StorageContext context, string methodName)
    {
        if (!ValidateUIComponents(context, methodName))
        {
            return false;
        }

        // Now completely safe to access without null-conditional operators
        RefreshAllWindowsInternal(context, includeViewComponents: true);
        return true;
    }

    /// <summary>
    /// Validates UI components are available and refreshes all windows if valid.
    /// Creates a StorageContext internally to access world and player information.
    /// </summary>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <returns>True if UI components were valid and refresh was performed, false otherwise</returns>
    public static bool ValidateAndRefreshUI(string methodName)
    {
        if (!ValidationHelper.ValidateStorageContext(methodName, out StorageContext context))
        {
            return false;
        }

        return ValidateAndRefreshUI(context, methodName);
    }

    /// <summary>
    /// Validates UI components without performing a refresh.
    /// Useful for checking if UI operations are possible before proceeding with expensive operations.
    /// </summary>
    /// <param name="context">The storage context containing world and player information</param>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <returns>True if UI components are valid, false otherwise</returns>
    public static bool ValidateUIComponents(StorageContext context, string methodName)
    {
        if (context?.WorldPlayerContext?.Player?.playerUI?.xui == null)
        {
            ModLogger.DebugLog($"{methodName}: Required UI components are null");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates UI components without performing a refresh.
    /// Creates a StorageContext internally to access world and player information.
    /// </summary>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <returns>True if UI components are valid, false otherwise</returns>
    public static bool ValidateUIComponents(string methodName)
    {
        if (!ValidationHelper.ValidateStorageContext(methodName, out StorageContext context))
        {
            return false;
        }

        return ValidateUIComponents(context, methodName);
    }

    /// <summary>
    /// Performs a UI refresh assuming UI components have already been validated.
    /// Should only be called after ValidateUIComponents returns true.
    /// For this reason, the method is private to ensure it is not misused.
    /// </summary>
    /// <param name="context">The storage context containing world and player information</param>
    /// <param name="includeViewComponents">Whether to include view components in the refresh</param>
    private static void RefreshAllWindowsInternal(StorageContext context, bool includeViewComponents = true)
    {
        // Caller is responsible for validation - this method assumes components are valid
        context.WorldPlayerContext.Player.playerUI.xui.RefreshAllWindows(_includeViewComponents: includeViewComponents);
    }

    /// <summary>
    /// Performs a UI refresh, creating a StorageContext internally and validating components.
    /// This is a convenience method that handles context creation and validation automatically.
    /// </summary>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <param name="includeViewComponents">Whether to include view components in the refresh</param>
    /// <returns>True if refresh was performed successfully, false if validation failed</returns>
    public static bool RefreshAllWindows(string methodName, bool includeViewComponents = true)
    {
        if (!ValidationHelper.ValidateStorageContext(methodName, out StorageContext context))
        {
            return false;
        }

        if (!ValidateUIComponents(context, methodName))
        {
            return false;
        }

        RefreshAllWindowsInternal(context, includeViewComponents);
        return true;
    }
}