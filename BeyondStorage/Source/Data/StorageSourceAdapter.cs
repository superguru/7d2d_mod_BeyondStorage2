using System;
using System.Runtime.CompilerServices;
using BeyondStorage.Scripts.Diagnostics;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

internal class StorageSourceAdapter<T> : IStorageSource where T : class
{
    private const int HASH_MULTIPLIER = 397;

    // Replace the private readonly field and explicit property with an auto-property
    public T StorageSource { get; }

    private readonly Type _storageSourceType;

    private readonly Func<T, T, bool> _equalsFunc;
    private readonly Func<T, ItemStack[]> _getPullableItemStacksFunc;
    private readonly Func<T, ItemStack[]> _getAllSlotsItemStacksFunc;
    private readonly Action<T> _markModifiedAction;
    private readonly Func<T, string> _getNameFunc;

    public StorageSourceAdapter(
        T storageSource,
        Func<T, T, bool> equalsFunc,
        Func<T, ItemStack[]> getPullableItemStacksFunc,
        Func<T, ItemStack[]> getAllSlotsItemStacksFunc,
        Action<T> markModifiedAction,
        Func<T, string> getNameFunc)
    {
        const string d_MethodName = nameof(StorageSourceAdapter<>);

        if (storageSource == null)
        {
            var error = $"{d_MethodName}: {nameof(storageSource)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(storageSource), error);
        }

        if (equalsFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(equalsFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(equalsFunc), error);
        }

        if (getPullableItemStacksFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getPullableItemStacksFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getPullableItemStacksFunc), error);
        }

        if (getAllSlotsItemStacksFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getAllSlotsItemStacksFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getAllSlotsItemStacksFunc), error);
        }

        if (markModifiedAction == null)
        {
            var error = $"{d_MethodName}: {nameof(markModifiedAction)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(markModifiedAction), error);
        }

        if (getNameFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getNameFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getNameFunc), error);
        }

        StorageSource = storageSource;

        _storageSourceType = storageSource.GetType();

        _equalsFunc = equalsFunc;
        _getPullableItemStacksFunc = getPullableItemStacksFunc;
        _getAllSlotsItemStacksFunc = getAllSlotsItemStacksFunc;
        _markModifiedAction = markModifiedAction;
        _getNameFunc = getNameFunc;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as IStorageSource);
    }

    public bool Equals(IStorageSource other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is StorageSourceAdapter<T> otherAdapter)
        {
            return _equalsFunc(StorageSource, otherAdapter.StorageSource);
        }

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = RuntimeHelpers.GetHashCode(StorageSource);
            hash = (hash * HASH_MULTIPLIER) ^ typeof(T).GetHashCode();
            return hash;
        }
    }

    private ItemStack[] GetSpecifiedItemStacks(string methodName, Func<T, ItemStack[]> getItemStacksFunc)
    {
        var sourceTypeAbbrev = TypeNames.GetAbbrev(_storageSourceType);

        try
        {
            var items = getItemStacksFunc(StorageSource);
            if (items == null)
            {
                ModLogger.DebugLog($"{methodName}({sourceTypeAbbrev}) | Returned null items, using empty array");
                return [];
            }
            return items;
        }
        catch (NullReferenceException ex)
        {
            ModLogger.DebugLog($"{methodName}({sourceTypeAbbrev}) | Null reference accessing items: {ex.Message}. Storage source may have been disposed.");
            return [];
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"{methodName}({sourceTypeAbbrev}) | Error getting items: {ex.Message}");
            return [];
        }
    }

    public ItemStack[] GetPullableItemStacks()
    {
        const string d_MethodName = nameof(GetPullableItemStacks);
        return GetSpecifiedItemStacks(d_MethodName, _getPullableItemStacksFunc);
    }

    public ItemStack[] GetAllSlotItemsStacks()
    {
        const string d_MethodName = nameof(GetAllSlotItemsStacks);
        return GetSpecifiedItemStacks(d_MethodName, _getAllSlotsItemStacksFunc);
    }

    public string GetName()
    {
        const string d_MethodName = nameof(GetName);
        const string UNKNOWN = "Unknown Storage";

        var sourceTypeAbbrev = TypeNames.GetAbbrev(_storageSourceType);
        try
        {
            return _getNameFunc(StorageSource) ?? UNKNOWN;
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Error getting name: {ex.Message}");
            return UNKNOWN;
        }
    }

    public Type GetSourceType()
    {
        return _storageSourceType;
    }

    public void MarkModified()
    {
        const string d_MethodName = nameof(MarkModified);
        var sourceTypeAbbrev = TypeNames.GetAbbrev(_storageSourceType);

        try
        {
            _markModifiedAction(StorageSource);
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Error marking source as modified: {ex.Message}");
        }
    }

    public override string ToString()
    {
        return $"{typeof(T).Name}: {StorageSource}";
    }
}
