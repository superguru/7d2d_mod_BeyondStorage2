using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;
internal class CurrencyCache
{
    private static readonly HashSet<int> s_currencyCache = [];

    private static void InitCurrencyCache()
    {
        const string d_MethodName = nameof(InitCurrencyCache);

        if (s_currencyCache.Count > 0)
        {
            return;
        }

        // Currently only one currency type is defined in the game
        ItemValue currencyItem = ItemClass.GetItem(TraderInfo.CurrencyItem);
        int type = currencyItem?.type ?? -1;

        s_currencyCache.Add(type); // add even if invalid, to avoid repeated intialisation

        if (type <= 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: Invalid currency item type, please check TraderInfo.CurrencyItem");
        }
        else
        {
            ModLogger.DebugLog($"{d_MethodName}: Initialized with currency item type {type}");
        }
    }

    public static bool IsCurrencyItem(int itemType)
    {
        InitCurrencyCache();
        return s_currencyCache.Contains(itemType);
    }

    public static bool IsCurrencyItem(ItemValue itemValue)
    {
        return IsCurrencyItem(itemValue?.type ?? -1);
    }

    public static bool IsCurrencyItem(ItemStack stack)
    {
        return IsCurrencyItem(stack?.itemValue);
    }

    public static bool IsCurrencyItem(XUiC_ItemStack xUiC_ItemStack)
    {
        return IsCurrencyItem(xUiC_ItemStack?.ItemStack);
    }
}
