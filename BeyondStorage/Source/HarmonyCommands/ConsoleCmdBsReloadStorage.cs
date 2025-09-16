using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

public class ConsoleCmdBsReloadStorage : ConsoleCmdAbstract
{
    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            var paramList = string.Join(" ", _params);
#if DEBUG
            ModLogger.Info($"Executing {nameof(ConsoleCmdBsReloadStorage)} with parameters: [{paramList}]");
#endif
            ReloadStorage();
        }
        catch (Exception e)
        {
            ModLogger.Error($"Error in {nameof(ConsoleCmdBsReloadStorage)}: {e.Message}", e);
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
            "bsreloadstorage",
            "bsclearcache",
        ];
    }

    public override string getDescription()
    {
        return "Invalidates cache and reloads items from storage";
    }
}