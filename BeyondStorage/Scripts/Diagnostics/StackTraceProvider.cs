using System;

namespace BeyondStorage.Scripts.Diagnostics;

public static class StackTraceProvider
{
    public static string GetStackTrace()
    {
        return Environment.StackTrace;
    }
}
