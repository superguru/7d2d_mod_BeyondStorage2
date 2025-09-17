using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Source.HarmonyCommands;

internal class BsCommandRegistry
{
    private static readonly Dictionary<string, CommandInfo> _registeredCommands = new();

    public static void RegisterCommand(string commandName, string description, string usage = null)
    {
        if (string.IsNullOrEmpty(commandName))
        {
            return;
        }

        var commandInfo = new CommandInfo
        {
            Name = commandName.ToLower(),
            Description = description ?? "No description available",
            Usage = usage ?? commandName
        };

        _registeredCommands[commandInfo.Name] = commandInfo;
        ModLogger.DebugLog($"Registered command: {commandName}");
    }

    public static List<CommandInfo> GetAllCommands()
    {
        return _registeredCommands.Values.OrderBy(x => x.Name).ToList();
    }

    public static CommandInfo GetCommand(string commandName)
    {
        if (string.IsNullOrEmpty(commandName))
        {
            return null;
        }

        _registeredCommands.TryGetValue(commandName.ToLower(), out var commandInfo);
        return commandInfo;
    }

    public static bool IsCommandRegistered(string commandName)
    {
        return !string.IsNullOrEmpty(commandName) && _registeredCommands.ContainsKey(commandName.ToLower());
    }

    public static void UnregisterCommand(string commandName)
    {
        if (!string.IsNullOrEmpty(commandName) && _registeredCommands.Remove(commandName.ToLower()))
        {
            ModLogger.DebugLog($"Unregistered command: {commandName}");
        }
    }

    public static void ClearAllCommands()
    {
        _registeredCommands.Clear();
        ModLogger.DebugLog(" All registered commands cleared");
    }

    public class CommandInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Usage { get; set; }
    }
}