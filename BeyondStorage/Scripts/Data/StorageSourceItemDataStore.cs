#nullable enable

using System;
using System.Collections.Generic;

namespace BeyondStorage.Scripts.Data;

internal class StorageSourceItemDataStore
{
    private readonly Dictionary<IStorageSource, List<ItemStack>> _itemStacksBySource = new Dictionary<IStorageSource, List<ItemStack>>();
    private readonly Dictionary<ItemStack, IStorageSource> _sourcesByItemStack = new Dictionary<ItemStack, IStorageSource>();
    private readonly Dictionary<Type, List<IStorageSource>> _sourcesByType = new();

    public void AddRelationship(IStorageSource source, ItemStack stack)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source), "Storage source cannot be null.");
        }

        if (stack == null)
        {
            throw new ArgumentNullException(nameof(stack), "Item stack cannot be null.");
        }

        if (_sourcesByItemStack.TryGetValue(stack, out var existingStorageSource))
        {
            if (existingStorageSource.Equals(source))
            {
                throw new InvalidOperationException("ItemStack is already associated with this storage source.");
            }
            else
            {
                throw new InvalidOperationException("ItemStack is already associated with a different storage source.");
            }
        }

        if (!_itemStacksBySource.TryGetValue(source, out var itemStackList))
        {
            itemStackList = CollectionFactory.CreateItemStackList();
            _itemStacksBySource[source] = itemStackList;

            // Add to type tracking
            var sourceType = source.GetType();
            if (!_sourcesByType.TryGetValue(sourceType, out var sourcesOfType))
            {
                sourcesOfType = CollectionFactory.CreateStorageSourceList();
                _sourcesByType[sourceType] = sourcesOfType;
            }
            sourcesOfType.Add(source);
        }

        itemStackList.Add(stack);
        _sourcesByItemStack[stack] = source;
    }

    public void RemoveRelationship(IStorageSource? source, ItemStack? stack)
    {
        if (source == null || stack == null)
        {
            return;
        }

        // Check if the relationship exists and is correct
        if (_sourcesByItemStack.TryGetValue(stack, out var existingSource) &&
            existingSource.Equals(source))
        {
            _sourcesByItemStack.Remove(stack);

            if (_itemStacksBySource.TryGetValue(source, out var itemStackList))
            {
                itemStackList.Remove(stack);

                // Clean up empty lists to prevent memory bloat
                if (itemStackList.Count == 0)
                {
                    _itemStacksBySource.Remove(source);

                    // Remove from type tracking
                    var sourceType = source.GetType();
                    if (_sourcesByType.TryGetValue(sourceType, out var sourcesOfType))
                    {
                        sourcesOfType.Remove(source);
                        if (sourcesOfType.Count == 0)
                        {
                            _sourcesByType.Remove(sourceType);
                        }
                    }
                }
            }
        }
    }

    public void RemoveAllRelationshipsForSource(IStorageSource? source)
    {
        if (source == null)
        {
            return;
        }

        if (_itemStacksBySource.TryGetValue(source, out var itemStackList))
        {
            // Remove all reverse mappings
            foreach (var itemStack in itemStackList)
            {
                _sourcesByItemStack.Remove(itemStack);
            }

            // Remove the source entry
            _itemStacksBySource.Remove(source);

            // Remove from type tracking
            var sourceType = source.GetType();
            if (_sourcesByType.TryGetValue(sourceType, out var sourcesOfType))
            {
                sourcesOfType.Remove(source);
                if (sourcesOfType.Count == 0)
                {
                    _sourcesByType.Remove(sourceType);
                }
            }
        }
    }

    /// <summary>
    /// Clears all relationships from the data store, removing all storage sources and item stacks.
    /// </summary>
    public void Clear()
    {
        _itemStacksBySource.Clear();
        _sourcesByItemStack.Clear();
        _sourcesByType.Clear();
    }

    public IReadOnlyList<IStorageSource> GetSourcesByType<T>() where T : class, IStorageSource
    {
        return GetSourcesByType(typeof(T));
    }

    public IReadOnlyList<IStorageSource> GetSourcesByType(Type sourceType)
    {
        if (_sourcesByType.TryGetValue(sourceType, out var sources))
        {
            return sources.AsReadOnly();
        }
        return Array.Empty<IStorageSource>();
    }

    public List<ItemStack> GetItemsStacksBySource(IStorageSource source)
    {
        if (_itemStacksBySource.TryGetValue(source, out List<ItemStack> result))
        {
            return result;
        }

        return [];
    }

    public IStorageSource? GetSourceByItemStack(ItemStack stack)
    {
        if (_sourcesByItemStack.TryGetValue(stack, out var result))
        {
            return result;
        }

        return null;
    }

    public IEnumerable<IStorageSource> GetAllSources()
    {
        return _itemStacksBySource.Keys;
    }

    public IEnumerable<Type> GetAllSourceTypes()
    {
        return _sourcesByType.Keys;
    }

    /// <summary>
    /// Gets diagnostic information about the current state of the data store.
    /// </summary>
    public string GetDiagnosticInfo()
    {
        var totalSources = _itemStacksBySource.Count;
        var totalStacks = _sourcesByItemStack.Count;
        var totalTypes = _sourcesByType.Count;

        return $"Sources: {totalSources}, Stacks: {totalStacks}, Types: {totalTypes}";
    }
}
