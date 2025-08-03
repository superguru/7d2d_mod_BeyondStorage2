using System;
using System.IO;
using System.Reflection;

namespace BeyondStorage.Scripts.Infrastructure;

internal static class ModPathManager
{
    internal static string s_configAssetPath = "Config";
    internal static string GetConfigPath(bool create = false)
    {
        var result = GetAssetPath(s_configAssetPath, create);

        return result;
    }

    private static string s_mod_assembly_path = "";
    private static string GetModAssemblyPath()
    {
        if (string.IsNullOrEmpty(s_mod_assembly_path))
        {
            s_mod_assembly_path = Assembly.GetExecutingAssembly().Location ?? throw new InvalidOperationException("no assembly");
            s_mod_assembly_path = Path.GetDirectoryName(s_mod_assembly_path) ?? throw new InvalidOperationException("no path");
        }

        if (string.IsNullOrEmpty(s_mod_assembly_path))
        {
            ModLogger.Error("Failed to get mod assembly path.");
            throw new InvalidOperationException("Mod assembly path is null or empty.");
        }
        ModLogger.DebugLog($"Mod assembly path: {s_mod_assembly_path}");

        return s_mod_assembly_path;
    }

    private static string GetAssetPath(string assetname, bool create = false)
    {
        var result = Path.Combine(GetModAssemblyPath(), assetname);
        ModLogger.DebugLog($"Asset path for asset [{assetname}] is {result}");

        if (create && !Directory.Exists(result))
        {
            Directory.CreateDirectory(result);
        }
        return result;
    }
}