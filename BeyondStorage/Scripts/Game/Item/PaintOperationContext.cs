using System;
using BeyondStorage.Scripts.Infrastructure;
using static ItemActionTextureBlock;

namespace BeyondStorage.Scripts.Game.Item;

/// <summary>
/// Context class that tracks the state of a paint operation including resource counting and execution phases.
/// Used to manage paint consumption across multi-phase paint operations like flood fill and area painting.
/// </summary>
public class PaintOperationContext
{
    /// <summary>
    /// Unique identifier for this paint operation
    /// </summary>
    public Guid OperationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Total amount of paint required for this operation (calculated during counting phase)
    /// </summary>
    public int TotalPaintRequired { get; set; } = 0;

    /// <summary>
    /// Total amount of paint available from all sources
    /// </summary>
    public int PaintCountAvailable { get; set; } = 0;

    /// <summary>
    /// Amount of paint that will actually be removed from storage
    /// </summary>
    public int PaintToRemove { get; set; } = 0;

    /// <summary>
    /// Number of faces that will actually be painted (limited by available paint)
    /// </summary>
    public int FacesToPaint { get; set; } = 0;

    /// <summary>
    /// Whether this operation is currently in the counting phase (true) or execution phase (false)
    /// </summary>
    public bool IsCountingPhase { get; set; } = true;

    /// <summary>
    /// The action data associated with this paint operation
    /// </summary>
    public ItemActionTextureBlockData ActionData { get; set; }

    /// <summary>
    /// The type of ammo/paint being used for this operation
    /// </summary>
    public ItemValue AmmoType { get; set; }

    /// <summary>
    /// The original texture block instance performing the paint operation
    /// </summary>
    public ItemActionTextureBlock TextureBlock { get; }

    /// <summary>
    /// The exposed texture block wrapper that provides access to enhanced painting methods.
    /// This wraps the original TextureBlock instance to preserve all game state.
    /// </summary>
    public ItemActionTextureBlockExposed ExposedTextureBlock { get; }

    /// <summary>
    /// Creates a new paint operation context with the specified parameters
    /// </summary>
    /// <param name="tb">The texture block instance</param>
    /// <param name="actionData">The action data for this operation</param>
    /// <param name="ammoType">The type of paint/ammo being used</param>
    public PaintOperationContext(ItemActionTextureBlock tb, ItemActionTextureBlockData actionData, ItemValue ammoType)
    {
        TextureBlock = tb;
        ActionData = actionData;
        AmmoType = ammoType;

        // Create exposed wrapper around the original texture block to preserve all game state
        ExposedTextureBlock = new ItemActionTextureBlockExposed(tb);
        ModLogger.DebugLog($"{nameof(PaintOperationContext)}: Intercepting fireShotLater. InfiniteAmmo={TextureBlock.InfiniteAmmo}, HasInfiniteAmmo(_actionData)={TextureBlock.HasInfiniteAmmo(ActionData)}");
    }

    /// <summary>
    /// Calculates and sets the total paint count available from entity inventory and storage.
    /// Gets entity available paint from bag and inventory, then combines with storage paint count.
    /// </summary>
    public void CalculateAndSetPaintCountAvailable()
    {
        // Get entity available paint
        ItemActionTextureBlockData itemActionTextureBlockData = ActionData;
        int paintCost = BlockTextureData.list[itemActionTextureBlockData.idx].PaintCost;
        EntityAlive holdingEntity = ActionData.invData.holdingEntity;

        int entityAvailableCount = holdingEntity.bag.GetItemCount(AmmoType);
        entityAvailableCount += holdingEntity.inventory.GetItemCount(AmmoType);

        // Get the total paint required and available, including the entity's held paint
        PaintCountAvailable = ItemTexture.ItemTexture_GetAmmoCount(AmmoType, entityAvailableCount);
    }

    /// <summary>
    /// Determines if paint consumption should be skipped (god mode, infinite ammo, creative mode, etc.)
    /// </summary>
    /// <returns>True if paint consumption should be skipped, false if paint should be consumed normally</returns>
    public bool ShouldSkipPaintConsumption()
    {
        // Invalid context - default to skipping paint consumption
        if (TextureBlock == null)
        {
            return true;
        }

        if (TextureBlock.InfiniteAmmo || TextureBlock.HasInfiniteAmmo(ActionData))
        {
            return true;
        }

        // Creative Mode (2) or Debug Mode (8) - skip paint consumption
        var gameMode = GameStats.GetInt(EnumGameStats.GameModeId);
        ModLogger.DebugLog($"{nameof(ShouldSkipPaintConsumption)}: Game mode is {gameMode} for operation {OperationId}");
        if (gameMode == 2 || gameMode == 8)  // TODO: Use constants instead of magic numbers
        {
            return true;
        }

        // Normal gameplay - consume paint
        return false;
    }

    /// <summary>
    /// Checks if we should paint the next face during execution phase.
    /// Decrements the FacesToPaint counter and returns true if paint is available.
    /// </summary>
    /// <returns>True if this face should be painted, false if no more paint is available</returns>
    public bool ShouldPaintFace()
    {
        if (!IsCountingPhase && FacesToPaint > 0)
        {
            FacesToPaint--;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns a string representation of this paint operation context for debugging
    /// </summary>
    /// <returns>Formatted string with operation details</returns>
    public override string ToString()
    {
        var phase = IsCountingPhase ? "COUNTING" : "EXECUTION";
        return $"PaintOperation[Id: {OperationId}, Phase: {phase}, " +
               $"Required: {TotalPaintRequired}, Available: {PaintCountAvailable}, " +
               $"ToRemove: {PaintToRemove}, ToPaint: {FacesToPaint}]";
    }
}