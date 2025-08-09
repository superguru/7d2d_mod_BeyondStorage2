using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.PowerSource;

public static class PowerSourceRefuel
{
    public static int RefuelRemoveRemaining(ItemValue itemValue, int lastRemoved, int totalNeeded)
    {
        const string d_MethodName = nameof(RefuelRemoveRemaining);

        int DEFAULT_RETURN_VALUE = lastRemoved;

        if (totalNeeded <= 0 || lastRemoved >= totalNeeded)
        {
            return DEFAULT_RETURN_VALUE;
        }

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, config => config.EnableForGeneratorRefuel,
            out StorageContext context, out _, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        int amountToRemove = totalNeeded - lastRemoved;
        if (amountToRemove <= 0)
        {
            return DEFAULT_RETURN_VALUE;
        }

        int removed = context.RemoveRemaining(itemValue, amountToRemove);

        int result = lastRemoved + removed;

        if (removed > 0)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: item {itemName}; lastRemoved {lastRemoved}; totalNeeded {totalNeeded}; amountToRemove {amountToRemove}; removed {removed}; updated result {result}");
#endif
        }

        return result;
    }
}