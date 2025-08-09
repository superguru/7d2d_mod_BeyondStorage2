using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Infrastructure;

/// <summary>
/// Provides common validation methods used across multiple game feature classes.
/// Centralizes validation logic to ensure consistency and reduce code duplication.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates ItemValue and extracts ItemClass and item name.
    /// </summary>
    /// <param name="itemValue">The ItemValue to validate</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="itemClass">Output parameter for the validated ItemClass</param>
    /// <param name="itemName">Output parameter for the item name</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public static bool ValidateItemValue(ItemValue itemValue, string methodName, out ItemClass itemClass, out string itemName)
    {
        itemClass = null;
        itemName = null;

        if (itemValue == null || itemValue.IsEmpty())
        {
            ModLogger.DebugLog($"{methodName}: itemValue is null or empty");
            return false;
        }

        itemClass = itemValue.ItemClass;
        if (itemClass == null)
        {
            ModLogger.DebugLog($"{methodName}: itemClass is null");
            return false;
        }

        itemName = itemClass.GetItemName();
        if (string.IsNullOrEmpty(itemName))
        {
            ModLogger.DebugLog($"{methodName}: itemName is null or empty");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates ItemValue and extracts just the item name (simplified version).
    /// </summary>
    /// <param name="itemValue">The ItemValue to validate</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="itemName">Output parameter for the item name</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public static bool ValidateItemValue(ItemValue itemValue, string methodName, out string itemName)
    {
        return ValidateItemValue(itemValue, methodName, out _, out itemName);
    }

    /// <summary>
    /// Creates and validates a StorageContext.
    /// </summary>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="context">Output parameter for the validated StorageContext</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public static bool ValidateStorageContext(string methodName, out StorageContext context)
    {
        context = StorageContextFactory.Create(methodName);
        return StorageContextFactory.EnsureValidContext(context, methodName);
    }

    /// <summary>
    /// Creates and validates a StorageContext with a specific feature check.
    /// </summary>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="featureChecker">Function to check if the feature is enabled</param>
    /// <param name="context">Output parameter for the validated StorageContext</param>
    /// <returns>True if validation passes and feature is enabled, false otherwise</returns>
    public static bool ValidateStorageContextWithFeature(string methodName, System.Func<ConfigSnapshot, bool> featureChecker, out StorageContext context)
    {
        if (!ValidateStorageContext(methodName, out context))
        {
            return false;
        }

        if (!featureChecker(context.Config))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates both ItemValue and StorageContext with feature check in one call.
    /// </summary>
    /// <param name="itemValue">The ItemValue to validate</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="featureChecker">Function to check if the feature is enabled</param>
    /// <param name="context">Output parameter for the validated StorageContext</param>
    /// <param name="itemClass">Output parameter for the validated ItemClass</param>
    /// <param name="itemName">Output parameter for the item name</param>
    /// <returns>True if all validation passes, false otherwise</returns>
    public static bool ValidateItemAndContext(ItemValue itemValue, string methodName, System.Func<ConfigSnapshot, bool> featureChecker,
        out StorageContext context, out ItemClass itemClass, out string itemName)
    {
        context = null;
        itemClass = null;
        itemName = null;

        if (!ValidateItemValue(itemValue, methodName, out itemClass, out itemName))
        {
            return false;
        }

        if (!ValidateStorageContextWithFeature(methodName, featureChecker, out context))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates both ItemValue and StorageContext (simplified version without feature check).
    /// </summary>
    /// <param name="itemValue">The ItemValue to validate</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="context">Output parameter for the validated StorageContext</param>
    /// <param name="itemName">Output parameter for the item name</param>
    /// <returns>True if all validation passes, false otherwise</returns>
    public static bool ValidateItemAndContext(ItemValue itemValue, string methodName,
        out StorageContext context, out string itemName)
    {
        return ValidateItemAndContext(itemValue, methodName, out context, out _, out itemName);
    }

    /// <summary>
    /// Validates both ItemValue and StorageContext (simplified version without feature check).
    /// </summary>
    /// <param name="itemValue">The ItemValue to validate</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <param name="context">Output parameter for the validated StorageContext</param>
    /// <param name="itemClass">Output parameter for the validated ItemClass</param>
    /// <param name="itemName">Output parameter for the item name</param>
    /// <returns>True if all validation passes, false otherwise</returns>
    public static bool ValidateItemAndContext(ItemValue itemValue, string methodName,
        out StorageContext context, out ItemClass itemClass, out string itemName)
    {
        context = null;
        itemClass = null;
        itemName = null;

        if (!ValidateItemValue(itemValue, methodName, out itemClass, out itemName))
        {
            return false;
        }

        if (!ValidateStorageContext(methodName, out context))
        {
            return false;
        }

        return true;
    }
}