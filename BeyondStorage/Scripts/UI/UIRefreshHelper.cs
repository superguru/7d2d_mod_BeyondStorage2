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
    private static readonly Dictionary<string, DateTime> s_lastRefreshTimes = [];
    private static readonly object s_lockObject = new();

    public static void LogAndRefreshUI(StackOps operation, XUiC_ItemStack __instance, long callCount)
    {
        LogAndRefreshUIInternal(operation, __instance?.ItemStack, __instance?.xui?.PlayerInventory, callCount);
    }

    public static void LogAndRefreshUI(StackOps operation, ItemStack itemStack, long callCount)
    {
        LogAndRefreshUIInternal(operation, itemStack, null, callCount);
    }

    private static void LogAndRefreshUIInternal(StackOps operation, ItemStack itemStack, XUiM_PlayerInventory playerInventory, long callCount)
    {
        var methodName = StackOperation.GetStackOpName(operation);

#if DEBUG
        string callStr = " ";
        if (callCount > 0)
        {
            callStr = $"call #{callCount} ";
        }

        ModLogger.DebugLog($"{methodName}:{callStr} REFRESH_UI for {ItemX.Info(itemStack)}");
#endif

        RefreshAllWindows(methodName, isStackOperation: true, includeViewComponents: true);

        HandleCurrencyStackOp(operation, itemStack, playerInventory);
    }

    private static void HandleCurrencyStackOp(StackOps operation, ItemStack itemStack, XUiM_PlayerInventory playerInventory)
    {
        var isCurrencyStack = CurrencyCache.IsCurrencyItem(itemStack);
        if (isCurrencyStack)
        {
            if (playerInventory != null)
            {
                ActionHelper.SetTimeout(
                    () =>
                        {
                            // Refresh the wallet UI after a short delay to ensure it reflects the latest currency state
                            playerInventory.RefreshCurrency();
                        },
                    TimeSpan.FromMilliseconds(25) // 1.5 frames @ 60FPS : Short delay to allow UI to stabilize after stack operation
                );
            }
            else
            {
                // Fallback: Try to find player inventory through validation helper
                if (ValidationHelper.ValidateStorageContext(nameof(HandleCurrencyStackOp), out StorageContext context) &&
                    ValidateUIComponents(context, nameof(HandleCurrencyStackOp)))
                {
                    var fallbackPlayerInventory = context.WorldPlayerContext.Player.playerUI.xui.PlayerInventory;
                    ActionHelper.SetTimeout(
                        () =>
                            {
                                fallbackPlayerInventory.RefreshCurrency();
                            },
                        TimeSpan.FromMilliseconds(25)
                    );
                }
            }

            ModLogger.DebugLog($"Handling currency stack operation: {operation} for {ItemX.Info(itemStack)}");
        }
    }

    /// <summary>
    /// Logs a formatted debug message and triggers UI refresh for single item drop operations
    /// </summary>
    /// <param name="methodName">The calling method name</param>
    /// <param name="callCount">The call counter value</param>
    /// <param name="message">The message to log</param>
    public static void LogAndRefreshUI(string methodName)
    {
        RefreshAllWindows(methodName, isStackOperation: false, includeViewComponents: true);
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
        CheckAndInvalidateCacheIfNeeded(methodName, false);

        // Now completely safe to access without null-conditional operators
        RefreshAllWindowsInternal(context, includeViewComponents: true);
        return true;
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
    public static bool RefreshAllWindows(string methodName, bool isStackOperation, bool includeViewComponents = true)
    {
        // Check if we need to invalidate cache due to rapid successive calls
        bool cacheInvalidated = CheckAndInvalidateCacheIfNeeded(methodName, isStackOperation);

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
    /// Determines if cache invalidation is needed based on timing thresholds and operation type.
    /// This method implements a smart cache invalidation strategy to handle rapid successive UI refresh calls
    /// that can cause performance issues and visual glitches in the game UI.
    /// 
    /// Cache Invalidation Rules:
    /// - First calls: Always invalidate (no cached data available)
    /// - Stack operations: Always invalidate (immediate UI consistency required)
    /// - Storage operations: Only invalidate if called within 0.4 seconds of previous call (performance protection)
    /// </summary>
    /// <param name="methodName">The method name to check timing for - used for per-method timing tracking</param>
    /// <param name="isStackOperation">Whether this is a stack operation (always invalidates) or general storage operation (time-based)</param>
    /// <returns>True if cache was invalidated, false otherwise</returns>
    private static bool CheckAndInvalidateCacheIfNeeded(string methodName, bool isStackOperation)
    {
        // Thread safety: All cache timing operations must be synchronized to prevent race conditions
        // between multiple UI refresh calls that can happen simultaneously from different game events
        lock (s_lockObject)
        {
#if DEBUG
            // Log the refresh operation type for debugging UI performance issues
            // This helps identify whether rapid refreshes are coming from stack operations or storage operations
            ModLogger.DebugLog($"{methodName}: Refreshing UI for {(isStackOperation ? "stack operation" : "general storage operation")}");
#endif
            // Check if we have a previous refresh time recorded for this specific method
            // Each method is tracked separately because different operations have different refresh patterns
            bool isFirstCall = !s_lastRefreshTimes.TryGetValue(methodName, out DateTime lastRefreshTime);

            if (isFirstCall)
            {
#if DEBUG
                // First call from this method - ALWAYS invalidate cache because no cached data exists
                // This ensures proper initialization and prevents stale data from being displayed
                ModLogger.DebugLog($"{methodName}: First call detected, forcing cache invalidation");
#endif
                // Perform cache invalidation for first call
                PerformCacheInvalidation(methodName);
                return true;
            }

            // Calculate how much time has passed since the last refresh from this method
            var timeNow = DateTime.UtcNow;
            TimeSpan timeSinceLastRefresh = timeNow - lastRefreshTime;

            // Cache invalidation decision logic for subsequent calls:
            // 1. Stack operations ALWAYS invalidate cache because they directly modify inventory state
            //    and the UI must immediately reflect these changes to prevent visual inconsistencies
            // 2. General storage operations only invalidate if they occur within the threshold timeframe
            //    (< 0.4 seconds) to prevent performance issues from rapid successive calls
            if (isStackOperation || (timeSinceLastRefresh.TotalSeconds < CACHE_INVALIDATION_THRESHOLD_SECONDS))
            {
                // Cache invalidation is needed for rapid successive calls or stack operations
                PerformCacheInvalidation(methodName);
                return true;
            }

            // No cache invalidation needed:
            // This is a general storage operation that occurred outside the timing threshold (> 0.4 seconds)
            // The existing cache can be safely reused for performance
            return false;
        }
    }

    /// <summary>
    /// Performs the actual cache invalidation using the preferred architectural approach.
    /// Separated into its own method to avoid code duplication between first calls and subsequent calls.
    /// </summary>
    /// <param name="methodName">The method name for logging purposes</param>
    private static void PerformCacheInvalidation(string methodName)
    {
        // Attempt to create a StorageContext to properly invalidate caches through the architectural layers
        // This ensures that cache invalidation respects the proper data flow: StorageContext -> CacheManager -> DataStore
        if (ValidationHelper.ValidateStorageContext(methodName, out StorageContext context))
        {
            // Primary cache invalidation path: Use StorageContext to maintain proper architecture
            // This invalidates both the ItemStackCacheManager and any underlying data store caches
            // The StorageContext ensures that WorldPlayerContext is properly accessed and cache timing is coordinated
            context.InvalidateCache();
        }
        else
        {
            // Fallback cache invalidation path: Direct global cache invalidation
            // This is used when StorageContext creation fails (e.g., player not in world, UI not initialized)
            // While not ideal architecturally, it ensures cache invalidation still works in edge cases
            // The global invalidation affects all cache instances system-wide
            ItemStackCacheManager.InvalidateGlobalCache();
            ModLogger.DebugLog($"{methodName}: StorageContext creation failed during cache invalidation, using fallback");
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