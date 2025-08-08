using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Reload;

[HarmonyPatch(typeof(TileEntityPowerSource))]
public class Find_Power_Source_Patches
{
#if DEBUG
    [HarmonyDebug]
#endif

    [HarmonyPostfix]
    [HarmonyPatch(nameof(TileEntityPowerSource.MaxFuel), MethodType.Getter)]
    public static void TileEntityPowerSource_MaxFuel_Postfix(TileEntityPowerSource __instance, ref ushort __result)
    {
        const string d_MethodName = nameof(TileEntityPowerSource_MaxFuel_Postfix);

        ModLogger.DebugLog($"{d_MethodName}: called with instance {__instance} and result {__result}");
    }
}