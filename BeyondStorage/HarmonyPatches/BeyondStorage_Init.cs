using System.Reflection;
using BeyondStorage.HarmonyPatches.Item;
using BeyondStorage.Scripts.Caching;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game.Functions;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Game.Ranged;
using BeyondStorage.Scripts.Game.Recipe;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Multiplayer;
using BeyondStorage.Scripts.Storage;
using HarmonyLib;

#if DEBUG
using HarmonyLib.Tools;
#endif

namespace BeyondStorage;

public class BeyondStorage : IModApi
{
    private static BeyondStorage _context;

    internal static Mod ModInstance;

    public void InitMod(Mod modInstance)
    {
        _context = this;
        ModConfig.LoadConfig(_context);
        ModInstance = modInstance;
        var harmony = new Harmony(GetType().ToString());
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
            nameof(XUiCItemActionListPatches.ActionList_UpdateVisibleActions),
            //nameof(XUiMPlayerInventoryCraftPatches.)

            nameof(ItemCommon.HasItemInStorage),
            nameof(ItemCommon.ItemCommon_GetStorageItemCount),
            nameof(ItemCommon.ItemCommon_GetTotalAvailableItemCount),
            nameof(ItemCommon.ItemRemoveRemaining),

            nameof(ItemCraft.EntryBinding_AddPullableSourceStorageItemCount),
            nameof(ItemCraft.ItemCraft_AddPullableSourceStorageStacks),
            nameof(ItemCraft.ItemCraft_MaxGetAllStorageStacks),

            nameof(ItemRepair.ItemRepairOnActivatedGetItemCount),

            nameof(PurchasingCommon.GetAvailableSpaceWithStorage),
            nameof(PurchasingCommon.GetEnhancedAvailableSpace),
            nameof(PurchasingCommon.GetRemovableCountWithStorage),

            nameof(Ranged.GetAmmoCount),

            $"{nameof(WorkstationRecipe.BackgroundWorkstation_CraftCompleted)}.{nameof(WorkstationRecipe.Update_OpenWorkstations)}",
            $"{nameof(WorkstationRecipe.ForegroundWorkstation_CraftCompleted)}.{nameof(WorkstationRecipe.Update_OpenWorkstations)}",
        ]);
    }
}