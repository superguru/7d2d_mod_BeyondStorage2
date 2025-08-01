﻿using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.PowerSource;

public static class PowerSourceRefuel
{
    public static int RefuelRemoveRemaining(ItemValue itemValue, int lastRemoved, int totalNeeded)
    {
        const string d_method_name = "RefuelRemoveRemaining";
        var itemName = itemValue.ItemClass.GetItemName();

        if (totalNeeded <= 0)
        {
            ModLogger.DebugLog($"{d_method_name} - item {itemName}; totalNeeded {totalNeeded} <= 0, returning early"); // TODO: Remove once done debugging
            return 0;
        }

        if (lastRemoved >= totalNeeded)
        {
            ModLogger.DebugLog($"{d_method_name} - item {itemName}; lastRemoved {lastRemoved} >= totalNeeded {totalNeeded}, returning early"); // TODO: Remove once done debugging
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

        var context = StorageContextFactory.Create(d_method_name);
        int removed = context?.RemoveRemaining(itemValue, amountToRemove) ?? 0;

        int result = lastRemoved + removed;

        if (removed > 0)
        {
            ModLogger.DebugLog($"{d_method_name} - item {itemName}; lastRemoved {lastRemoved}; totalNeeded {totalNeeded}; amountToRemove {amountToRemove}; removed {removed}; updated result {result}");
        }

        return result;
    }
}