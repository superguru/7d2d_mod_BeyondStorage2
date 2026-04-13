using System.Collections.Generic;
using System.Text;
using BeyondStorage.Source.Game.UI;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.Storage;

namespace BeyondStorage.Source.Entities;

/// <summary>
/// Handles processing and filtering of items from entities and tile entities with slot lock support.
/// Provides methods for retrieving items based on lock status for three operation types:
/// Push (source items from unlocked slots), Pull (destination items), and Loadout (locked slot items).
/// </summary>
public static class LootableHandler
{
    /// <summary>
    /// Specifies how to filter storage items based on slot lock status.
    /// </summary>
    private enum StorageFilter
    {
        /// <summary>Returns all items regardless of lock status</summary>
        AllItems,
        /// <summary>Returns only items from unlocked slots</summary>
        UnlockedOnly,
        /// <summary>Returns only items from locked slots</summary>
        LockedOnly
    }

    /// <summary>
    /// Gets all item stacks from an entity's bag without any filtering.
    /// </summary>
    /// <param name="entity">The entity to get items from</param>
    /// <returns>Array of all ItemStack objects in the entity's bag, or an empty array if the bag is null or empty</returns>
    public static ItemStack[] GetAllSlotItems(EntityAlive entity)
    {
        var items = entity?.bag?.items;
        if (items == null || items.Length == 0)
        {
            return [];
        }

        return items;
    }

    /// <summary>
    /// Gets item stacks from an entity's bag that can be pushed to storage targets.
    /// Filters out items from locked slots and empty slots.
    /// </summary>
    /// <param name="entity">The entity to get pushable items from</param>
    /// <returns>Array of ItemStack objects from unlocked, non-empty slots</returns>
    public static ItemStack[] GetPushableItems(EntityAlive entity)
    {
        var items = GetAllSlotItems(entity);
        if (items.Length == 0)
        {
            return [];
        }

        var bag = entity.bag;
        var lockedSlots = bag?.LockedSlots;

        return GetFilteredItems(items, StorageFilter.UnlockedOnly, lockedSlots);
    }

    /// <summary>
    /// Gets item stacks from locked slots in an entity's bag (loadout items).
    /// Filters out items from unlocked slots and empty slots.
    /// </summary>
    /// <param name="entity">The entity to get loadout items from</param>
    /// <returns>Array of ItemStack objects from locked, non-empty slots</returns>
    /// <remarks>
    /// When the entity's bag does not have locked slots, an empty array is returned.
    /// </remarks>
    internal static ItemStack[] GetLoadoutItems(EntityAlive entity)
    {
        var items = GetAllSlotItems(entity);
        if (items.Length == 0)
        {
            return [];
        }

        var bag = entity.bag;
        var lockedSlots = bag?.LockedSlots;

        return GetFilteredItems(items, StorageFilter.LockedOnly, lockedSlots);
    }

    /// <summary>
    /// Gets item stacks from an entity's bag that can be pulled from storage sources.
    /// Filters out empty slots only (no locked slot filtering for entity bags).
    /// </summary>
    /// <param name="entity">The entity to get consumable items from</param>
    /// <returns>Array of ItemStack objects from non-empty slots</returns>
    /// <remarks>
    /// Gets all non-empty item stacks that can act as destinations for pull operations.
    /// These items can potentially stack with/absorb incoming items from storage sources.
    /// </remarks>
    public static ItemStack[] GetConsumableItems(EntityAlive entity)
    {
        var items = GetAllSlotItems(entity);
        if (items.Length == 0)
        {
            return [];
        }

        return GetFilteredItems(items, StorageFilter.AllItems, lockedSlots: null);
    }

    /// <summary>
    /// Gets all item stacks from a lootable tile entity without any filtering.
    /// </summary>
    /// <param name="lootable">The lootable tile entity to get items from</param>
    /// <returns>Array of all ItemStack objects in the lootable, or an empty array if the lootable is null or has no items</returns>
    public static ItemStack[] GetAllSlotItems(ITileEntityLootable lootable)
    {
        var items = lootable?.items;
        if (items == null || items.Length == 0)
        {
            return [];
        }

        return items;
    }

    /// <summary>
    /// Gets item stacks from a lootable tile entity that can be pushed to storage targets.
    /// Filters out items from locked slots (if slot locking is supported) and empty slots.
    /// </summary>
    /// <param name="lootable">The lootable tile entity to get pushable items from</param>
    /// <returns>Array of ItemStack objects from unlocked, non-empty slots</returns>
    /// <remarks>
    /// When the lootable does not support slot locking, all non-empty items are returned.
    /// </remarks>
    public static ItemStack[] GetPushableItems(ITileEntityLootable lootable)
    {
        var items = GetAllSlotItems(lootable);
        if (items.Length == 0)
        {
            return [];
        }

        var lockedSlots = lootable.HasSlotLocksSupport ? lootable.SlotLocks : null;

        return GetFilteredItems(items, StorageFilter.UnlockedOnly, lockedSlots);
    }

