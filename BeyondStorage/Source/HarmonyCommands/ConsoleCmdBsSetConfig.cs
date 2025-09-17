using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BeyondStorage;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.HarmonyCommands;

public class ConsoleCmdBsSetConfig : ConsoleCmdAbstract
{
    static ConsoleCmdBsSetConfig()
    {
        // Register this command when the class is first loaded
        BsCommandRegistry.RegisterCommand("bssetconfig", "Sets a configuration option and saves it to config file");

        // Initialize the config property registry
        BsConfigPropertyRegistry.InitializeProperties();
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            var paramList = string.Join(" ", _params);
#if DEBUG
            ModLogger.Info($"Executing {nameof(ConsoleCmdBsSetConfig)} with parameters: [{paramList}]");
#endif
            SetConfig(_params);
        }
        catch (Exception e)
        {
            ModLogger.Error($"Error in {nameof(ConsoleCmdBsSetConfig)}: {e.Message}", e);
        }
    }

    private void SetConfig(List<string> parameters)
    {
        if (parameters == null || parameters.Count < 2)
        {
            ShowUsage();
            return;
        }

        var propertyName = parameters[0].Trim();
        var propertyValue = parameters[1].Trim();

        // Handle special case where value might contain spaces (join remaining parameters)
        if (parameters.Count > 2)
        {
            propertyValue = string.Join(" ", parameters.Skip(1));
        }

        if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(propertyValue))
        {
            ModLogger.Info("Error: Property name and value cannot be empty.");
            ShowUsage();
            return;
        }

        // Find property (case-insensitive)
        var propertyInfo = BsConfigPropertyRegistry.FindProperty(propertyName);
        if (propertyInfo == null)
        {
            ModLogger.Info($"Error: Unknown property '{propertyName}'.");
            ShowAvailableProperties();
            return;
        }

        // Special handling for DEBUG-only properties
#if !DEBUG
        if (propertyInfo.SetValue == null)
        {
            ModLogger.Info($"Property '{propertyInfo.PropertyName}' only has an effect in DEBUG builds which only a developer would have.");
            return;
        }
#endif

        // Validate and set the property
        try
        {
            // Apply the new value to current config
            propertyInfo.SetValue(ModConfig.ClientConfig, propertyValue);

            // Validate the config after the change
            if (!BsConfigPropertyRegistry.ValidatePropertyChange(propertyInfo.PropertyName, propertyValue))
            {
                // Reload config to revert changes
                ReloadConfig();
                return;
            }

            // Save to file using ModConfig.SaveConfig()
            try
            {
                ModConfig.SaveConfig();
                ModLogger.Info($"Successfully set '{propertyInfo.PropertyName}' to '{propertyValue}' and saved to config file.");

                // Show current value for confirmation
                ShowCurrentValue(propertyInfo);
            }
            catch (Exception saveEx)
            {
                // Reload config to revert changes on save failure
                ReloadConfig();
                ModLogger.Info($"Failed to save config file: {saveEx.Message}. Config has been reloaded from file.");
            }
        }
        catch (ArgumentException ex)
        {
            // Reload config to revert any partial changes
            ReloadConfig();
            ModLogger.Info($"Error: Invalid value '{propertyValue}' for property '{propertyName}'. {ex.Message}");
            ModLogger.Info($"Expected type: {propertyInfo.Type}");
        }
        catch (Exception ex)
        {
            // Reload config to revert any partial changes
            ReloadConfig();
            ModLogger.Error($"Unexpected error setting config property: {ex.Message}", ex);
        }
    }

    private void ShowUsage()
    {
        ModLogger.Info("Usage: bssetconfig <property> <value>");
        ModLogger.Info("Example: bssetconfig range 50");
        ModLogger.Info("Example: bssetconfig pullFromDrones true");
        ModLogger.Info("");
        ShowAvailableProperties();
    }

    private void ShowAvailableProperties()
    {
        var allProperties = BsConfigPropertyRegistry.GetAllProperties();

        if (allProperties.Count == 0)
        {
            ModLogger.Info("No configuration properties are currently registered.");
            return;
        }

        // Find the longest property name for formatting
        int maxNameLength = allProperties.Max(p => p.PropertyName.Length);
        int maxTypeLength = allProperties.Max(p => p.Type.Length);

        // Use StringBuilder for efficient string building
        var sb = new StringBuilder();
        sb.AppendLine("Available properties:");

        foreach (var propertyInfo in allProperties)
        {
            var paddedName = propertyInfo.PropertyName.PadRight(maxNameLength);
            var paddedType = propertyInfo.Type.PadRight(maxTypeLength);

            var description = propertyInfo.Description;
#if !DEBUG
            if (propertyInfo.SetValue == null)
            {
                description += " (DEBUG build only)";
            }
#endif

            sb.AppendLine($"  {paddedName} ({paddedType}) - {description}");
        }

        sb.AppendLine();
        sb.AppendLine("Use 'bsshowconfig' to see current values.");

        // Output the complete formatted string in one call
        ModLogger.Info(sb.ToString().TrimEnd());
    }

    private void ShowCurrentValue(BsConfigPropertyRegistry.ConfigPropertyInfo propertyInfo)
    {
        var currentValue = BsConfigPropertyRegistry.GetCurrentPropertyValue(propertyInfo.PropertyName);
        ModLogger.Info($"Current value of '{propertyInfo.PropertyName}': {currentValue}");
    }

    private static void ReloadConfig()
    {
        try
        {
            ModConfig.LoadConfig(BeyondStorageMod.Context);
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Failed to reload config: {ex.Message}", ex);
        }
    }

    public override string[] getCommands()
    {
        return new[]
        {
            "bssetconfig"
        };
    }

    public override string getDescription()
    {
        return "Sets a configuration option and saves it to config file";
    }
}