using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

public class ItemTexture
{
    public static bool ItemTexture_checkAmmo(int currentCount, ItemActionData _actionData, ItemValue ammoType)
    {
        const string d_MethodName = nameof(ItemTexture_checkAmmo);

        if (currentCount > 0)
        {
            return true;
        }

        if (!IsValidAmmoType(ammoType, d_MethodName))
        {
            return false;
        }

        if (!ModConfig.EnableForBlockTexture())
        {
            return false;
        }

        if (!IsValidLocalPlayer(_actionData, d_MethodName))
        {
            return false;
        }

        var hasAmmo = ContainerUtils.HasItem(ammoType);
        LogUtil.DebugLog($"{d_MethodName}: hasAmmo is {hasAmmo} for ammoType {ammoType.ItemClass.Name}");

        return hasAmmo;
    }

    public static int ItemTexture_GetAmmoCount(ItemValue ammoType, int entityAvailableCount)
    {
        // entityAvailableCount is the total of bag count + entity inventory count

        const string d_MethodName = nameof(ItemTexture_GetAmmoCount);

        // Validate inputs
        if (!IsValidAmmoType(ammoType, d_MethodName))
        {
            LogUtil.DebugLog($"{d_MethodName}: Invalid ammo type, returning entityInventoryCount {entityAvailableCount}");
            return Math.Max(0, entityAvailableCount); // Ensure non-negative
        }

        if (entityAvailableCount < 0)
        {
            entityAvailableCount = 0;
        }

        // Check if feature is enabled
        if (!ModConfig.EnableForBlockTexture())
        {
            return entityAvailableCount;
        }

        var storageCount = ContainerUtils.GetItemCount(ammoType);
        var totalAvailableCount = storageCount + entityAvailableCount;

        LogUtil.DebugLog($"{d_MethodName}: ammoType {ammoType.ItemClass.Name}, storageCount {storageCount}, entityAvailableCount {entityAvailableCount}, new total (incl storages) {totalAvailableCount}");

        return totalAvailableCount;
    }

    public static int ItemTexture_RemoveAmmo(ItemValue ammoType, int stillNeeded, bool _ignoreModdedItems = false, IList<ItemStack> _removedItems = null)
    {
        const string d_MethodName = nameof(ItemTexture_RemoveAmmo);

        LogUtil.DebugLog($"{d_MethodName}: Before - ammoType {ammoType?.ItemClass?.Name ?? "null"}, stillNeeded {stillNeeded}, ignoreModdedItems {_ignoreModdedItems}");

        // Early exit conditions
        if (!ModConfig.EnableForBlockTexture())
        {
            LogUtil.DebugLog($"{d_MethodName}: Block texture feature disabled");
            return stillNeeded;
        }

        if (stillNeeded <= 0)
        {
            LogUtil.DebugLog($"{d_MethodName}: No ammo needed, returning {stillNeeded}");
            return stillNeeded;
        }

        // Validate ammo type
        if (!IsValidAmmoType(ammoType, d_MethodName))
        {
            LogUtil.DebugLog($"{d_MethodName}: Invalid ammo type, cannot remove from storage");
            return stillNeeded;
        }

        try
        {
            var removedFromStorage = ContainerUtils.RemoveRemaining(ammoType, stillNeeded, _ignoreModdedItems, _removedItems);
            var result = Math.Max(0, stillNeeded - removedFromStorage); // Ensure non-negative

            LogUtil.DebugLog($"{d_MethodName}: After - ammoType {ammoType.ItemClass.Name}, stillNeeded {stillNeeded}, removedFromStorage {removedFromStorage}, result {result}");

            return result;
        }
        catch (System.Exception ex)
        {
            LogUtil.Error($"{d_MethodName}: Exception while removing ammo: {ex.Message}");
            return stillNeeded; // Return original value on error
        }
    }

    /// <summary>
    /// Validates that the ammo type is not null and has a valid item class.
    /// </summary>
    /// <param name="ammoType">The ammo type to validate</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <returns>True if valid, false otherwise</returns>
    private static bool IsValidAmmoType(ItemValue ammoType, string methodName)
    {
        if (ammoType == null || ammoType.IsEmpty())
        {
            LogUtil.DebugLog($"{methodName}: ammoType is null or empty");
            return false;
        }

        if (ammoType.ItemClass == null)
        {
            LogUtil.DebugLog($"{methodName}: ammoType.ItemClass is null");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that the action data represents a valid local player context.
    /// </summary>
    /// <param name="actionData">The action data to validate</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <returns>True if valid local player, false otherwise</returns>
    private static bool IsValidLocalPlayer(ItemActionData actionData, string methodName)
    {
        var entity = actionData?.invData?.holdingEntity;
        if (entity is not EntityPlayer playerEntity)
        {
            LogUtil.DebugLog($"{methodName}: entity is not a player");
            return false;
        }

        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.Error($"{methodName}: World is null");
            return false;
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.Error($"{methodName}: Primary player is null");
            return false;
        }

        if (playerEntity.entityId != player.entityId)
        {
            LogUtil.DebugLog($"{methodName}: Not the local player (entityId: {playerEntity.entityId} vs {player.entityId})");
            return false;
        }

        LogUtil.DebugLog($"{methodName}: Valid local player context");
        return true;
    }
}