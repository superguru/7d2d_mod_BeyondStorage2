using System;
using System.IO;
using System.Reflection;

namespace BeyondStorage.Scripts.Utils;

internal static class FileUtil
{
    internal static string ConfigAssetPath = "Config";
    internal static string GetConfigPath(bool create = false)
    {
        var result = GetAssetPath(ConfigAssetPath, create);

        return result;
    }

    private static string s_mod_assembly_path = "";
    private static string GetModAssemblyPath(bool create = false)
    {
        if (string.IsNullOrEmpty(s_mod_assembly_path))
        {
            s_mod_assembly_path = Assembly.GetExecutingAssembly().Location ?? throw new InvalidOperationException("no assembly");
            s_mod_assembly_path = Path.GetDirectoryName(s_mod_assembly_path) ?? throw new InvalidOperationException("no path");  
        }

        if (string.IsNullOrEmpty(s_mod_assembly_path))
        {
            LogUtil.Error("Failed to get mod assembly path.");
            throw new InvalidOperationException("Mod assembly path is null or empty.");
        }
        LogUtil.DebugLog($"Mod assembly path: {s_mod_assembly_path}");

        return s_mod_assembly_path;
    }

    private static string GetAssetPath(string assetname, bool create = false)
    {
        var result = Path.Combine(GetModAssemblyPath(), assetname);
        LogUtil.DebugLog($"Asset path for asset [{assetname}] is {result}");

        if (create && !Directory.Exists(result))
        {
            Directory.CreateDirectory(result);
        }
        return result;
    }
}