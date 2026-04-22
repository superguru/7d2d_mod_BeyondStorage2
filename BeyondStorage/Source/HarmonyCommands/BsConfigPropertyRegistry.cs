using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeyondStorage.Source.Configuration;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.HarmonyCommands;

internal static class BsConfigPropertyRegistry
{
    private static readonly Dictionary<string, ConfigPropertyInfo> s_registeredProperties = [];
    private static bool s_isInitialized = false;
    private static readonly object s_lockObject = new();

    /// <summary>
    /// Initializes and registers all available configuration properties
    /// </summary>
    public static void InitializeProperties()
    {
        lock (s_lockObject)
        {
            if (s_isInitialized)
            {
                return;
            }

            RegisterConfigurationProperties();
            s_isInitialized = true;

            ModLogger.DebugLog($"Initialized {s_registeredProperties.Count} config properties");
        }
    }

    /// <summary>
    /// Registers all configuration properties with their metadata and setters
    /// </summary>
    private static void RegisterConfigurationProperties()
    {
        // Register all available properties
        RegisterProperty("pullFromDrones", "bool", "Pull items from nearby drones",
            (config, value) => config.pullFromDrones = ParseBool(value));

        RegisterProperty("pullFromCollectors", "bool", "Pull items from nearby collectors (e.g. dew collectors, apiaries)",
            (config, value) => config.pullFromCollectors = ParseBool(value));

        RegisterProperty("pullFromWorkstationOutputs", "bool", "Pull items from nearby workstation output stacks",
            (config, value) => config.pullFromWorkstationOutputs = ParseBool(value));

        RegisterProperty("pullFromVehicleStorage", "bool", "Pull items from nearby vehicle storages",
            (config, value) => config.pullFromVehicleStorage = ParseBool(value));

        RegisterProperty("isDebug", "bool", "Enable additional logging",
            (config, value) => config.isDebug = ParseBool(value));
    }

    /// <summary>
    /// Registers a single configuration property
    /// </summary>
    /// <param name="propertyName">The property name (case-sensitive for storage)</param>
    /// <param name="type">The property type description</param>
    /// <param name="description">The property description</param>
    /// <param name="setValue">Action to set the property value, null for DEBUG-only properties</param>
    private static void RegisterProperty(string propertyName, string type, string description, Action<BsConfig, string> setValue)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            ModLogger.Error("Cannot register config property with null or empty name");
            return;
        }

        var propertyInfo = new ConfigPropertyInfo(propertyName, type, description, setValue);
        s_registeredProperties[propertyName] = propertyInfo;

        ModLogger.DebugLog($"Registered config property: {propertyName} ({type})");
    }

    /// <summary>
    /// Finds a property by name (case-insensitive lookup)
    /// </summary>
    /// <param name="propertyName">The property name to find</param>
    /// <returns>ConfigPropertyInfo if found, null otherwise</returns>
    public static ConfigPropertyInfo FindProperty(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        EnsureInitialized();

        // Case-insensitive property lookup
        foreach (var kvp in s_registeredProperties)
        {
            if (string.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets all registered properties sorted by name (case-insensitive)
    /// </summary>
    /// <returns>List of all registered config properties</returns>
    public static List<ConfigPropertyInfo> GetAllProperties()
    {
        EnsureInitialized();

        return [.. s_registeredProperties.Values.OrderBy(p => p.PropertyName, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Gets the current value of a configuration property as a string
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <returns>Current property value as string, or "Unknown" if not found</returns>
    public static string GetCurrentPropertyValue(string propertyName)
    {
        var config = ModConfig.ClientConfig;
        return propertyName switch
        {
            "pullFromDrones" => config.pullFromDrones.ToString(),
            "pullFromCollectors" => config.pullFromCollectors.ToString(),
            "pullFromWorkstationOutputs" => config.pullFromWorkstationOutputs.ToString(),
            "pullFromVehicleStorage" => config.pullFromVehicleStorage.ToString(),
            "isDebug" => config.isDebug.ToString(),
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Validates a configuration property change
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <param name="value">The new value</param>
    /// <returns>True if the change is valid, false otherwise</returns>
    public static bool ValidatePropertyChange(string propertyName, string value)
    {
        return true;
    }

    /// <summary>
    /// Gets the number of registered properties
    /// </summary>
    /// <returns>Number of registered config properties</returns>
    public static int GetRegisteredPropertyCount()
    {
        EnsureInitialized();
        return s_registeredProperties.Count;
    }

    /// <summary>
    /// Checks if a property is registered
    /// </summary>
    /// <param name="propertyName">The property name to check</param>
    /// <returns>True if the property is registered</returns>
    public static bool IsPropertyRegistered(string propertyName)
    {
        return FindProperty(propertyName) != null;
    }

    /// <summary>
    /// Ensures the registry is initialized
    /// </summary>
    private static void EnsureInitialized()
    {
        if (!s_isInitialized)
        {
            InitializeProperties();
        }
    }

    /// <summary>
    /// Parses a string value to float with proper error handling
    /// </summary>
    /// <param name="value">The string value to parse</param>
    /// <returns>Parsed float value</returns>
    /// <exception cref="ArgumentException">Thrown when value cannot be parsed</exception>
    private static float ParseFloat(string value)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"'{value}' is not a valid decimal number.");
        }
        return result;
    }

    /// <summary>
    /// Parses a string value to bool with flexible input handling
    /// </summary>
    /// <param name="value">The string value to parse</param>
    /// <returns>Parsed boolean value</returns>
    /// <exception cref="ArgumentException">Thrown when value cannot be parsed</exception>
    private static bool ParseBool(string value)
    {
        var lowerValue = value.ToLower();
        return lowerValue switch
        {
            "true" or "1" or "yes" or "on" or "enabled" => true,
            "false" or "0" or "no" or "off" or "disabled" => false,
            _ => throw new ArgumentException($"'{value}' is not a valid boolean value. Use: true/false, 1/0, yes/no, on/off, enabled/disabled.")
        };
    }

    /// <summary>
    /// Clears all registered properties (mainly for testing purposes)
    /// </summary>
    public static void Clear()
    {
        lock (s_lockObject)
        {
            s_registeredProperties.Clear();
            s_isInitialized = false;
            ModLogger.DebugLog("Config property registry cleared");
        }
    }

    /// <summary>
    /// Information about a registered configuration property
    /// </summary>
    public class ConfigPropertyInfo
    {
        public string PropertyName { get; }
        public string Type { get; }
        public string Description { get; }
        public Action<BsConfig, string> SetValue { get; }

        public ConfigPropertyInfo(string propertyName, string type, string description, Action<BsConfig, string> setValue)
        {
            PropertyName = propertyName;
            Type = type;
            Description = description;
            SetValue = setValue;
        }
    }
}