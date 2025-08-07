using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.PowerSource;

public static class PowerSourceRefuel
{
    public static int RefuelRemoveRemaining(ItemValue itemValue, int lastRemoved, int totalNeeded)
    {
        const string d_MethodName = "RefuelRemoveRemaining";
        var itemName = itemValue.ItemClass.GetItemName();

        if (itemValue == null)
        {
            ModLogger.Warning($"{d_MethodName}: itemValue is null, returning lastRemoved {lastRemoved}");
            return lastRemoved;
        }

        if (totalNeeded <= 0)
        {
            return lastRemoved;
        }

        if (lastRemoved >= totalNeeded)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: item {itemName}; lastRemoved {lastRemoved} >= totalNeeded {totalNeeded}, returning early"); // TODO: ClearStacksForFilter once done debugging
#endif
            return lastRemoved;
        }

        int amountToRemove = totalNeeded - lastRemoved;
        if (amountToRemove <= 0)
        {
            return lastRemoved;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        if (!context.Config.EnableForGeneratorRefuel)
        {
            return lastRemoved;
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