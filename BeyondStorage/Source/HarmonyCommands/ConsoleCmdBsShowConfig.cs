using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.HarmonyCommands;

public class ConsoleCmdBsShowConfig : ConsoleCmdAbstract
{
    static ConsoleCmdBsShowConfig()
    {
        // Register this command when the class is first loaded
        BsCommandRegistry.RegisterCommand("bsconfig", "Displays the current active config settings");
    }

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
        ModLogger.Info($"Current Config Snapshot:\n{configJson}\nDo not copy and paste this into the config.json file. The values above are formatted for reading in the console.");
    }

    public override string[] getCommands()
    {
        return
        [
            "bsconfig",
        ];
    }

    public override string getDescription()
    {
        return "Displays the current active config settings";
    }
}