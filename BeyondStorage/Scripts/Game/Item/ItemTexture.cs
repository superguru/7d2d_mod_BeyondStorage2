using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using UnityEngine;

namespace BeyondStorage.Scripts.Game.Item;

/// <summary>
/// Provides static methods for managing paint/texture block operations with storage integration.
/// Handles ammo checking, counting, removal, and orchestrates multi-phase paint operations.
/// </summary>
public static class ItemTexture
{
    #region Private Fields

    // Simple tracking if needed
    private static readonly Dictionary<int, int> s_paintRemovals = [];

    // Paint counting system
    private static readonly Dictionary<Guid, PaintOperationContext> s_activeOperations = [];

    #endregion

    #region Ammo Management Methods

    /// <summary>
    /// Checks if sufficient ammo is available for painting operations.
    /// Considers both entity-held ammo and storage ammo when determining availability.
    /// However, the entityAvailableCount should be calculated by the caller and passed in.
    /// </summary>
    /// <param name="entityAvailableCount">Amount of ammo currently held by the entity</param>
    /// <param name="_actionData">Action data for the texture operation</param>
    /// <param name="ammoType">Type of ammo/paint being checked</param>
    /// <returns>True if sufficient ammo is available, false otherwise</returns>
    public static bool ItemTexture_checkAmmo(int entityAvailableCount, ItemActionData _actionData, ItemValue ammoType)
    {
        const string d_MethodName = nameof(ItemTexture_checkAmmo);
        const bool DEFAULT_RETURN_VALUE = false;

        if (!ValidationHelper.ValidateStorageContextWithFeature(d_MethodName, config => config.EnableForBlockTexture, out StorageContext context))
        {
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateItemValue(ammoType, d_MethodName, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        // Paint cost is 1 for everything in v2.x
        if (entityAvailableCount > 0)
        {
            return true;
        }

        var hasAmmo = context.HasItem(ammoType);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: hasAmmo is {hasAmmo} for itemValue {itemName}");
#endif
        return hasAmmo;
    }

    /// <summary>
    /// Gets the total count of ammo available from both entity inventory and storage.
    /// Combines entity-held ammo with storage ammo to provide total available count.
    /// </summary>
    /// <param name="ammoType">Type of ammo/paint to count</param>
    /// <param name="entityAvailableCount">Amount of ammo currently held by the entity</param>
    /// <returns>Total count of available ammo from all sources</returns>
    public static int ItemTexture_GetAmmoCount(ItemValue ammoType, int entityAvailableCount)
    {
        const string d_MethodName = nameof(ItemTexture_GetAmmoCount);

        if (entityAvailableCount < 0)
        {
            entityAvailableCount = 0;
        }

        int DEFAULT_RETURN_VALUE = entityAvailableCount;

        if (!ValidationHelper.ValidateItemAndContext(ammoType, d_MethodName, config => config.EnableForBlockTexture,
            out StorageContext context, out _, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var storageCount = context.GetItemCount(ammoType);
        var totalAvailableCount = storageCount + entityAvailableCount;

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: {itemName} has storageCount {storageCount}, entityAvailableCount {entityAvailableCount}, total {totalAvailableCount}");
#endif
        return totalAvailableCount;
    }

    /// <summary>
    /// Removes ammo from storage for painting operations.
    /// Attempts to remove the specified amount of paint from storage and returns actual amount removed.
    /// The player 's entity-held ammo is not modified by this method. As it is expected that the caller will handle entity ammo separately.
    /// </summary>
    /// <param name="itemValue">Type of ammo/paint to remove</param>
    /// <param name="paintCost">Amount of paint needed</param>
    /// <param name="_ignoreModdedItems">Whether to ignore modded items during removal</param>
    /// <param name="_removedItems">Optional list to store removed item stacks</param>
    /// <returns>Actual amount of ammo removed from storage</returns>
    public static int ItemTexture_RemoveAmmo(ItemValue itemValue, int paintCost, bool _ignoreModdedItems = false, IList<ItemStack> _removedItems = null)
    {
        const string d_MethodName = nameof(ItemTexture_RemoveAmmo);
        const int DEFAULT_RETURN_VALUE = 0;

        // Early exit conditions
        if (paintCost <= 0)
        {
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, config => config.EnableForBlockTexture,
            out StorageContext context, out _, out string itemName))
        {
            return paintCost;
        }

        var removedFromStorage = context.RemoveRemaining(itemValue, paintCost, _ignoreModdedItems, _removedItems);
        var stillNeeded = paintCost - removedFromStorage;

        // Invalidate paint caches if needed
        if (removedFromStorage > 0)
        {
            s_paintRemovals.TryGetValue(itemValue.type, out var current);
            s_paintRemovals[itemValue.type] = current + removedFromStorage;
        }
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: itemValue {itemName}, paintCost {paintCost}, removedFromStorage {removedFromStorage}, stillNeeded {stillNeeded}");
#endif
        return removedFromStorage;
    }

    #endregion

    #region Paint Operation Lifecycle Methods

    /// <summary>
    /// Registers an existing paint operation paintContext for tracking.
    /// Used when the paintContext is created externally (e.g., in SmartFireShotLater) and needs to be tracked.
    /// </summary>
    /// <param name="paintContext">The paint operation paintContext to register</param>
    /// <returns>The operation ID for tracking this paint operation</returns>
    public static Guid RegisterPaintOperation(PaintOperationContext paintContext)
    {
        const string d_MethodName = nameof(RegisterPaintOperation);

        s_activeOperations[paintContext.OperationId] = paintContext;

        ModLogger.DebugLog($"{d_MethodName}: Registered paint operation {paintContext.OperationId}");
        return paintContext.OperationId;
    }

    /// <summary>
    /// Counts paint usage during the counting phase of a paint operation.
    /// Increments the total paint required if consumption should not be skipped (e.g., not in creative mode).
    /// </summary>
    /// <param name="operationId">The operation ID to count paint usage for</param>
    /// <returns>True if the operation is in counting phase, false otherwise</returns>
    public static bool CountPaintUsage(Guid operationId)
    {
#if DEBUG
        const string d_MethodName = nameof(CountPaintUsage);
        //TODO: Count paint usage
        ModLogger.DebugLog($"{d_MethodName}: Counting paint usage for operation {operationId}");
#endif
        if (s_activeOperations.TryGetValue(operationId, out var paintContext) && paintContext.IsCountingPhase)
        {
            paintContext.TotalPaintRequired++;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Paint required incremented to {paintContext.TotalPaintRequired} for operation {operationId}");
#endif
            return true; // Always return true during counting
        }

        return false;
    }

    /// <summary>
    /// Switches a paint operation from counting phase to execution phase.
    /// Calculates available resources, removes paint from storage, and prepares for execution.
    /// </summary>
    /// <param name="operationId">The operation ID to switch to execution phase</param>
    /// <returns>True if the operation can proceed with execution, false if insufficient resources</returns>
    public static bool SwitchToExecutionPhase(Guid operationId)
    {
        const string d_MethodName = nameof(SwitchToExecutionPhase);

        if (s_activeOperations.TryGetValue(operationId, out var paintContext))
        {
            paintContext.IsCountingPhase = false;

            // Calculate and set total paint count available using the paintContext method
            paintContext.CalculateAndSetPaintCountAvailable();

            // Calculate how much paint to actually remove and how many faces to paint
            paintContext.PaintToRemove = Math.Min(paintContext.TotalPaintRequired, paintContext.PaintCountAvailable);
            paintContext.FacesToPaint = paintContext.PaintToRemove; // 1:1 ratio since each face costs 1 paint

            ModLogger.DebugLog($"{d_MethodName}: Operation {operationId} - Required: {paintContext.TotalPaintRequired}, Available: {paintContext.PaintCountAvailable}, Will remove: {paintContext.PaintToRemove}, Will paint: {paintContext.FacesToPaint} faces");

            // Check if paint consumption should be skipped (creative mode, infinite ammo, etc.)
            if (paintContext.ShouldSkipPaintConsumption())
            {
                // Skip paint removal but allow full painting
                paintContext.FacesToPaint = paintContext.TotalPaintRequired; // Paint all faces without consuming resources
                ModLogger.DebugLog($"{d_MethodName}: Paint consumption skipped for operation {operationId}, will paint all {paintContext.FacesToPaint} faces");
                return paintContext.TotalPaintRequired > 0; // Return true if there are faces to paint
            }

            if (paintContext.PaintToRemove > 0)
            {
                // Remove the calculated amount of paint
                var actuallyRemoved = ItemTexture_RemoveAmmo(paintContext.AmmoType, paintContext.PaintToRemove);
                paintContext.PaintToRemove = actuallyRemoved; // Update with what was actually removed
                paintContext.FacesToPaint = actuallyRemoved; // Update faces to paint accordingly

                return actuallyRemoved > 0;
            }
            else
            {
                ModLogger.DebugLog($"{d_MethodName}: No paint available for operation {operationId}");
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a face should be painted during the execution phase.
    /// This is a convenience wrapper around the PaintOperationContext.ShouldPaintFace() method.
    /// </summary>
    /// <param name="operationId">The operation ID to check</param>
    /// <returns>True if this face should be painted, false if no more paint is available</returns>
    public static bool ShouldPaintFace(Guid operationId)
    {
        if (s_activeOperations.TryGetValue(operationId, out var context))
        {
            return context.ShouldPaintFace();
        }
        return false;
    }

    /// <summary>
    /// Finishes a paint operation and cleans up its paintContext.
    /// Removes the operation from the active operations dictionary and performs cleanup.
    /// </summary>
    /// <param name="operationId">The operation ID to finish</param>
    public static void FinishPaintOperation(Guid operationId)
    {
        const string d_MethodName = nameof(FinishPaintOperation);

        if (s_activeOperations.Remove(operationId))
        {
            ModLogger.DebugLog($"{d_MethodName}: Finished paint operation {operationId}");
        }
    }

    #endregion

    #region Operation Query Methods

    /// <summary>
    /// Checks if a paint operation is currently in the counting phase.
    /// Used to determine the current state of an operation for phase-specific logic.
    /// </summary>
    /// <param name="operationId">The operation ID to check</param>
    /// <returns>True if the operation exists and is in counting phase, false otherwise</returns>
    public static bool IsCountingPhase(Guid operationId)
    {
        return s_activeOperations.TryGetValue(operationId, out var context) && context.IsCountingPhase;
    }

    /// <summary>
    /// Gets the operation paintContext for debugging and advanced operations.
    /// Provides access to the full paintContext object for detailed inspection or manipulation.
    /// </summary>
    /// <param name="operationId">The operation ID to get paintContext for</param>
    /// <returns>The operation paintContext if found, null otherwise</returns>
    public static PaintOperationContext GetOperationContext(Guid operationId)
    {
        s_activeOperations.TryGetValue(operationId, out var context);
        return context;
    }

    #endregion

    #region High-Level Paint Operations

    /// <summary>
    /// Performs a smart flood fill paint operation with resource management using an existing paint paintContext.
    /// Executes a three-phase operation: counting, resource calculation, and execution.
    /// Handles resource validation and ensures paint is only consumed as needed.
    /// </summary>
    /// <param name="paintContext">The paint operation paintContext containing all necessary data</param>
    /// <param name="_world">The world instance</param>
    /// <param name="_cc">The chunk cluster</param>
    /// <param name="_entityId">The entity performing the operation</param>
    /// <param name="_lpRelative">Player data relative to the operation</param>
    /// <param name="_sourcePaint">Source paint index for flood fill matching</param>
    /// <param name="_hitPosition">Position where the flood fill starts</param>
    /// <param name="_hitFaceNormal">Normal of the face that was hit</param>
    /// <param name="_dir1">First direction vector for flood fill</param>
    /// <param name="_dir2">Second direction vector for flood fill</param>
    /// <param name="_channel">Paint channel to operate on</param>
    public static void SmartFloodFill(PaintOperationContext paintContext, World _world, ChunkCluster _cc, int _entityId, PersistentPlayerData _lpRelative, int _sourcePaint, Vector3 _hitPosition, Vector3 _hitFaceNormal, Vector3 _dir1, Vector3 _dir2, int _channel)
    {
        const string d_MethodName = nameof(SmartFloodFill);
        ModLogger.DebugLog($"{d_MethodName}: Starting smart flood fill with paintContext {paintContext.OperationId}");

        // Register the paint paintContext for tracking
        var operationId = RegisterPaintOperation(paintContext);

        try
        {
            // Phase 1: Count paint usage and store faces to paint
            ModLogger.DebugLog($"{d_MethodName}: Phase 1 - Counting paint usage and storing faces");
            paintContext.ExposedTextureBlock.CountFloodFill(_world, _cc, _entityId, paintContext.ActionData, _lpRelative, _sourcePaint, _hitPosition, _hitFaceNormal, _dir1, _dir2, _channel, operationId);

            // Phase 2: Calculate resources and switch to execution
            var canProceed = SwitchToExecutionPhase(operationId);
            if (!canProceed)
            {
                ModLogger.DebugLog($"{d_MethodName}: No paint available, aborting operation");
                return;
            }

            var context = GetOperationContext(operationId);
            ModLogger.DebugLog($"{d_MethodName}: Will paint {context?.FacesToPaint} out of {context?.TotalPaintRequired} required faces");

            // Phase 3: Execute painting using stored faces (no flood fill loop needed!)
            ModLogger.DebugLog($"{d_MethodName}: Phase 3 - Executing painting from stored faces");
            paintContext.ExposedTextureBlock.ExecuteFloodFill(_world, _cc, _entityId, paintContext.ActionData, _lpRelative, _sourcePaint, _hitPosition, _hitFaceNormal, _dir1, _dir2, _channel, operationId);
        }
        finally
        {
            // Cleanup both operation paintContext and stored faces
            paintContext.ExposedTextureBlock.CleanupOperation(operationId);
            FinishPaintOperation(operationId);
        }
    }

    /// <summary>
    /// Performs a smart area paint operation with resource management using an existing paint paintContext.
    /// Executes a three-phase operation: counting, resource calculation, and execution.
    /// Handles resource validation and ensures paint is only consumed as needed.
    /// </summary>
    /// <param name="paintContext">The paint operation paintContext containing all necessary data</param>
    /// <param name="_world">The world instance</param>
    /// <param name="_cc">The chunk cluster</param>
    /// <param name="_entityId">The entity performing the operation</param>
    /// <param name="_lpRelative">Player data relative to the operation</param>
    /// <param name="_pos">Target position for area painting</param>
    /// <param name="_origin">Origin point for the area paint operation</param>
    /// <param name="_dir1">First direction vector for area calculation</param>
    /// <param name="_dir2">Second direction vector for area calculation</param>
    /// <param name="_radius">Radius of the area to paint</param>
    /// <param name="_mode">Paint mode description for logging</param>
    public static void SmartAreaPaint(PaintOperationContext paintContext, World _world, ChunkCluster _cc, int _entityId, PersistentPlayerData _lpRelative, Vector3 _pos, Vector3 _origin, Vector3 _dir1, Vector3 _dir2, float _radius, string _mode)
    {
        const string d_MethodName = nameof(SmartAreaPaint);
        ModLogger.DebugLog($"{d_MethodName}: Starting smart {_mode} paint with radius {_radius} using paintContext {paintContext.OperationId}");

        if (paintContext.ExposedTextureBlock == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: ExposedTextureBlock is null, cannot perform area paint");
            return;
        }

        // Register the paint paintContext for tracking
        var operationId = RegisterPaintOperation(paintContext);

        try
        {
            // Phase 1: Count paint usage and store faces to paint
            ModLogger.DebugLog($"{d_MethodName}: Phase 1 - Counting paint usage for {_mode} mode");
            paintContext.ExposedTextureBlock.CountAreaPaint(_world, _cc, _entityId, paintContext.ActionData, _lpRelative, _pos, _origin, _dir1, _dir2, _radius, operationId);

            // Phase 2: Calculate resources and switch to execution
            var canProceed = SwitchToExecutionPhase(operationId);
            if (!canProceed)
            {
                ModLogger.DebugLog($"{d_MethodName}: No paint available, aborting operation");
                return;
            }

            var context = GetOperationContext(operationId);
            ModLogger.DebugLog($"{d_MethodName}: Will paint {context?.FacesToPaint} out of {context?.TotalPaintRequired} required faces");

            // Phase 3: Execute painting using stored faces
            ModLogger.DebugLog($"{d_MethodName}: Phase 3 - Executing {_mode} painting from stored faces");
            paintContext.ExposedTextureBlock.ExecuteAreaPaint(_entityId, paintContext.ActionData, operationId);
        }
        finally
        {
            // Cleanup both operation paintContext and stored faces
            paintContext.ExposedTextureBlock.CleanupOperation(operationId);
            FinishPaintOperation(operationId);
        }
    }
    #endregion
}