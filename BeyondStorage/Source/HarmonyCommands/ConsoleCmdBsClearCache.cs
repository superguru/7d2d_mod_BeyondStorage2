using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;
using BeyondStorage.Source.HarmonyCommands;

public class ConsoleCmdBsClearCache : ConsoleCmdAbstract
{
    static ConsoleCmdBsClearCache()
    {
        // Register this command when the class is first loaded
        BsCommandRegistry.RegisterCommand("bsclearcache", "Invalidates cache and reloads items from storage");
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            var paramList = string.Join(" ", _params);
#if DEBUG
            ModLogger.Info($"Executing {nameof(ConsoleCmdBsClearCache)} with parameters: [{paramList}]");
#endif
            ReloadStorage();
        }
        catch (Exception e)
        {
            ModLogger.Error($"Error in {nameof(ConsoleCmdBsClearCache)}: {e.Message}", e);
        }
    }

    public void ReloadStorage()
    {
        StorageContextFactory.InvalidateCache();

        ModLogger.Info($"Storage cache invalidated");
    }

    public override string[] getCommands()
    {
        return
        [
            "bsclearcache",
        ];
    }

    public override string getDescription()
    {
        return "Invalidates cache and reloads items from storage";
    }
}