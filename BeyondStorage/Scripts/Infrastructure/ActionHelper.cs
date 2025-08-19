using System;
using System.Threading.Tasks;

namespace BeyondStorage.Scripts.Infrastructure;
internal static class ActionHelper
{
    internal static void SetTimeout(Action action, TimeSpan delay)
    {
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay);
                action();
            }
            catch (Exception ex)
            {
                ModLogger.DebugLog($"SetTimeout: Error executing action after delay: {ex.Message}", ex);
            }
        });
    }

}
