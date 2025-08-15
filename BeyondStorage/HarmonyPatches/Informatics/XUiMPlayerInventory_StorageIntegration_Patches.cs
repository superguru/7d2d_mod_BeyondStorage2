using System.Threading;
using BeyondStorage.Scripts.Game.Item;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiM_PlayerInventory))]
public static class XUiMPlayerInventory_StorageIntegration_Patches
{
#if DEBUG
    private static long s_callCounter = 0;
#endif

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.GetItemCountWithMods))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void XUiM_PlayerInventory_GetItemCountWithMods_Postfix(XUiM_PlayerInventory __instance, ItemValue _itemValue, ref int __result)
    {
#if DEBUG
        var callCount = Interlocked.Increment(ref s_callCounter);
#endif
        int entityPlayerCount = __result;
        int storageCount = ItemCommon.ItemCommon_GetAvailableItemCount(_itemValue);
        __result = entityPlayerCount + storageCount;
#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName} [{callCount}]: item: {_itemValue.ItemClass.Name}; result {__result} = entityPlayerCount: {entityPlayerCount} + storageCount: {storageCount}");
#endif
    }
}