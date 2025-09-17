using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.HarmonyCommands;

public class ConsoleCmdBsHelp : ConsoleCmdAbstract
{
    static ConsoleCmdBsHelp()
    {
        // Register this command when the class is first loaded
        BsCommandRegistry.RegisterCommand("bshelp", "Lists all available BeyondStorage commands and their descriptions");
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
#if DEBUG
            ModLogger.Info($"Executing {nameof(ConsoleCmdBsHelp)}");
#endif
            ShowHelp();
        }
        catch (Exception e)
        {
            ModLogger.Error($"Error in {nameof(ConsoleCmdBsHelp)}: {e.Message}", e);
        }
    }

    private void ShowHelp()
    {
        var allCommands = BsCommandRegistry.GetAllCommands();

        if (allCommands.Count == 0)
        {
            ModLogger.Info("No BeyondStorage commands are currently registered.");
            return;
        }

        ModLogger.Info($"BeyondStorage Commands ({allCommands.Count} available):");
        ModLogger.Info("==========================================");

        // Find the longest command name for formatting
        int maxCommandLength = allCommands.Max(cmd => cmd.Name.Length);

        foreach (var commandInfo in allCommands)
        {
            // Format: "command    - description"
            var paddedCommand = commandInfo.Name.PadRight(maxCommandLength);
            ModLogger.Info($"  {paddedCommand} - {commandInfo.Description}");
        }

        ModLogger.Info("==========================================");
    }

    public override string[] getCommands()
    {
        return new[]
        {
            "bshelp"
        };
    }

    public override string getDescription()
    {
        return "Lists all available BeyondStorage commands and their descriptions";
    }
}