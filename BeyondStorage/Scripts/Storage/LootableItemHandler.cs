using System;
using System.Collections.Generic;
using System.Text;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Handles processing and filtering of items from lootable tile entities with slot lock support.
/// </summary>
public static class LootableItemHandler
{
    private static readonly PackedBoolArray s_emptyLockedSlots = new PackedBoolArray();

    /// <summary>
    /// Gets items from a lootable entity, filtering out items from locked slots.
    /// </summary>
    /// <param name="lootable">The lootable entity to extract items from</param>
    /// <returns>Array of ItemStack objects from unlocked slots</returns>
    public static ItemStack[] GetLootableItems(ITileEntityLootable lootable)
    {
        if (lootable == null)
        {
            return [];
        }

        var items = lootable.items;
        if (items == null || items.Length == 0)
        {
            return [];
        }

        if (!lootable.HasSlotLocksSupport)
        {
            return items;
        }

        PackedBoolArray lockedSlots = lootable.SlotLocks ?? s_emptyLockedSlots;
        var containerSize = lootable.GetContainerSize();
        int cx = containerSize.x;
        int cy = containerSize.y;
        int totalSlots = cx * cy;
        int lockedSlotsLength = lockedSlots.Length;
        int itemsLength = items.Length;
        int maxSlots = Math.Min(totalSlots, itemsLength);

        // Pre-calculate result capacity to minimize reallocations
        int estimatedUnlockedSlots = Math.Min(maxSlots, Math.Max(0, lockedSlotsLength == 0 ? maxSlots : maxSlots - CountLockedSlots(lockedSlots, maxSlots)));
        var result = new List<ItemStack>(estimatedUnlockedSlots);

        // Single loop optimization - avoid nested loops
        for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
        {
            // Skip locked slots early
            if (slotIndex < lockedSlotsLength && lockedSlots[slotIndex])
            {
                continue;
            }

            var stack = items[slotIndex];
            if (stack != null)
            {
                result.Add(stack);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Counts the number of locked slots within the specified range.
    /// </summary>
    /// <param name="lockedSlots">The packed boolean array representing locked slots</param>
    /// <param name="maxSlots">The maximum number of slots to check</param>
    /// <returns>The count of locked slots</returns>
    private static int CountLockedSlots(PackedBoolArray lockedSlots, int maxSlots)
    {
        int count = 0;
        int checkSlots = Math.Min(lockedSlots.Length, maxSlots);

        for (int i = 0; i < checkSlots; i++)
        {
            if (lockedSlots[i])
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Logs detailed information about slot locks for debugging purposes.
    /// </summary>
    /// <param name="context">The storage context containing configuration</param>
    /// <param name="lootable">The lootable entity to log information for</param>
    /// <param name="tileEntity">The tile entity for position information</param>
    /// <param name="methodName">The calling method name for logging</param>
    public static void LogLootableSlotLocks(StorageContext context, ITileEntityLootable lootable, TileEntity tileEntity, string methodName)
    {
        if (!(context?.Config?.IsDebugLogSettingsAccess ?? false))
        {
            return;
        }

        var hasLockSlotSupport = lootable.HasSlotLocksSupport;
        if (hasLockSlotSupport)
        {
            ModLogger.DebugLog($"{methodName}: Found lootable {lootable} with slot locks support {tileEntity.ToWorldPos()}");

            var containerSize = lootable.GetContainerSize();
            int cx = containerSize.x;
            int cy = containerSize.y;

            PackedBoolArray lockedSlots = lootable.SlotLocks;

            // Create grid representation: 1 for locked, 0 for unlocked
            var sb = new StringBuilder(cx * cy * 2);
            int lastColSepIdx = containerSize.x - 1;
            int lastRowSepIdx = cy - 1;
            for (int y = 0; y < cy; y++)
            {
                for (int x = 0; x < cx; x++)
                {
                    int slotIndex = y * cx + x;
                    bool isLocked = lockedSlots != null && slotIndex < lockedSlots.Length && lockedSlots[slotIndex];
                    sb.Append(isLocked ? '1' : '0');

                    // Add space between columns (except for last column)
                    if (x < lastColSepIdx)
                    {
                        sb.Append(' ');
                    }
                }

                // Add newline between rows (except for last row)
                if (y < lastRowSepIdx)
                {
                    sb.AppendLine();
                }
            }

            var lockSlotMap = sb.ToString();
            ModLogger.DebugLog($"{methodName}: Locked slots for {lootable} {containerSize.x}x{containerSize.y}:\n{lockSlotMap}");
        }
    }

    public static void MarkLootableModified(ITileEntityLootable lootable)
    {
        if (lootable == null)
        {
            ModLogger.Error("MarkLootableModified: lootable is null");
            return;
        }

        lootable.SetModified();
    }
}