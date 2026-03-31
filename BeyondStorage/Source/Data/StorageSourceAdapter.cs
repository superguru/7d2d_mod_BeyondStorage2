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
    private readonly Func<T, ItemStack[]> _getItemStacksFunc;
    private readonly Action<T> _markModifiedAction;
    private readonly Func<T, string> _getNameFunc;

    public StorageSourceAdapter(
        T storageSource,
        Func<T, T, bool> equalsFunc,
        Func<T, ItemStack[]> getItemStacksFunc,
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

        if (getItemStacksFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getItemStacksFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getItemStacksFunc), error);
        }

        if (markModifiedAction == null)
        {
            var error = $"{d_MethodName}: {nameof(markModifiedAction)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(markModifiedAction), error);
        }

        StorageSource = storageSource;

        _storageSourceType = storageSource.GetType();
        var sourceTypeAbbrev = TypeNames.GetAbbrev(_storageSourceType);

        _equalsFunc = equalsFunc;
        _getItemStacksFunc = getItemStacksFunc;
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
        if (StorageSource == null)
        {
            return typeof(T).GetHashCode();
        }

        unchecked
        {
            int hash = RuntimeHelpers.GetHashCode(StorageSource);
            hash = (hash * HASH_MULTIPLIER) ^ typeof(T).GetHashCode();
            return hash;
        }
    }

    public ItemStack[] GetItemStacks()
    {
        const string d_MethodName = nameof(GetItemStacks);
        var sourceTypeAbbrev = TypeNames.GetAbbrev(_storageSourceType);

        try
        {
            var items = _getItemStacksFunc(StorageSource);
            if (items == null)
            {
                ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Returned null items, using empty array");
                return [];
            }
            return items;
        }
        catch (NullReferenceException ex)
        {
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Null reference accessing items: {ex.Message}. Storage source may have been disposed.");
            return [];
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Error getting items: {ex.Message}");
            return [];
        }
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
