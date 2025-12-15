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
        BsCommandRegistry.RegisterCommand("bsshowconfig", "Displays the current active config settings");
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
#if DEBUG
            var paramList = string.Join(" ", _params);
            ModLogger.DebugLog($"Executing {nameof(ConsoleCmdBsShowConfig)} with parameters: [{paramList}]");
#endif
            ConfigDisplayHelper.ShowConfig();
        }
        catch (Exception e)
        {
            ModLogger.Error($"Error in {nameof(ConsoleCmdBsShowConfig)}: {e.Message}", e);
        }
    }

    public override string[] getCommands()
    {
        return new[]
        {
            "bsshowconfig"
        };
    }

    public override string getDescription()
    {
        return "Displays the current active config settings";
    }
}