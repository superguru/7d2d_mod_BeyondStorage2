#nullable enable

using System;
using System.Collections.Generic;

namespace BeyondStorage.Scripts.Data;

internal class StorageSourceItemDataStore
{
    private readonly Dictionary<IStorageSource, List<ItemStack>> _itemStacksByStorageSource = new Dictionary<IStorageSource, List<ItemStack>>();
    private readonly Dictionary<ItemStack, IStorageSource> _storageSourcesByItemStack = new Dictionary<ItemStack, IStorageSource>();

    public void AddRelationship(IStorageSource storageSource, ItemStack itemStack)
    {
        if (storageSource == null)
        {
            throw new ArgumentNullException(nameof(storageSource), "Storage source cannot be null.");
        }

        if (itemStack == null)
        {
            throw new ArgumentNullException(nameof(itemStack), "Item stack cannot be null.");
        }

        if (_storageSourcesByItemStack.TryGetValue(itemStack, out var existingStorageSource))
        {
            if (existingStorageSource.Equals(storageSource))
            {
                // You made a programming mistake when you changed the mod, and now we're here.
                throw new InvalidOperationException("ItemStack is already associated with this storage source.");
            }
            else
            {
                // If the item stack is associated with a different entity, we should not allow this.
                throw new InvalidOperationException("ItemStack is already associated with a different storage source.");
            }
        }

        if (!_itemStacksByStorageSource.TryGetValue(storageSource, out var itemStackList))
        {
            itemStackList = CollectionFactory.CreateItemStackList();
            _itemStacksByStorageSource[storageSource] = itemStackList;
        }

        itemStackList.Add(itemStack);
        _storageSourcesByItemStack[itemStack] = storageSource;
    }

    public void RemoveRelationship(IStorageSource? storageSource, ItemStack? itemStack)
    {
        if (storageSource == null || itemStack == null)
        {
            return; // Nothing to remove
        }

        // Check if the relationship exists and is correct
        if (_storageSourcesByItemStack.TryGetValue(itemStack, out var existingStorageSource) &&
            existingStorageSource.Equals(storageSource))
        {
            _storageSourcesByItemStack.Remove(itemStack);

            if (_itemStacksByStorageSource.TryGetValue(storageSource, out var itemStackList))
            {
                itemStackList.Remove(itemStack);

                // Clean up empty lists to prevent memory bloat
                if (itemStackList.Count == 0)
                {
                    _itemStacksByStorageSource.Remove(storageSource);
                }
            }
        }
    }

    public void RemoveAllRelationshipsForStorageSource(IStorageSource? storageSource)
    {
        if (storageSource == null)
        {
            return;
        }

        if (_itemStacksByStorageSource.TryGetValue(storageSource, out var itemStackList))
        {
            // Remove all reverse mappings
            foreach (var itemStack in itemStackList)
            {
                _storageSourcesByItemStack.Remove(itemStack);
            }

            // Remove the storage source entry
            _itemStacksByStorageSource.Remove(storageSource);
        }
    }

    /// <summary>
    /// Clears all relationships from the data store, removing all storage sources and item stacks.
    /// </summary>
    public void Clear()
    {
        _itemStacksByStorageSource.Clear();
        _storageSourcesByItemStack.Clear();
    }

    public List<ItemStack> GetItemsStacksForStorageSource(IStorageSource storageSource)
    {
        if (_itemStacksByStorageSource.TryGetValue(storageSource, out List<ItemStack> result))
        {
            return result;
        }

        return [];
    }

    public IStorageSource? GetStorageSourceForItemStack(ItemStack itemStack)
    {
        if (_storageSourcesByItemStack.TryGetValue(itemStack, out var result))
        {
            return result;
        }

        return null;
    }

    public IEnumerable<IStorageSource> GetAllEntities()
    {
        return _itemStacksByStorageSource.Keys;
    }
}
