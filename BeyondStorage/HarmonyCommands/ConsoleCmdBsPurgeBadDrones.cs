using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Diagnostics;
using BeyondStorage.Scripts.Infrastructure;

public class ConsoleCmdBsPurgeBadDrones : ConsoleCmdAbstract
{
    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            if (WorldTools.IsServer())
            {
                ModLogger.Warning("This command can only be executed on the client side.");
                return;
            }

            var paramList = string.Join(" ", _params);
            ModLogger.Info($"Executing {nameof(ConsoleCmdBsPurgeBadDrones)} with parameters: [{paramList}]");
            PurgeBadDrones.DeleteBadDronesForLocalPlayer();
        }
        catch (Exception e)
        {
            ModLogger.Error($"Error in {nameof(ConsoleCmdBsPurgeBadDrones)}: {e.Message}", e);
        }
    }

    public override string[] getCommands()
    {
        return
        [
            "bspurgebaddrones",
        ];
    }

    public override string getDescription()
    {
        return "Purges the drones not linked to anything";
    }
}