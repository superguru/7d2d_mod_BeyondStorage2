using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.UI;

/// <summary>
/// Utility class for refreshing UI components when storage changes affect game state.
/// Provides common functionality for validating UI contexts and refreshing all windows.
/// </summary>
public static class UIRefreshHelper
{
    private const double CACHE_INVALIDATION_THRESHOLD_SECONDS = 0.4;
    private static readonly Dictionary<string, DateTime> s_lastRefreshTimes = new();
    private static readonly object s_lockObject = new();

    /// <summary>
    /// Logs a formatted debug message and triggers UI refresh for single item drop operations
    /// </summary>
    /// <param name="methodName">The calling method name</param>
    /// <param name="callCount">The call counter value</param>
    /// <param name="message">The message to log</param>
    public static void LogAndRefreshUI(string methodName, long callCount)
    {
        string callStr = " ";
#if DEBUG
        if (callCount > 0)
        {
            callStr = $"call #{callCount} ";
        }
        ModLogger.DebugLog($"{methodName}:{callStr} REFRESH_UI");
#endif
        RefreshAllWindows(methodName, includeViewComponents: true);
    }

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

        // Check if we need to invalidate cache due to rapid successive calls
        CheckAndInvalidateCacheIfNeeded(methodName);

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
        // Check if we need to invalidate cache due to rapid successive calls
        CheckAndInvalidateCacheIfNeeded(methodName);

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
        //context.WorldPlayerContext.Player.playerUI.xui.PlayerInventory.onBackpackItemsChanged();
        //context.WorldPlayerContext.Player.playerUI.xui.PlayerInventory.onToolbeltItemsChanged();
        context.WorldPlayerContext.Player.playerUI.xui.RefreshAllWindows(includeViewComponents);
    }

    /// <summary>
    /// Performs a UI refresh, creating a StorageContext internally and validating components.
    /// This is a convenience method that handles context creation and validation automatically.
    /// Includes timing-based cache invalidation for rapid successive calls.
    /// </summary>
    /// <param name="methodName">The calling method name for logging purposes</param>
    /// <param name="includeViewComponents">Whether to include view components in the refresh</param>
    /// <returns>True if refresh was performed successfully, false if validation failed</returns>
    public static bool RefreshAllWindows(string methodName, bool includeViewComponents = true)
    {
        // Check if we need to invalidate cache due to rapid successive calls
        bool cacheInvalidated = CheckAndInvalidateCacheIfNeeded(methodName);

        if (!ValidationHelper.ValidateStorageContext(methodName, out StorageContext context))
        {
            return false;
        }

        if (!ValidateUIComponents(context, methodName))
        {
            return false;
        }

        RefreshAllWindowsInternal(context, includeViewComponents);

        // Update the last refresh time for this method
        UpdateLastRefreshTime(methodName);

        if (cacheInvalidated)
        {
            ModLogger.DebugLog($"{methodName}: Cache invalidated due to rapid successive UI refresh calls (< {CACHE_INVALIDATION_THRESHOLD_SECONDS}s)");
        }

        return true;
    }

    /// <summary>
    /// Checks if the previous call to RefreshAllWindows for the same methodName was within the threshold,
    /// and invalidates the context cache if so.
    /// </summary>
    /// <param name="methodName">The method name to check timing for</param>
    /// <returns>True if cache was invalidated, false otherwise</returns>
    private static bool CheckAndInvalidateCacheIfNeeded(string methodName)
    {
        lock (s_lockObject)
        {
            var isStackOp = StackOperation.IsValidOperation(methodName);
            ModLogger.DebugLog($"{methodName}: Refreshing UI for {(isStackOp ? "stack operation" : "general storage operation")}");

            if (s_lastRefreshTimes.TryGetValue(methodName, out DateTime lastRefreshTime))
            {
                var timeSinceLastRefresh = DateTime.UtcNow - lastRefreshTime;

                if (isStackOp || (timeSinceLastRefresh.TotalSeconds < CACHE_INVALIDATION_THRESHOLD_SECONDS))
                {
                    // Create a temporary StorageContext to properly invalidate caches
                    // This ensures WorldPlayerContext is always accessed through StorageContext
                    if (ValidationHelper.ValidateStorageContext(methodName, out StorageContext context))
                    {
                        // Invalidate through the StorageContext to maintain proper architecture
                        context.InvalidateCache();
                    }
                    else
                    {
                        // Fallback to direct cache invalidation if StorageContext creation fails
                        // This ensures cache invalidation still works even if context creation fails
                        ItemStackCacheManager.InvalidateGlobalCache();
                        ModLogger.DebugLog($"{methodName}: StorageContext creation failed during cache invalidation, using fallback");
                    }

                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Updates the last refresh time for the specified method name.
    /// </summary>
    /// <param name="methodName">The method name to update timing for</param>
    private static void UpdateLastRefreshTime(string methodName)
    {
        lock (s_lockObject)
        {
            s_lastRefreshTimes[methodName] = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets diagnostic information about recent refresh calls.
    /// Useful for debugging rapid successive refresh issues.
    /// </summary>
    /// <returns>String containing refresh timing information</returns>
    public static string GetRefreshTimingInfo()
    {
        lock (s_lockObject)
        {
            if (s_lastRefreshTimes.Count == 0)
            {
                return "No recent refresh calls recorded";
            }

            var now = DateTime.UtcNow;
            var timingInfo = new List<string>();

            foreach (var kvp in s_lastRefreshTimes)
            {
                var age = now - kvp.Value;
                timingInfo.Add($"{kvp.Key}: {age.TotalSeconds:F2}s ago");
            }

            return $"Recent refresh calls: {string.Join(", ", timingInfo)}";
        }
    }

    /// <summary>
    /// Clears all recorded refresh timing information.
    /// Useful for testing or when starting fresh.
    /// </summary>
    public static void ClearRefreshTimingHistory()
    {
        lock (s_lockObject)
        {
            s_lastRefreshTimes.Clear();
        }
    }
}