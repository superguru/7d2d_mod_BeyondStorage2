using System.Collections.Generic;
using System.Text;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using BeyondStorage.Source.Game.UI;

namespace BeyondStorage.Scripts.Entities;

/// <summary>
/// Handles processing and filtering of items from entity tile entities with slot lock support.
/// </summary>
public static class LootableHandler
{
    private static readonly PackedBoolArray s_emptyLockedSlots = new();

    public static ItemStack[] GetAllSlotItems(EntityAlive entity)
    {
        var items = entity?.bag?.items;
        if (items == null || items.Length == 0)
        {
            return [];
        }

        return items;
    }

    public static ItemStack[] GetPushableItems(EntityAlive entity)
    {
        var items = GetAllSlotItems(entity);

        if (items.Length == 0)
        {
            return items;
        }

        // Check if the bag has locked slots support
        var bag = entity.bag;
        var lockedSlots = bag?.LockedSlots;
        if (lockedSlots == null || lockedSlots.Length == 0)
        {
            return items;
        }

        return GetItemsWithSlotFiltering(items, lockedSlots, filterEmptySlots: true);
    }

    public static ItemStack[] GetPullableItems(EntityAlive entity)
    {
        var items = GetAllSlotItems(entity);

        if (items.Length == 0)
        {
            return items;
        }

        return GetItemsWithSlotFiltering(items, s_emptyLockedSlots, filterEmptySlots: true);
    }

    public static ItemStack[] GetAllSlotItemsStacks(ITileEntityLootable lootable)
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

        return items;
    }

    public static ItemStack[] GetPushableItems(ITileEntityLootable lootable)
    {
        var items = GetAllSlotItemsStacks(lootable);

        if (items.Length == 0)
        {
            return items;
        }

        if (!lootable.HasSlotLocksSupport)
        {
            return items;
        }

        var slotLocks = lootable.SlotLocks;
        if (slotLocks == null || slotLocks.Length == 0)
        {
            return items;
        }

        return GetItemsWithSlotFiltering(items, slotLocks, filterEmptySlots: true);
    }

    /// <summary>
    /// Gets items from a lootable, filtering out items from locked slots.
    /// </summary>
    /// <param name="lootable">The entity entity to extract items from</param>
    /// <returns>Array of ItemStack objects from unlocked slots</returns>
    public static ItemStack[] GetPullableItems(ITileEntityLootable lootable)
    {
        var items = GetAllSlotItemsStacks(lootable);

        if (items.Length == 0)
        {
            return items;
        }

        return GetItemsWithSlotFiltering(items, s_emptyLockedSlots, filterEmptySlots: true);
    }

    /// <summary>
    /// Core logic for filtering items based on locked slots.
    /// </summary>
    /// <param name="items">The item array to filter</param>
    /// <param name="lockedSlots">The locked slots array</param>
    /// <returns>Array of ItemStack objects from unlocked slots</returns>
    private static ItemStack[] GetItemsWithSlotFiltering(ItemStack[] items, PackedBoolArray lockedSlots, bool filterEmptySlots)
    {
        int itemsLength = items.Length;

        int lockedSlotsLength = lockedSlots?.Length ?? 0;
        bool filterLockedSlots = lockedSlotsLength > 0;

        // Pre-calculate result capacity to minimize reallocations
        var result = new List<ItemStack>(itemsLength);

        // Single loop optimization - avoid nested loops
        for (int slotIndex = 0; slotIndex < itemsLength; slotIndex++)
        {
            // Skip locked slots early
            if (filterLockedSlots && (slotIndex < lockedSlotsLength && lockedSlots[slotIndex]))
            {
                continue;
            }

            var stack = items[slotIndex];
            if (filterEmptySlots && (stack == null || stack.count == 0))
            {
                continue;
            }

            result.Add(stack);
        }

        return [.. result];
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
        if (context?.Config?.IsDebugLogSettingsAccess != true)
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

    public static string GetLootableName(ITileEntityLootable lootable)
    {
        const string d_MethodName = nameof(GetLootableName);

        string name = "Unnamed Lootable";

        if (lootable == null)
        {
            return name;
        }

        if (lootable.TryGetSelfOrFeature(out TEFeatureSignable signable) && signable != null)
        {
            // Check cache first
            if (EntityNameCache.TryGetName(signable, out string cachedName))
            {
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Returning cached name '{cachedName}' for signable");
#endif
                return cachedName;
            }

            var authoredText = signable.GetAuthoredText();
            if (authoredText != null && !string.IsNullOrEmpty(authoredText.Text))
            {
                name = authoredText.Text;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Found signed text '{name}' for signable");
#endif
                EntityNameCache.CacheName(signable, name);
                return name;
            }
        }

        Block block = WorldTools.GetBlockFromEntity(lootable);

        var localisedName = block.localizedBlockName;
        if (!string.IsNullOrEmpty(localisedName))
        {
            name = localisedName;
        }

        EntityNameCache.CacheName(lootable, name);
        return name;
    }


    public static void MarkLootableModified(ITileEntityLootable lootable)
    {
        const string d_MethodName = nameof(MarkLootableModified);

        if (lootable == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: entity is null");
            return;
        }

        lootable.SetModified();
    }

    public static void MarkLootableModified(EntityPlayerLocal entity)
    {
        const string d_MethodName = nameof(MarkLootableModified);

        if (entity == null || entity.playerUI == null || entity.playerUI.xui == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: entity or player UI is null");
            return;
        }

        //TODO: This is a bit hacky, but we need to mark both backpack and toolbelt as modified to ensure the UI updates correctly.
        entity.playerUI.xui.PlayerInventory.onBackpackItemsChanged();
        entity.playerUI.xui.PlayerInventory.onToolbeltItemsChanged();
    }

    public static void MarkLootableModified(EntityVehicle entity)
    {
        const string d_MethodName = nameof(MarkLootableModified);

        if (entity == null || entity.bag == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: entity or bag is null");
            return;
        }

        entity.SetBagModified();
        entity.lootContainer?.setModified();

        WindowStateManager.SetOpenVehicleEntityModified();
    }
}