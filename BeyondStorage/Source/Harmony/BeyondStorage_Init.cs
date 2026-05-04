using System.Reflection;
using BeyondStorage.Caching;
using BeyondStorage.Configuration;
using BeyondStorage.Data;
using BeyondStorage.Game.Functions;
using BeyondStorage.Game.Item;
using BeyondStorage.Game.Ranged;
using BeyondStorage.Game.Recipe;
using BeyondStorage.Infrastructure;
using BeyondStorage.Multiplayer;
using BeyondStorage.Storage;
using BeyondStorage.Harmony.Extends;

#if DEBUG
using HarmonyLib.Tools;
#endif

namespace BeyondStorage;

public class BeyondStorageMod : IModApi
{
    public static BeyondStorageMod Context { get; set; }

    internal static Mod s_modInstance;
    private static string s_mod_assembly_path = "";

    internal static string GetModAssemblyPath()
    {
        return s_mod_assembly_path;
    }

    public void InitMod(Mod modInstance)
    {
        Context = this;
        s_mod_assembly_path = modInstance.Path;
        ModConfig.LoadConfig();
        s_modInstance = modInstance;
        var harmony = new HarmonyLib.Harmony(GetType().ToString());
#if DEBUG
        HarmonyFileLog.Enabled = true;
#endif
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        ExcludeCacheLoggers();

        ModEvents.PlayerSpawnedInWorld.RegisterHandler(ServerUtils.PlayerSpawnedInWorld);

        // Game Start Done Called when:
        //      - Loading into singleplayer world
        //      - Starting client hosted multiplayer world
        //      - Loading into dedicated world
        // Not Called during connecting TO client server
        ModEvents.GameStartDone.RegisterHandler(ModLifecycleManager.GameStartDone);

        // Game Shutdown Called when:
        //      - Leaving the world
        ModEvents.GameShutdown.RegisterHandler(ModLifecycleManager.GameShutdown);

        // Player Disconnected Called When:
        //      - Player disconnects from server YOU'RE hosting
        // NOT called when YOU disconnect
        // ModEvents.PlayerDisconnected.RegisterHandler(EventsUtil.PlayerDisconnected);
    }

    private void ExcludeCacheLoggers()
    {
        // Comment out the lines below to enable cache logging for these methods if you're debugging.
        ExpiringCache<StorageContext>.AddSuppressLoggingMethodNames([
            // All StackOps operations
            $"{StackOps.ItemStack_DropMerge_Operation}",
            $"{StackOps.ItemStack_Drop_Operation}",
            $"{StackOps.ItemStack_DropSingleItem_Operation}",
            $"{StackOps.ItemStack_Pickup_Operation}",
            $"{StackOps.ItemStack_Pickup_Half_Stack_Operation}",
            $"{StackOps.ItemStack_Shift_Operation}",
            $"{StackOps.MoveAll_Operation}",
            $"{StackOps.Stack_LockStateChange_Operation}",
            
            // Method-specific suppressions
            nameof(XUiC_ItemActionList_Ext.ActionList_UpdateVisibleActions),
            //nameof(XUiM_PlayerInventory_Patch.)

            nameof(ItemCommon.HasItemInStorage),
            nameof(ItemCommon.ItemCommon_GetStorageItemCount),
            nameof(ItemCommon.ItemCommon_GetTotalAvailableItemCount),
            nameof(ItemCommon.ItemRemoveRemaining),
            nameof(ItemRepair.ItemRepairRefreshGetItemCount),

            nameof(ItemCraft.EntryBinding_AddPullableSourceStorageItemCount),
            nameof(ItemCraft.ItemCraft_AddPullableSourceStorageStacks),
            nameof(ItemCraft.ItemCraft_MaxGetAllStorageStacks),

            nameof(ItemRepair.ItemRepairOnActivatedGetItemCount),

            nameof(PurchasingCommon.GetAvailableSpaceWithStorage),
            nameof(PurchasingCommon.GetEnhancedAvailableSpace),
            nameof(PurchasingCommon.GetRemovableCountWithStorage),

            nameof(Ranged.GetAmmoCount),
            nameof(Ranged.RemoveAmmoForReload),

            $"{nameof(WorkstationRecipe.BackgroundWorkstation_CraftCompleted)}.{nameof(WorkstationRecipe.Update_OpenWorkstations)}",
            $"{nameof(WorkstationRecipe.ForegroundWorkstation_CraftCompleted)}.{nameof(WorkstationRecipe.Update_OpenWorkstations)}",

            // General cache related suppression
            nameof(StorageContextFactory.InvalidateContext),
            nameof(SmartSortingFunctions.SmartPlayerInventoryLoadoutPull),
            nameof(SmartSortingFunctions.SmartPlayerInventoryPush),
            nameof(SmartSortingFunctions.SmartVehicleLoadoutPull),
            nameof(SmartSortingFunctions.SmartVehiclePush),
            nameof(SmartSortingFunctions.SmartWorkstationOutputPush),
        ]);
    }
}