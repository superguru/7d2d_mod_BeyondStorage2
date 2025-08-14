using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;

public class ConsoleCmdBsShowConfig : ConsoleCmdAbstract
{
    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            var paramList = string.Join(" ", _params);
#if DEBUG
            ModLogger.Info($"Executing {nameof(ConsoleCmdBsShowConfig)} with parameters: [{paramList}]");
#endif
            ShowConfig();
        }
        catch (Exception e)
        {
            ModLogger.Error($"Error in {nameof(ConsoleCmdBsShowConfig)}: {e.Message}", e);
        }
    }

    public void ShowConfig()
    {
        var snapshot = ConfigSnapshot.Current;
        string configJson = snapshot.ToJson();
        ModLogger.Info($"Current Config Snapshot:\n{configJson}");
    }

    public override string[] getCommands()
    {
        return
        [
            "bsconfig",
            "bsshowconfig",
        ];
    }

    public override string getDescription()
    {
        return "Displays the current active config settings";
    }
}