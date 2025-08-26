using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Vehicle;

public static class VehicleRepair
{
    public static int VehicleRepairRemoveRemaining(ItemValue itemValue, int itemCount)
    {
        const string d_MethodName = nameof(VehicleRepairRemoveRemaining);
        const int DEFAULT_RETURN_VALUE = 0;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        // Remove repair kit from storage
        var countRemoved = context.RemoveRemaining(itemValue, itemCount);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Removed {countRemoved} {itemName} from storage");
#endif

        return countRemoved;
    }
}