    /// <summary>
    /// Gets item stacks from locked slots in a lootable tile entity (loadout items).
    /// Filters out items from unlocked slots and empty slots.
    /// </summary>
    /// <param name="lootable">The lootable tile entity to get loadout items from</param>
    /// <returns>Array of ItemStack objects from locked, non-empty slots</returns>
    /// <remarks>
    /// When the lootable does not support slot locking, an empty array is returned.
    /// </remarks>
    internal static ItemStack[] GetLoadoutItems(ITileEntityLootable lootable)
    {
        var items = GetAllSlotItems(lootable);
        if (items.Length == 0)
        {
            return [];
        }

        var lockedSlots = lootable.HasSlotLocksSupport ? lootable.SlotLocks : null;

        return GetFilteredItems(items, StorageFilter.LockedOnly, lockedSlots);
    }

    /// <summary>
    /// Gets item stacks from a lootable tile entity that can be pulled from storage sources.
    /// Filters out empty slots only (no locked slot filtering for pull operations).
    /// </summary>
    /// <param name="lootable">The lootable tile entity to pull items from</param>
    /// <returns>Array of ItemStack objects from non-empty slots</returns>
    public static ItemStack[] GetConsumableItems(ITileEntityLootable lootable)
    {
        var items = GetAllSlotItems(lootable);
        if (items.Length == 0)
        {
            return [];
        }

        return GetFilteredItems(items, StorageFilter.AllItems, lockedSlots: null);
    }

    /// <summary>
    /// Core logic for filtering items based on locked slots and emptiness.
    /// Optimized with a single-pass loop to minimize allocations and improve performance.
    /// Empty slots are always filtered out.
    /// </summary>
    /// <param name="items">The item array to filter</param>
    /// <param name="filter">The inventory filter to apply (AllItems, UnlockedOnly, or LockedOnly)</param>
    /// <param name="lockedSlots">The locked slots array, or null if slot locking is not supported</param>
    /// <returns>Array of non-empty ItemStack objects that pass the specified filter</returns>
    /// <remarks>
    /// - StorageFilter.AllItems: Returns all non-empty items regardless of lock status
    /// - StorageFilter.UnlockedOnly: Returns only non-empty items from unlocked slots (or all if no lock data)
    /// - StorageFilter.LockedOnly: Returns only non-empty items from locked slots (or all if no lock data)
    /// When lock data is unavailable, UnlockedOnly and LockedOnly behave identically to AllItems.
    /// </remarks>
    private static ItemStack[] GetFilteredItems(ItemStack[] items, StorageFilter filter, PackedBoolArray lockedSlots = null)
    {
        int itemsLength = items.Length;
        int lockedSlotsLength = lockedSlots?.Length ?? 0;
        bool hasLockedSlots = lockedSlotsLength > 0;

        var result = new List<ItemStack>(itemsLength);

        for (int slotIndex = 0; slotIndex < itemsLength; slotIndex++)
        {
            var stack = items[slotIndex];

            // Always filter out empty slots
            if (stack == null || stack.count == 0)
            {
                continue;
            }

            // Apply lock-based filtering only when lock data is available
            if (hasLockedSlots)
            {
                // Slots beyond lockedSlots array length are treated as unlocked
                bool isLocked = (slotIndex < lockedSlotsLength) && lockedSlots[slotIndex];

                if (filter == StorageFilter.UnlockedOnly && isLocked)
                {
                    continue;
                }
                else if (filter == StorageFilter.LockedOnly && !isLocked)
                {
                    continue;
                }
            }

            result.Add(stack);
        }

        return [.. result];
    }

    /// <summary>
    /// Logs detailed information about slot locks for debugging purposes.
    /// </summary>
    /// <param name="context">The storage context containing configuration</param>
    /// <param name="lootable">The tile entity to log information for</param>
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

    /// <summary>
    /// Gets the display name for a lootable tile entity, checking custom signs first, then falling back to localized block name.
    /// </summary>
    /// <param name="lootable">The lootable tile entity to get the name for</param>
    /// <returns>The display name of the lootable, or "Unnamed Lootable" if unavailable</returns>
    /// <remarks>
    /// Names are cached to improve performance. Checks in order: custom sign text, localized block name, default fallback.
    /// </remarks>
    public static string GetLootableName(ITileEntityLootable lootable)
    {
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
                return cachedName;
            }

            var authoredText = signable.GetAuthoredText();
            if (authoredText != null && !string.IsNullOrEmpty(authoredText.Text))
            {
                name = authoredText.Text;

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

    /// <summary>
    /// Marks a lootable tile entity as modified to trigger save and network synchronization.
    /// </summary>
    /// <param name="lootable">The lootable tile entity to mark as modified</param>
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

    /// <summary>
    /// Marks a player entity's inventory as modified, triggering UI updates for backpack and toolbelt.
    /// </summary>
    /// <param name="entity">The player entity whose inventory was modified</param>
    public static void MarkLootableModified(EntityPlayerLocal entity)
    {
        const string d_MethodName = nameof(MarkLootableModified);

        if (entity == null || entity.playerUI == null || entity.playerUI.xui == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: entity or player UI is null");
            return;
        }

        entity.playerUI.xui.PlayerInventory.onBackpackItemsChanged();
        entity.playerUI.xui.PlayerInventory.onToolbeltItemsChanged();
    }

    /// <summary>
    /// Marks a vehicle entity's storage as modified, updating both bag and loot container.
    /// </summary>
    /// <param name="entity">The vehicle entity whose storage was modified</param>
    /// <remarks>
    /// Triggers updates for vehicle bag, loot container, and notifies any open vehicle windows.
    /// </remarks>
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