using System;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

internal class StorageSourceAdapter<T> : IStorageSource where T : class
{
    private readonly T _storageSource;
    private readonly StorageSourceItemDataStore _dataStore;
    private readonly Action<T> _markModifiedAction;
    private readonly Func<T, ItemStack[]> _getItemsFunc;
    private readonly Func<T, T, bool> _equalsFunc;

    public StorageSourceAdapter(T storageSource, StorageSourceItemDataStore dataStore, Action<T> markModifiedAction, Func<T, ItemStack[]> getItemsFunc, Func<T, T, bool> equalsFunc)
    {
        _storageSource = storageSource ?? throw new ArgumentNullException(nameof(storageSource));
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _markModifiedAction = markModifiedAction ?? throw new ArgumentNullException(nameof(markModifiedAction));
        _getItemsFunc = getItemsFunc ?? throw new ArgumentNullException(nameof(getItemsFunc));
        _equalsFunc = equalsFunc ?? throw new ArgumentNullException(nameof(equalsFunc));
    }

    public T StorageSource => _storageSource;

    public StorageSourceItemDataStore DataStore => _dataStore;

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

        // If the other is also a StorageSourceAdapter<T>, use the custom equals function
        if (other is StorageSourceAdapter<T> otherAdapter)
        {
            return _equalsFunc(_storageSource, otherAdapter._storageSource);
        }

        return false;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as IStorageSource);
    }

    public override int GetHashCode()
    {
        return _storageSource?.GetHashCode() ?? 0;
    }

    public ItemStack[] GetItems()
    {
        try
        {
            return _getItemsFunc(_storageSource) ?? Array.Empty<ItemStack>();
        }
        catch (Exception ex)
        {
            ModLogger.Error($"Error getting items from storage source of type {typeof(T).Name}: {ex.Message}");
            return [];
        }
    }

    public void MarkModified()
    {
        _markModifiedAction(_storageSource);
    }
}
