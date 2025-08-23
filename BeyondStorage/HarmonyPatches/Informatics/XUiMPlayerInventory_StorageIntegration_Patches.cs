using System.Threading;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiM_PlayerInventory))]
internal static class XUiMPlayerInventory_StorageIntegration_Patches
{
#if DEBUG
    private static long s_callCounter = 0;
#endif

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.GetItemCountWithMods))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiM_PlayerInventory_GetItemCountWithMods_Postfix(XUiM_PlayerInventory __instance, ItemValue _itemValue, ref int __result)
    {
#if DEBUG
        //const string d_MethodName = nameof(XUiM_PlayerInventory_GetItemCountWithMods_Postfix);
        var callCount = Interlocked.Increment(ref s_callCounter);
#endif
        var entityPlayerCount = __result;
        var storageCount = ItemCommon.ItemCommon_GetStorageItemCount(_itemValue);
        __result = entityPlayerCount + storageCount;
#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName} [{callCount}]: item: {_itemValue.ItemClass.Name}; result {__result} = entityPlayerCount: {entityPlayerCount} + storageCount: {storageCount}");
#endif
    }

    //    [HarmonyPostfix]
    //    [HarmonyPatch(nameof(XUiM_PlayerInventory.GetItemCount), [typeof(int)])]
    //#if DEBUG
    //    [HarmonyDebug]
    //#endif
    private static void XUiM_PlayerInventory_GetItemCount_ItemType_Postfix(XUiM_PlayerInventory __instance, int _itemId, ref int __result)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiM_PlayerInventory_GetItemCount_ItemType_Postfix);
#endif
        var entityPlayerCount = __result;
        //var storageCount = ItemCommon.ItemCommon_GetStorageItemCount(_itemId);
        var itemName = ItemX.NameOf(_itemId);

#if DEBUG
        var message = $"{d_MethodName}: itemId: {_itemId} ({itemName}); result {__result} = entityPlayerCount: {entityPlayerCount} + storageCount: unknown (skipped)";

        //if (_itemId == 65672)
        //{
        //    message = StackTraceProvider.AppendStackTrace(message);
        //}

        ModLogger.DebugLog(message);
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.GetItemCount), [typeof(ItemValue)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiM_PlayerInventory_GetItemCount_ItemValue_Postfix(XUiM_PlayerInventory __instance, ItemValue _itemValue, ref int __result)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiM_PlayerInventory_GetItemCount_ItemValue_Postfix);
#endif
        var entityPlayerCount = __result;
        //var storageCount = ItemCommon.ItemCommon_GetStorageItemCount(_itemId);
        var itemName = ItemX.NameOf(_itemValue);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: ({itemName}); result {__result} = entityPlayerCount: {entityPlayerCount} + storageCount: unknown (skipped)");
#endif
    }
}