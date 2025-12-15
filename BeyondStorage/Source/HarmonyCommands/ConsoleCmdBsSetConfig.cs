using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
#if DEBUG
            var paramList = string.Join(" ", _params);
            ModLogger.DebugLog($"Executing {nameof(ConsoleCmdBsSetConfig)} with parameters: [{paramList}]");
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
        if (!ValidateParameters(parameters, out string propertyName, out string propertyValue))
        {
            return;
        }

        var propertyInfo = FindAndValidateProperty(propertyName);
        if (propertyInfo == null)
        {
            return;
        }

        ApplyPropertyChange(propertyInfo, propertyValue);
    }

    /// <summary>
    /// Validates input parameters and extracts property name and value
    /// </summary>
    /// <param name="parameters">Input parameters</param>
    /// <param name="propertyName">Extracted property name</param>
    /// <param name="propertyValue">Extracted property value</param>
    /// <returns>True if parameters are valid</returns>
    private bool ValidateParameters(List<string> parameters, out string propertyName, out string propertyValue)
    {
        propertyName = null;
        propertyValue = null;

        if (parameters == null || parameters.Count < 2)
        {
            ShowUsage();
            return false;
        }

        propertyName = parameters[0].Trim();
        propertyValue = parameters[1].Trim();

        // Handle special case where value might contain spaces (join remaining parameters)
        if (parameters.Count > 2)
        {
            propertyValue = string.Join(" ", parameters.Skip(1));
        }

        if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(propertyValue))
        {
            ModLogger.Info("Error: Property name and value cannot be empty.");
            ShowUsage();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Finds the property and validates it can be set
    /// </summary>
    /// <param name="propertyName">Name of the property to find</param>
    /// <returns>Property info if found and valid, null otherwise</returns>
    private BsConfigPropertyRegistry.ConfigPropertyInfo FindAndValidateProperty(string propertyName)
    {
        var propertyInfo = BsConfigPropertyRegistry.FindProperty(propertyName);
        if (propertyInfo == null)
        {
            ModLogger.Info($"Error: Unknown property '{propertyName}'.");
            ShowAvailableProperties();
            return null;
        }

#if !DEBUG
        if (propertyInfo.SetValue == null)
        {
            ModLogger.Info($"Property '{propertyInfo.PropertyName}' only has an effect in DEBUG builds which only a developer would have.");
            return null;
        }
#endif

        return propertyInfo;
    }

    /// <summary>
    /// Applies the property change and handles all related operations
    /// </summary>
    /// <param name="propertyInfo">Property to change</param>
    /// <param name="propertyValue">New value to set</param>
    private void ApplyPropertyChange(BsConfigPropertyRegistry.ConfigPropertyInfo propertyInfo, string propertyValue)
    {
        try
        {
            SetPropertyValue(propertyInfo, propertyValue);

            if (!ValidatePropertyChange(propertyInfo, propertyValue))
            {
                return; // Validation failed, config already reloaded
            }

            SaveConfigAndConfirm(propertyInfo, propertyValue);
        }
        catch (ArgumentException ex)
        {
            HandlePropertySetError(propertyInfo.PropertyName, propertyValue, ex.Message, propertyInfo.Type);
        }
        catch (Exception ex)
        {
            HandleUnexpectedError(ex);
        }
    }

    /// <summary>
    /// Sets the property value on the current config
    /// </summary>
    /// <param name="propertyInfo">Property to set</param>
    /// <param name="propertyValue">Value to set</param>
    private static void SetPropertyValue(BsConfigPropertyRegistry.ConfigPropertyInfo propertyInfo, string propertyValue)
    {
        propertyInfo.SetValue(ModConfig.ClientConfig, propertyValue);
    }

    /// <summary>
    /// Validates the property change
    /// </summary>
    /// <param name="propertyInfo">Property that was changed</param>
    /// <param name="propertyValue">Value that was set</param>
    /// <returns>True if validation passed</returns>
    private bool ValidatePropertyChange(BsConfigPropertyRegistry.ConfigPropertyInfo propertyInfo, string propertyValue)
    {
        if (!BsConfigPropertyRegistry.ValidatePropertyChange(propertyInfo.PropertyName, propertyValue))
        {
            ConfigReloadHelper.ReloadConfig();
            return false;
        }
        return true;
    }

    /// <summary>
    /// Saves the config and shows confirmation
    /// </summary>
    /// <param name="propertyInfo">Property that was changed</param>
    /// <param name="propertyValue">Value that was set</param>
    private void SaveConfigAndConfirm(BsConfigPropertyRegistry.ConfigPropertyInfo propertyInfo, string propertyValue)
    {
        try
        {
            ModConfig.SaveConfig();
            ModLogger.Info($"Successfully set '{propertyInfo.PropertyName}' to '{propertyValue}' and saved to config file.");
            ShowCurrentValue(propertyInfo);
        }
        catch (Exception saveEx)
        {
            ConfigReloadHelper.ReloadConfig();
            ModLogger.Info($"Failed to save config file: {saveEx.Message}. Config has been reloaded from file.");
        }
    }

    /// <summary>
    /// Handles property setting errors with appropriate error messages
    /// </summary>
    /// <param name="propertyName">Name of the property</param>
    /// <param name="propertyValue">Value that failed to set</param>
    /// <param name="errorMessage">Error message from the exception</param>
    /// <param name="expectedType">Expected type for the property</param>
    private static void HandlePropertySetError(string propertyName, string propertyValue, string errorMessage, string expectedType)
    {
        ConfigReloadHelper.ReloadConfig();
        ModLogger.Info($"Error: Invalid value '{propertyValue}' for property '{propertyName}'. {errorMessage}");
        ModLogger.Info($"Expected type: {expectedType}");
    }

    /// <summary>
    /// Handles unexpected errors during property setting
    /// </summary>
    /// <param name="ex">The unexpected exception</param>
    private static void HandleUnexpectedError(Exception ex)
    {
        ConfigReloadHelper.ReloadConfig();
        ModLogger.Error($"Unexpected error setting config property: {ex.Message}", ex);
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

        // Calculate approximate capacity for StringBuilder to reduce allocations
        int estimatedCapacity = 50 + (allProperties.Count * (maxNameLength + maxTypeLength + 100)); // Header + (properties * estimated line length)

        // Use StringBuilder for efficient string building with pre-calculated capacity
        var sb = new StringBuilder(estimatedCapacity);
        sb.AppendLine("Available properties:");

        foreach (var propertyInfo in allProperties)
        {
            var paddedName = propertyInfo.PropertyName.PadRight(maxNameLength);
            var paddedType = propertyInfo.Type.PadRight(maxTypeLength);

            var description = propertyInfo.Description;
#if !DEBUG
            if (propertyInfo.SetValue == null)
            {
                description = $"{description} (DEBUG build only)";
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