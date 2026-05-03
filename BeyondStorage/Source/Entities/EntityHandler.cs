using BeyondStorage.Game.UI;
using BeyondStorage.Infrastructure;

namespace BeyondStorage.Entities;

public static class EntityHandler
{
    public static string GetEntityName(Entity entity)
    {
#if DEBUG
        const string d_MethodName = nameof(GetEntityName);
#endif
        string name = "Unnamed Entity";

        if (entity == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: entity is null, returning default name");
#endif
            return name;
        }

        // Check cache first
        if (EntityNameCache.TryGetName(entity, out string cachedName))
        {
            return cachedName;
        }

        var localisedName = entity.LocalizedEntityName;
        if (!string.IsNullOrEmpty(localisedName))
        {
            name = localisedName;
        }

        EntityNameCache.CacheName(entity, name);
        return name;
    }

    public static string GetPlayerName(EntityPlayerLocal entity)
    {
        string name = "Unnamed Player Lootable";

        var cachedPlayerName = entity?.cachedPlayerName;
        if (cachedPlayerName != null)
        {
            var displayname = cachedPlayerName.DisplayName;
            if (!string.IsNullOrEmpty(displayname))
            {
                //name = $"[007F0E]{displayname}[-]";  // decorated version with green color
                name = displayname;
            }
        }

        return name;
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
    /// Marks a player entity's inventory as modified, triggering UI updates for backpack and toolbelt.
    /// </summary>
    /// <param name="entity">The player entity whose inventory was modified</param>
    public static void MarkPlayerInventoryModified(EntityPlayerLocal entity)
    {
        const string d_MethodName = nameof(MarkPlayerInventoryModified);

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
    /// Triggers updates for vehicle bag, loot container, and notifies any open vehicle windows.
    /// </summary>
    /// <param name="entity">The vehicle entity whose storage was modified</param>
    public static void MarkVehicleStorageModified(EntityVehicle entity)
    {
        const string d_MethodName = nameof(MarkVehicleStorageModified);

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
