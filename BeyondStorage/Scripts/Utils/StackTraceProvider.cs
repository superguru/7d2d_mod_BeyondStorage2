using System;

namespace BeyondStorage.Scripts.Utils;

public static class StackTraceProvider
{
    public static string GetStackTrace()
    {
        return Environment.StackTrace;
    }
}
