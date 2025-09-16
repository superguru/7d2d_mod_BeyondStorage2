using BeyondStorage.Scripts.Game.Item;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Reload;

[HarmonyPatch(typeof(ItemActionRanged))]
internal static class ItemActionRangedPatches
{
    // Used For:
    //          Weapon Reload (check if allowed to reload)
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionRanged.CanReload))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionRanged_CanReload_Prefix(ItemActionRanged __instance, ItemActionData _actionData, ref bool __result)
    {
        // Check infinite ammo first (highest priority)
        if (__instance.HasInfiniteAmmo(_actionData))
        {
            __result = true;
            return false; // Skip original method
        }

        ItemActionRanged.ItemActionDataRanged actionData = (ItemActionRanged.ItemActionDataRanged)_actionData;
        ItemValue holdingItemItemValue = _actionData.invData.holdingEntity.inventory.holdingItemItemValue;
        ItemValue ammoItemValue = ItemClass.GetItem(__instance.MagazineItemNames[holdingItemItemValue.SelectedAmmoTypeIndex]);
        int magazineSize = (int)EffectManager.GetValue(PassiveEffects.MagazineSize, holdingItemItemValue, __instance.BulletsPerMagazine, _actionData.invData.holdingEntity);
        EntityPlayerLocal entityPlayerLocal = _actionData.invData.holdingEntity as EntityPlayerLocal;

        // Check prerequisites (original logic)
        if (!ItemActionRanged.NotReloading(actionData) ||
            (entityPlayerLocal?.CancellingInventoryActions == true) ||
            _actionData.invData.itemValue.Meta >= magazineSize)
        {
            __result = false;
            return false; // Skip original method
        }

        // Priority order: Bag → Toolbelt → Storage
        // Check bag first
        bool hasAmmoInBag = _actionData.invData.holdingEntity.bag.GetItemCount(ammoItemValue) > 0;
        if (hasAmmoInBag)
        {
            __result = true;
            return false; // Skip original method
        }

        // Check toolbelt second
        bool hasAmmoInToolbelt = _actionData.invData.holdingEntity.inventory.GetItemCount(ammoItemValue) > 0;
        if (hasAmmoInToolbelt)
        {
            __result = true;
            return false; // Skip original method
        }

        // Check storage last (fallback)
        bool canReloadFromStorage = ItemCommon.HasItemInStorage(ammoItemValue);
        __result = canReloadFromStorage;

        return false; // Skip original method
    }
}