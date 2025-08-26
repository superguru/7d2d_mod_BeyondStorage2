using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.UI;

/// <summary>
/// Utility class for refreshing UI components when storage changes affect game state.
/// Provides common functionality for validating UI contexts and refreshing all windows.
/// 
/// Key Features:
/// - Thread-safe timing-based cache invalidation strategy
/// - Intelligent UI refresh timing to prevent performance issues
/// - Stack operation logging and UI refresh coordination
/// - Currency operation handling with delayed wallet updates
/// - Comprehensive UI component validation
/// 
/// Cache Invalidation Rules:
/// - First calls: Always invalidate (no cached data available)
/// - Stack operations: Always invalidate (immediate UI consistency required)
/// - Storage operations: Only invalidate if called within 0.4 seconds of previous call (performance protection)
/// </summary>
public static class UIRefreshHelper
{
    #region Constants

    /// <summary>
    /// Time threshold in seconds for determining when to invalidate cache for rapid successive calls.
    /// Operations called within this timeframe will trigger cache invalidation to prevent stale data.
    /// </summary>
    private const double CACHE_INVALIDATION_THRESHOLD_SECONDS = 0.4;

    #endregion

    #region Private Fields

    /// <summary>
    /// Thread-safe dictionary tracking the last refresh time for each method to implement timing-based cache invalidation.
    /// </summary>
    private static readonly Dictionary<string, DateTime> s_lastRefreshTimes = [];

    /// <summary>
    /// Lock object for thread-safe access to timing data and cache operations.
    /// </summary>
    private static readonly object s_lockObject = new();

    #endregion

    #region Public API - Stack Operation Logging and Refresh

    /// <summary>
    /// Logs stack operation details and triggers UI refresh for operations involving ItemStack instances.
    /// Handles currency-specific operations with special wallet refresh logic.
    /// </summary>
    /// <param name="operation">The type of stack operation being performed</param>
    /// <param name="instance">The ItemStack UI component instance (may be null)</param>
    /// <param name="callCount">The sequential call number for logging purposes</param>
    public static void LogAndRefreshUI(StackOps operation, XUiC_ItemStack instance, long callCount)
    {
        LogAndRefreshUIInternal(operation, instance?.ItemStack, instance?.xui?.PlayerInventory, callCount);
    }

    /// <summary>
    /// Logs stack operation details and triggers UI refresh for operations involving ItemStack data.
    /// Uses fallback currency handling when player inventory is not directly available.
    /// </summary>
    /// <param name="operation">The type of stack operation being performed</param>
    /// <param name="itemStack">The ItemStack data being operated on</param>
    /// <param name="callCount">The sequential call number for logging purposes</param>
    public static void LogAndRefreshUI(StackOps operation, ItemStack itemStack, long callCount)
    {
        LogAndRefreshUIInternal(operation, itemStack, null, callCount);
    }

    /// <summary>
    /// Logs a debug message and triggers UI refresh for general operations without stack-specific context.
    /// Treats the operation as a general storage operation rather than a stack operation.
    /// </summary>
    /// <param name="methodName">The calling method name for logging and timing purposes</param>
    public static void LogAndRefreshUI(string methodName)
    {
        RefreshAllWindows(methodName, isStackOperation: false, includeViewComponents: true);
    }

    #endregion

    #region Public API - UI Validation and Refresh

    /// <summary>
    /// Validates UI components are available and refreshes all windows if valid.
    /// This is commonly needed when storage operations affect the game state and UI needs to be updated.
    /// Includes timing-based cache invalidation for performance optimization.
    /// </summary>
    /// <param name="context">The storage context containing world and player information</param>
    /// <param name="methodName">The calling method name for logging and timing purposes</param>
    /// <returns>True if UI components were valid and refresh was performed, false otherwise</returns>
    public static bool ValidateAndRefreshUI(StorageContext context, string methodName)
    {
        if (!ValidateUIComponents(context, methodName))
        {
            return false;
        }

        // Check if we need to invalidate cache due to rapid successive calls
        CheckAndInvalidateCacheIfNeeded(methodName, false);

        RefreshAllWindowsInternal(context, includeViewComponents: true);

        // Update the last refresh time for this method to maintain consistency with RefreshAllWindows
        UpdateLastRefreshTime(methodName);

        return true;
    }

