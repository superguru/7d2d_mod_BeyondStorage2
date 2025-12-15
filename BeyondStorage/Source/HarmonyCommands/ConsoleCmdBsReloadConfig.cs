using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using BeyondStorage.Source.HarmonyCommands;

public class ConsoleCmdBsReloadConfig : ConsoleCmdAbstract
{
    static ConsoleCmdBsReloadConfig()
    {
        // Register this command when the class is first loaded
        BsCommandRegistry.RegisterCommand("bsreloadconfig", "Reloads configuration from disk and invalidates all caches");
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
#if DEBUG
            var paramList = string.Join(" ", _params);
            ModLogger.DebugLog($"Executing {nameof(ConsoleCmdBsReloadConfig)} with parameters: [{paramList}]");
#endif
            ReloadConfigAndInvalidateCaches();
        }
        catch (Exception e)
        {
            ModLogger.Error($"Error in {nameof(ConsoleCmdBsReloadConfig)}: {e.Message}", e);
        }
    }

    private void ReloadConfigAndInvalidateCaches()
    {
        try
        {
            // Reload config from disk
            ModLogger.Info("Reloading configuration from disk...");
            ConfigReloadHelper.ReloadConfig();

            // Invalidate all caches
            ModLogger.Info("Invalidating all caches...");
            InvalidateAllCaches();

            ModLogger.Info("Configuration reloaded and all caches invalidated successfully.");
            ModLogger.Info("");

            // Display current config using shared helper
            ConfigDisplayHelper.ShowConfig();
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Failed to reload config or invalidate caches: {ex.Message}", ex);
            ModLogger.Info("Config reload failed. Current configuration state may be inconsistent.");
        }
    }

    /// <summary>
    /// Invalidates all mod caches to ensure fresh data after config reload
    /// </summary>
    private static void InvalidateAllCaches()
    {
        // Invalidate storage context factory cache
        StorageContextFactory.InvalidateCache();

        // Invalidate global ItemStack cache
        ItemStackCacheManager.InvalidateGlobalCache();

        ModLogger.Info("All caches invalidated:");
        ModLogger.Info("  - StorageContext cache");
        ModLogger.Info("  - Global ItemStack cache");
    }

    public override string[] getCommands()
    {
        return new[]
        {
            "bsreloadconfig"
        };
    }

    public override string getDescription()
    {
        return "Reloads configuration from disk and invalidates all caches";
    }
}