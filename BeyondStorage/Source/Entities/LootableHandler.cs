using BeyondStorage.Infrastructure;

namespace BeyondStorage.Entities;

/// <summary>
/// Handles processing and filtering of items from entities and tile entities with slot lock support.
/// Provides methods for retrieving items based on lock status for three operation types:
/// Push (source items from unlocked slots), Pull (destination items), and Loadout (locked slot items).
/// </summary>
public static class LootableHandler
{
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
    /// Gets the packed lock state for a lootable tile entity.
    /// Returns null when the lootable has no slot lock support, which the store
    /// treats as all slots being unlocked.
    /// </summary>
    /// <param name="lootable">The lootable tile entity to get lock state from</param>
    /// <returns>Packed lock state, or null if slot locking is not supported or lootable is null</returns>
    public static PackedBoolArray GetLootableLockedSlots(ITileEntityLootable lootable)
    {
        if (lootable == null || !lootable.HasSlotLocksSupport)
        {
            return null;
        }

        return lootable.SlotLocks;
    }
}