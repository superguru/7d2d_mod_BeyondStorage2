using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using UnityEngine;
using static ItemActionTextureBlock;

namespace BeyondStorage.Scripts.Game.Item;

public class ItemTexture
{
    // Simple tracking if needed
    private static readonly Dictionary<int, int> s_paintRemovals = [];

    // Paint counting system
    private static readonly Dictionary<Guid, PaintOperationContext> s_activeOperations = [];

    public class PaintOperationContext(ItemActionTextureBlock.ItemActionTextureBlockData actionData, ItemValue ammoType)
    {
        public Guid OperationId { get; set; } = Guid.NewGuid();
        public int TotalPaintRequired { get; set; } = 0;
        public int PaintAvailable { get; set; } = 0;
        public int PaintToRemove { get; set; } = 0;
        public int FacesToPaint { get; set; } = 0;
        public bool IsCountingPhase { get; set; } = true;
        public ItemActionTextureBlockData ActionData { get; set; } = actionData;
        public ItemValue AmmoType { get; set; } = ammoType;
    }

    public static bool ItemTexture_checkAmmo(int entityAvailableCount, ItemActionData _actionData, ItemValue ammoType)
    {
        const string d_MethodName = nameof(ItemTexture_checkAmmo);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Starting");
#endif
        // Paint cost is 1 for everything in v2.x
        if (entityAvailableCount > 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: Entity has available count {entityAvailableCount}, no need to check ammo");
            return true;
        }

        if (!ModConfig.EnableForBlockTexture())
        {
            return false;
        }

        // Use StorageContext directly for efficient checking
        var storageContext = StorageContextFactory.Create(d_MethodName);
        var hasAmmo = storageContext?.HasItem(ammoType) ?? false;

        ModLogger.DebugLog($"{d_MethodName}: StorageContext is {storageContext != null}, hasAmmo is {hasAmmo} for ammoType {ammoType?.ItemClass?.Name}");
        return hasAmmo;
    }

    public static int ItemTexture_GetAmmoCount(ItemValue ammoType, int entityAvailableCount)
    {
        const string d_MethodName = nameof(ItemTexture_GetAmmoCount);

        if (entityAvailableCount < 0)
        {
            entityAvailableCount = 0;
        }

        // Check if feature is enabled
        if (!ModConfig.EnableForBlockTexture())
        {
            return entityAvailableCount;
        }

        // Use StorageContext directly for efficient item counting
        var storageContext = StorageContextFactory.Create(d_MethodName);
        var storageCount = storageContext?.GetItemCount(ammoType) ?? 0;
        var totalAvailableCount = storageCount + entityAvailableCount;

        ModLogger.DebugLog($"{d_MethodName}: StorageContext is {storageContext != null}, storageCount {storageCount}, entityAvailableCount {entityAvailableCount}, total {totalAvailableCount}");
        return totalAvailableCount;
    }

    public static int ItemTexture_RemoveAmmo(ItemValue ammoType, int paintCost, bool _ignoreModdedItems = false, IList<ItemStack> _removedItems = null)
    {
        const string d_MethodName = nameof(ItemTexture_RemoveAmmo);

        // Early exit conditions
        if (paintCost <= 0)
        {
            return paintCost;
        }

        if (!ModConfig.EnableForBlockTexture())
        {
            return paintCost;
        }

        // Use StorageContext directly for efficient removal operations
        var storageContext = StorageContextFactory.Create(d_MethodName);
        var removedFromStorage = storageContext?.RemoveRemaining(ammoType, paintCost, _ignoreModdedItems, _removedItems) ?? 0;
        var stillNeeded = paintCost - removedFromStorage;

        // Invalidate paint caches if needed
        if (removedFromStorage > 0)
        {
            s_paintRemovals.TryGetValue(ammoType.type, out var current);
            s_paintRemovals[ammoType.type] = current + removedFromStorage;
        }

        ModLogger.DebugLog($"{d_MethodName}: StorageContext is {storageContext != null}, ammoType {ammoType?.ItemClass?.Name}, paintCost {paintCost}, removedFromStorage {removedFromStorage}, stillNeeded {stillNeeded}");
        return removedFromStorage;
    }

    // New method to start a paint operation
    public static Guid StartPaintOperation(ItemActionTextureBlockData actionData, ItemValue ammoType)
    {
        const string d_MethodName = nameof(StartPaintOperation);

        var context = new PaintOperationContext(actionData, ammoType);
        s_activeOperations[context.OperationId] = context;

        ModLogger.DebugLog($"{d_MethodName}: Started paint operation {context.OperationId}");
        return context.OperationId;
    }

    // Method to count paint usage (called during counting phase)
    public static bool CountPaintUsage(Guid operationId)
    {
        const string d_MethodName = nameof(CountPaintUsage);

        if (s_activeOperations.TryGetValue(operationId, out var context) && context.IsCountingPhase)
        {
            context.TotalPaintRequired++;
            ModLogger.DebugLog($"{d_MethodName}: Paint required incremented to {context.TotalPaintRequired} for operation {operationId}");
            return true; // Always return true during counting
        }

        return false;
    }

