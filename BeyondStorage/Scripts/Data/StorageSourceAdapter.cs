using System;

namespace BeyondStorage.Scripts.Data;

internal class StorageSourceAdapter<T> : IStorageSource where T : class
{
    private readonly T _storageSource;
    private readonly StorageSourceItemDataStore _dataStore;
    private readonly Action<T> _markModifiedAction;
    private readonly Func<T, ItemStack[]> _getItemsFunc;

    public StorageSourceAdapter(T storageSource, StorageSourceItemDataStore dataStore, Action<T> markModifiedAction, Func<T, ItemStack[]> getItemsFunc)
    {
        _storageSource = storageSource ?? throw new ArgumentNullException(nameof(storageSource));
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _markModifiedAction = markModifiedAction ?? throw new ArgumentNullException(nameof(markModifiedAction));
        _getItemsFunc = getItemsFunc ?? throw new ArgumentNullException(nameof(getItemsFunc));
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

        // If the other is also a StorageSourceAdapter<T>, compare the wrapped storage sources
        if (other is StorageSourceAdapter<T> otherAdapter)
        {
            return ReferenceEquals(_storageSource, otherAdapter._storageSource) ||
                   (_storageSource?.Equals(otherAdapter._storageSource) ?? false);
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
            // Log the exception and return empty array to maintain stability
            // Replace with your logging mechanism
            Console.WriteLine($"Error getting items from storage source: {ex.Message}");
            return Array.Empty<ItemStack>();
        }
    }

    public void MarkModified()
    {
        _markModifiedAction(_storageSource);
    }
}
