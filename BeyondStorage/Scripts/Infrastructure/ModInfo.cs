using System;

namespace BeyondStorage.Scripts.Infrastructure;

public static class ModInfo
{
    internal const string ModName = "[BeyondStorage2]";

    private static string s_version = "";
    public static string Version
    {
        get
        {
            if (string.IsNullOrEmpty(s_version))
            {
                try
                {
                    s_version = ModPathManager.GetAssemblyVersion();
                }
                catch (Exception)
                {
                    // Fallback to just ModName if version retrieval fails
                    s_version = "0.0.0";
                }
            }
            return s_version;
        }
    }
}
