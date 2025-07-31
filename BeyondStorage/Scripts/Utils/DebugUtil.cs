using System;

namespace BeyondStorage.Scripts.Utils;

public static class DebugUtil
{
    public static string GetStackTrace()
    {
        return Environment.StackTrace;
    }
}