    /// <summary>
    /// Performs a UI refresh, creating a StorageContext internally and validating components.
    /// This is a convenience method that handles context creation and validation automatically.
    /// Includes timing-based cache invalidation for rapid successive calls.
    /// </summary>
    /// <param name="methodName">The calling method name for logging and timing purposes</param>
    /// <param name="isStackOperation">Whether this is a stack operation (always invalidates cache) or general storage operation (time-based invalidation)</param>
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

#if DEBUG
        //if (cacheInvalidated)
        //{
        //    ModLogger.DebugLog($"{methodName}: Cache invalidated due to rapid successive UI refresh calls (< {CACHE_INVALIDATION_THRESHOLD_SECONDS}s)");
        //}
#endif

        return true;
    }

    #endregion

    #region Public API - UI Component Validation

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

    #endregion

    #region Public API - Diagnostics

    /// <summary>
    /// Gets diagnostic information about recent refresh calls.
    /// Useful for debugging rapid successive refresh issues and timing analysis.
    /// </summary>
    /// <returns>String containing refresh timing information for all tracked methods</returns>
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

    #endregion

    #region Private Implementation - Stack Operation Processing

    /// <summary>
    /// Internal implementation for stack operation logging and UI refresh.
    /// Handles both direct player inventory access and fallback methods for currency operations.
    /// </summary>
    /// <param name="operation">The type of stack operation being performed</param>
    /// <param name="itemStack">The ItemStack data being operated on</param>
    /// <param name="playerInventory">Player inventory instance for direct currency refresh (may be null)</param>
    /// <param name="callCount">The sequential call number for logging purposes</param>
    private static void LogAndRefreshUIInternal(StackOps operation, ItemStack itemStack, XUiM_PlayerInventory playerInventory, long callCount)
    {
        var methodName = StackOperation.GetStackOpName(operation);

#if DEBUG
        //string callStr = " ";
        //if (callCount > 0)
        //{
        //    callStr = $"call #{callCount} ";
        //}

        //ModLogger.DebugLog($"{methodName}:{callStr} REFRESH_UI for {ItemX.Info(itemStack)}");
#endif

        RefreshAllWindows(methodName, isStackOperation: true, includeViewComponents: true);

        HandleCurrencyStackOp(operation, itemStack, playerInventory);
    }

    /// <summary>
    /// Handles special processing for currency stack operations with delayed wallet UI updates.
    /// Implements a 25ms delay to ensure UI stability after currency changes.
    /// Uses fallback method when direct player inventory access is not available.
    /// </summary>
    /// <param name="operation">The stack operation that was performed</param>
    /// <param name="itemStack">The ItemStack that was operated on</param>
    /// <param name="playerInventory">Player inventory for direct access (may be null)</param>
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
                if (ValidationHelper.ValidateStorageContext(StackOperation.GetStackOpName(operation), out StorageContext context) &&
                    ValidateUIComponents(context, StackOperation.GetStackOpName(operation)))
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

#if DEBUG
            //ModLogger.DebugLog($"Handling currency stack operation: {operation} for {ItemX.Info(itemStack)}");
#endif
        }
    }

    #endregion

    #region Private Implementation - UI Refresh

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

    #endregion

    #region Private Implementation - Cache Management

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
            //ModLogger.DebugLog($"{methodName}: Refreshing UI for {(isStackOperation ? "stack operation" : "general storage operation")}");
#endif
            // Check if we have a previous refresh time recorded for this specific method
            // Each method is tracked separately because different operations have different refresh patterns
            bool isFirstCall = !s_lastRefreshTimes.TryGetValue(methodName, out DateTime lastRefreshTime);

            if (isFirstCall)
            {
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
    /// Uses StorageContext when available for proper architectural layering, falls back to global invalidation.
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

    #endregion

    #region Private Implementation - Timing Management

    /// <summary>
    /// Updates the last refresh time for the specified method name.
    /// Used to maintain timing information for cache invalidation decisions.
    /// Thread-safe implementation using the shared lock object.
    /// </summary>
    /// <param name="methodName">The method name to update timing for</param>
    private static void UpdateLastRefreshTime(string methodName)
    {
        lock (s_lockObject)
        {
            s_lastRefreshTimes[methodName] = DateTime.UtcNow;
        }
    }

    #endregion
}