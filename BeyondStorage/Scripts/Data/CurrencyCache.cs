using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;
internal class CurrencyCache
{
    private static readonly Dictionary<int, string> s_currencyCache = [];
    private static ItemStack s_emptyCurrencyStack = null;

    private static void InitCurrencyCache()
    {
        const string d_MethodName = nameof(InitCurrencyCache);

        if (s_currencyCache.Count > 0)
        {
            return;
        }

        // Currently only one currency itemType is defined in the game
        ItemValue currencyItem = ItemClass.GetItem(TraderInfo.CurrencyItem);
        int itemType = currencyItem?.type ?? -1;

        s_currencyCache[itemType] = currencyItem?.ItemClass?.GetItemName(); // add even if invalid, to avoid repeated intialisation

        if (itemType <= 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: Invalid currency item itemType, please check TraderInfo.CurrencyItem");
        }
        else
        {
            ModLogger.DebugLog($"{d_MethodName}: Initialized with currency item itemType {itemType}");
        }
    }

    public static bool IsCurrencyItem(int itemType)
    {
        InitCurrencyCache();
        return s_currencyCache.ContainsKey(itemType);
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

    public static ItemStack GetEmptyCurrencyStack()
    {
        InitEmptyCurrencyStack();
        return s_emptyCurrencyStack;
    }

    private static void InitEmptyCurrencyStack()
    {
        const string d_MethodName = nameof(InitEmptyCurrencyStack);

        if (s_emptyCurrencyStack != null)
        {
            return;
        }

        InitCurrencyCache();
        if (s_currencyCache.Count == 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: No valid trader currency item itemType defined");
            return;
        }

        // Use the cached item name to get the ItemClass
        var currency = s_currencyCache.First();
        var itemName = currency.Value;

        if (string.IsNullOrEmpty(itemName))
        {
            ModLogger.DebugLog($"{d_MethodName}: Cached currency item name is null or empty for itemType {currency.Key}");
            return;
        }

        var itemClass = ItemClass.GetItem(itemName);
        if (itemClass == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Currency item class not found for name '{itemName}' (itemType {currency.Key})");
            return;
        }

        s_emptyCurrencyStack = new ItemStack(itemClass, 0);
    }
}
