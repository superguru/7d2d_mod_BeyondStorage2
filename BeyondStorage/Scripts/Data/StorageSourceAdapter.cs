using System;
using System.Runtime.CompilerServices;
using BeyondStorage.Scripts.Diagnostics;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

internal class StorageSourceAdapter<T> : IStorageSource where T : class
{
    // Replace the private readonly field and explicit property with an auto-property
    public T StorageSource { get; }

    private readonly Type _storageSourceType;

    private readonly Func<T, T, bool> _equalsFunc;
    private readonly Func<T, ItemStack[]> _getItemStacksFunc;
    private readonly Action<T> _markModifiedAction;

    public StorageSourceAdapter(
        T storageSource,
        Func<T, T, bool> equalsFunc,
        Func<T, ItemStack[]> getItemStacksFunc,
        Action<T> markModifiedAction)
    {
        const string d_MethodName = nameof(StorageSourceAdapter<T>);

        if (storageSource == null)
        {
            var error = $"{d_MethodName}: {nameof(storageSource)} cannot be null";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(storageSource), error);
        }

        if (equalsFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(equalsFunc)} cannot be null";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(equalsFunc), error);
        }

        if (getItemStacksFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getItemStacksFunc)} cannot be null";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(getItemStacksFunc), error);
        }

        if (markModifiedAction == null)
        {
            var error = $"{d_MethodName}: {nameof(markModifiedAction)} cannot be null";
            ModLogger.Error(error);
            throw new ArgumentNullException(nameof(markModifiedAction), error);
        }

        StorageSource = storageSource;

        _storageSourceType = storageSource.GetType();
        var sourceTypeAbbrev = NameLookups.GetAbbrev(_storageSourceType);

        _equalsFunc = equalsFunc;
        _getItemStacksFunc = getItemStacksFunc;
        _markModifiedAction = markModifiedAction;
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
            hash = (hash * 397) ^ typeof(T).GetHashCode();
            return hash;
        }
    }

    public ItemStack[] GetItemStacks()
    {
        const string d_MethodName = nameof(GetItemStacks);
        var sourceTypeAbbrev = NameLookups.GetAbbrev(_storageSourceType);

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

    public Type GetSourceType()
    {
        return _storageSourceType;
    }

    public void MarkModified()
    {
        const string d_MethodName = nameof(MarkModified);
        var sourceTypeAbbrev = NameLookups.GetAbbrev(_storageSourceType);

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
