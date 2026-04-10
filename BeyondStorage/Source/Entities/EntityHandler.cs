using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Entities;

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
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Returning cached name '{cachedName}' for entity {entity.entityId}");
#endif
            return cachedName;
        }

        var localisedName = entity.LocalizedEntityName;
        if (!string.IsNullOrEmpty(localisedName))
        {
            name = localisedName;
        }

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Resolved and caching name '{name}' for entity {entity.entityId} ({entity.GetType().Name})");
#endif

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
}
