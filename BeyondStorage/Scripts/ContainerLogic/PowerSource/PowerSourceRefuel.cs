using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.PowerSource;

public static class PowerSourceRefuel
{
    public static int RefuelRemoveRemaining(ItemValue itemValue, int lastRemoved, int totalNeeded)
    {
        const string d_method_name = "PowerSourceRefuel.RefuelRemoveRemaining";

        if (totalNeeded <= 0)
        {
            return 0;
        }

        if (lastRemoved >= totalNeeded)
        {
            return lastRemoved;
        }

        if (!ModConfig.EnableForGeneratorRefuel())
        {
            return lastRemoved;
        }

        int amountToRemove = totalNeeded - lastRemoved;
        if (amountToRemove <= 0)
        {
            return lastRemoved;
        }

        int removed = ContainerUtils.RemoveRemaining(itemValue, amountToRemove);

        int result = lastRemoved + removed;

        if (LogUtil.IsDebug() && removed > 0)
        {
            LogUtil.DebugLog($"{d_method_name} - item {itemValue.ItemClass.GetItemName()}; lastRemoved {lastRemoved}; totalNeeded {totalNeeded}; amountToRemove {amountToRemove}; removed {removed}; updated result {result}");
        }

        return result;
    }
}