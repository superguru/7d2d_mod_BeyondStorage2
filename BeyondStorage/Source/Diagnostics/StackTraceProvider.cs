using System;

namespace BeyondStorage.Source.Diagnostics;

public static class StackTraceProvider
{
    public static string AppendStackTrace(string error, Exception e = null)
    {
#if DEBUG
        var stackTrace = e == null ? $"SYS_STACK{Environment.StackTrace}" : $"EX_STACK{e.StackTrace}";
        error = $"{error}:\n{stackTrace}";
#endif
        return error;
    }
}
