using System;
using System.IO;
using System.Reflection;

namespace BeyondStorage.Scripts.Infrastructure;

internal static class ModPathManager
{
    private static string s_assemblyLocation = "";
    private static string s_mod_assembly_path = "";
    private static string s_assemblyVersion = "";

    /// <summary>
    /// Gets the path where config files should be stored (mod assembly directory as of v2.4.0)
    /// </summary>
    /// <param name="create">Whether to create the directory if it doesn't exist</param>
    /// <returns>Path to config directory</returns>
    internal static string GetConfigPath(bool create = false)
    {
        // As of v2.4.0, config files are stored in the mod assembly directory
        var result = GetModAssemblyPath();

        if (create && !Directory.Exists(result))
        {
            Directory.CreateDirectory(result);
        }

        return result;
    }

    /// <summary>
    /// Gets the legacy config path (Config subdirectory) for migration purposes
    /// </summary>
    /// <returns>Path to legacy config directory</returns>
    internal static string GetLegacyConfigPath()
    {
        return Path.Combine(GetModAssemblyPath(), "Config");
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

    /// <summary>
    /// Gets an asset path relative to the mod assembly directory
    /// </summary>
    /// <param name="assetname">Name of the asset subdirectory</param>
    /// <param name="create">Whether to create the directory if it doesn't exist</param>
    /// <returns>Full path to the asset directory</returns>
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