using BeyondStorage.Scripts.Game.Item;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Vehicle;

[HarmonyPatch(typeof(EntityVehicle))]
internal static class EntityVehiclePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(EntityVehicle.hasGasCan))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void EntityVehicle_hasGasCan_Patch(EntityVehicle __instance, ref bool __result)
    {
        // If player already has fuel, no need to check storage
        if (__result)
        {
            return;
        }

        // Get the fuel item for this vehicle
        string fuelItemName = __instance.GetVehicle()?.GetFuelItem() ?? "";
        if (string.IsNullOrEmpty(fuelItemName))
        {
            return;
        }

        ItemValue fuelItemValue = ItemClass.GetItem(fuelItemName);

        // Check if storage has the fuel item
        __result = ItemCommon.HasItemInStorage(fuelItemValue);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EntityVehicle.takeFuel), [typeof(EntityAlive), typeof(int)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool EntityVehicle_takeFuel_Prefix(EntityVehicle __instance, EntityAlive _entityFocusing, int count, ref float __result)
    {
        // Validate entity is a player (original logic)
        EntityPlayer entityPlayer = _entityFocusing as EntityPlayer;
        if (!entityPlayer)
        {
            __result = 0f;
            return false; // Skip original method
        }

        // Get fuel item for this vehicle (original logic)
        string fuelItem = __instance.GetVehicle().GetFuelItem();
        if (fuelItem == "")
        {
            __result = 0f;
            return false; // Skip original method
        }

        ItemValue item = ItemClass.GetItem(fuelItem);

        // Use sequential removal: Bag → Toolbelt → Storage (enhanced logic)
        // Note: Original game uses Toolbelt → Bag, but we use Bag → Toolbelt for consistency
        int totalRemoved = ItemCommon.RemoveItemsSequential(entityPlayer.bag, entityPlayer.inventory, item, count);

        if (totalRemoved > 0)
        {
            // Update UI to show fuel consumption (original logic)
            LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(_entityFocusing as EntityPlayerLocal);
            if (uIForPlayer != null)
            {
                ItemStack itemStack = new ItemStack(item, totalRemoved);
                uIForPlayer.xui.CollectedItemList.RemoveItemStack(itemStack);
            }
            else
            {
                Log.Warning("EntityVehicle::takeFuel - Failed to remove item stack from player's collected item list.");
            }
        }

        __result = (float)totalRemoved;
        return false; // Skip original method
    }
}