using System;
using System.IO;
using System.Reflection;

namespace BeyondStorage.Scripts.Infrastructure;

internal static class ModPathManager
{
    private static string s_assemblyLocation = "";
    private static string s_mod_assembly_path = "";
    private static string s_configAssetPath = "Config";
    private static string s_assemblyVersion = "";

    internal static string GetConfigPath(bool create = false)
    {
        var result = GetAssetPath(s_configAssetPath, create);
        return result;
    }

    private static string GetAssemblyLocation()
    {
        if (string.IsNullOrEmpty(s_assemblyLocation))
        {
            s_assemblyLocation = Assembly.GetExecutingAssembly().Location ?? throw new InvalidOperationException("no assembly");
        }
        return s_assemblyLocation;
    }

    private static string GetModAssemblyPath()
    {
        if (string.IsNullOrEmpty(s_mod_assembly_path))
        {
            var assemblyLocation = GetAssemblyLocation();
            s_mod_assembly_path = Path.GetDirectoryName(assemblyLocation) ?? throw new InvalidOperationException("no path");
        }

        if (string.IsNullOrEmpty(s_mod_assembly_path))
        {
            throw new InvalidOperationException("Mod assembly path is null or empty.");
        }

        return s_mod_assembly_path;
    }

    internal static string GetAssemblyVersion()
    {
        if (string.IsNullOrEmpty(s_assemblyVersion))
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;

            if (version != null)
            {
                s_assemblyVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            }
            else
            {
                s_assemblyVersion = "0.0.0";  // This is kinda bad. Not sure how to handle this.
            }
        }

        return s_assemblyVersion;
    }

    private static string GetAssetPath(string assetname, bool create = false)
    {
        var result = Path.Combine(GetModAssemblyPath(), assetname);

        if (create && !Directory.Exists(result))
        {
            Directory.CreateDirectory(result);
        }
        return result;
    }
}