    // Method to switch to execution phase with proper resource calculation
    public static bool SwitchToExecutionPhase(Guid operationId)
    {
        const string d_MethodName = nameof(SwitchToExecutionPhase);

        if (s_activeOperations.TryGetValue(operationId, out var context))
        {
            context.IsCountingPhase = false;

            // Get total paint available
            context.PaintAvailable = ItemTexture_GetAmmoCount(context.AmmoType, 0);

            // Calculate how much paint to actually remove and how many faces to paint
            context.PaintToRemove = Math.Min(context.TotalPaintRequired, context.PaintAvailable);
            context.FacesToPaint = context.PaintToRemove; // 1:1 ratio since each face costs 1 paint

            ModLogger.DebugLog($"{d_MethodName}: Operation {operationId} - Required: {context.TotalPaintRequired}, Available: {context.PaintAvailable}, Will remove: {context.PaintToRemove}, Will paint: {context.FacesToPaint} faces");

            if (context.PaintToRemove > 0)
            {
                // Remove the calculated amount of paint
                var actuallyRemoved = ItemTexture_RemoveAmmo(context.AmmoType, context.PaintToRemove);
                context.PaintToRemove = actuallyRemoved; // Update with what was actually removed
                context.FacesToPaint = actuallyRemoved; // Update faces to paint accordingly

                ModLogger.DebugLog($"{d_MethodName}: Actually removed {actuallyRemoved} paint for operation {operationId}");
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

    // Method to check if we should paint this face during execution
    public static bool ShouldPaintFace(Guid operationId)
    {
        if (s_activeOperations.TryGetValue(operationId, out var context) && !context.IsCountingPhase)
        {
            if (context.FacesToPaint > 0)
            {
                context.FacesToPaint--;
                return true;
            }
        }
        return false;
    }

    // Method to finish paint operation
    public static void FinishPaintOperation(Guid operationId)
    {
        const string d_MethodName = nameof(FinishPaintOperation);

        if (s_activeOperations.Remove(operationId))
        {
            ModLogger.DebugLog($"{d_MethodName}: Finished paint operation {operationId}");
        }
    }

    // Check if operation is in counting phase
    public static bool IsCountingPhase(Guid operationId)
    {
        return s_activeOperations.TryGetValue(operationId, out var context) && context.IsCountingPhase;
    }

    // Get operation context for debugging
    public static PaintOperationContext GetOperationContext(Guid operationId)
    {
        s_activeOperations.TryGetValue(operationId, out var context);
        return context;
    }

    public static void SmartFloodFill(ItemActionTextureBlock tb, World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, int _sourcePaint, Vector3 _hitPosition, Vector3 _hitFaceNormal, Vector3 _dir1, Vector3 _dir2, int _channel)
    {
        const string d_MethodName = nameof(SmartFloodFill);
        ModLogger.DebugLog($"{d_MethodName}: Starting smart flood fill");

        var exposed = tb as ItemActionTextureBlockExposed ?? new ItemActionTextureBlockExposed();
        var ammoType = tb.currentMagazineItem;

        // Start paint operation
        var operationId = StartPaintOperation(_actionData, ammoType);

        try
        {
            // Phase 1: Count paint usage and store faces to paint
            ModLogger.DebugLog($"{d_MethodName}: Phase 1 - Counting paint usage and storing faces");
            exposed.CountFloodFill(_world, _cc, _entityId, _actionData, _lpRelative, _sourcePaint, _hitPosition, _hitFaceNormal, _dir1, _dir2, _channel, operationId);

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
            exposed.ExecuteFloodFill(_world, _cc, _entityId, _actionData, _lpRelative, _sourcePaint, _hitPosition, _hitFaceNormal, _dir1, _dir2, _channel, operationId);
        }
        finally
        {
            // Cleanup both operation context and stored faces
            exposed.CleanupOperation(operationId);
            FinishPaintOperation(operationId);
        }
    }

    public static void SmartAreaPaint(ItemActionTextureBlock tb, World _world, ChunkCluster _cc, int _entityId, ItemActionTextureBlockData _actionData, PersistentPlayerData _lpRelative, Vector3 _pos, Vector3 _origin, Vector3 _dir1, Vector3 _dir2, float _radius, string _mode)
    {
        const string d_MethodName = nameof(SmartAreaPaint);
        ModLogger.DebugLog($"{d_MethodName}: Starting smart {_mode} paint with radius {_radius}");

        var exposed = tb as ItemActionTextureBlockExposed ?? new ItemActionTextureBlockExposed();
        var ammoType = tb.currentMagazineItem;

        // Start paint operation
        var operationId = StartPaintOperation(_actionData, ammoType);

        try
        {
            // Phase 1: Count paint usage and store faces to paint
            ModLogger.DebugLog($"{d_MethodName}: Phase 1 - Counting paint usage for {_mode} mode");
            exposed.CountAreaPaint(_world, _cc, _entityId, _actionData, _lpRelative, _pos, _origin, _dir1, _dir2, _radius, operationId);

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
            exposed.ExecuteAreaPaint(_entityId, _actionData, operationId);
        }
        finally
        {
            // Cleanup both operation context and stored faces
            exposed.CleanupOperation(operationId);
            FinishPaintOperation(operationId);
        }
    }
}