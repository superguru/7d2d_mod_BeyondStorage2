using System;
using System.Collections.Generic;
using System.Text;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.TileEntities;

/// <summary>
/// Handles processing and filtering of items from entity tile entities with slot lock support.
/// </summary>
public static class LootableItemHandler
{
    private static readonly PackedBoolArray s_emptyLockedSlots = new PackedBoolArray();

    public static ItemStack[] GetLootableItems(EntityAlive entity)
    {
        if (entity == null || entity.bag == null)
        {
            return [];
        }

        var bag = entity.bag;
        var items = bag.items;
        if (items == null || items.Length == 0)
        {
            return [];
        }

        // Check if the bag has locked slots support
        var lockedSlots = bag.LockedSlots;
        if (lockedSlots == null || lockedSlots.Length == 0)
        {
            return items;
        }

        // Get container size from lootContainer if available
        int? totalSlots = null;
        if (entity.lootContainer != null)
        {
            var containerSize = entity.lootContainer.GetContainerSize();
            totalSlots = containerSize.x * containerSize.y;
        }

        return GetItemsWithSlotFiltering(items, lockedSlots, totalSlots);
    }

    /// <summary>
    /// Gets items from a entity entity, filtering out items from locked slots.
    /// </summary>
    /// <param name="lootable">The entity entity to extract items from</param>
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

        var containerSize = lootable.GetContainerSize();
        int totalSlots = containerSize.x * containerSize.y;
        return GetItemsWithSlotFiltering(items, lootable.SlotLocks ?? s_emptyLockedSlots, totalSlots);
    }

    /// <summary>
    /// Core logic for filtering items based on locked slots.
    /// </summary>
    /// <param name="items">The item array to filter</param>
    /// <param name="lockedSlots">The locked slots array</param>
    /// <param name="totalSlots">Total container slots (null if unknown)</param>
    /// <returns>Array of ItemStack objects from unlocked slots</returns>
    private static ItemStack[] GetItemsWithSlotFiltering(ItemStack[] items, PackedBoolArray lockedSlots, int? totalSlots)
    {
        // Calculate maximum slots to check
        int lockedSlotsLength = lockedSlots.Length;
        int itemsLength = items.Length;
        int maxSlots = totalSlots.HasValue
            ? Math.Min(totalSlots.Value, itemsLength)
            : Math.Min(lockedSlotsLength, itemsLength);

        // Pre-calculate result capacity to minimize reallocations
        int estimatedUnlockedSlots = Math.Min(maxSlots, Math.Max(0, maxSlots - CountLockedSlots(lockedSlots, maxSlots)));
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
    /// <param name="lootable">The entity entity to log information for</param>
    /// <param name="tileEntity">The tile entity for position information</param>
    /// <param name="methodName">The calling method name for logging</param>
    public static void LogLootableSlotLocks(StorageContext context, ITileEntityLootable lootable, TileEntity tileEntity, string methodName)
    {
        if (!(context?.Config?.IsDebugLogSettingsAccess ?? false))
        {
            return;
        }

#if DEBUG
        var hasLockSlotSupport = lootable.HasSlotLocksSupport;
        if (hasLockSlotSupport)
        {
            ModLogger.DebugLog($"{methodName}: Found entity {lootable} with slot locks support {tileEntity.ToWorldPos()}");

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
#endif
    }

    public static void MarkLootableModified(ITileEntityLootable lootable)
    {
        if (lootable == null)
        {
            ModLogger.DebugLog("MarkLootableModified: entity is null");
            return;
        }

        lootable.SetModified();
    }
    public static void MarkLootableModified(EntityVehicle entity)
    {
        if (entity == null || entity.bag == null)
        {
            ModLogger.DebugLog("MarkLootableModified: entity or bag is null");
            return;
        }

        entity.SetBagModified();
    }
}