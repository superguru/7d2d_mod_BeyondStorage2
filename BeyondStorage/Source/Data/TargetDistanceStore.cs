using System.Collections.Generic;
using BeyondStorage.Infrastructure;
using BeyondStorage.Storage;

namespace BeyondStorage.Data;

/// <summary>
/// Stores (storage, distance) pairs with on-demand distance sorting.
/// Accepts any <see cref="IStorageTarget"/>, allowing mixed storage source types.
/// Slot maps are pre-built at registration and cloned per operation at query time.
/// Callers are responsible for ensuring each StorageSource is registered at most once.
/// </summary>
internal sealed class TargetDistanceStore
{
    private readonly List<(IStorageTarget Storage, float Distance, SlotMaps AllItems, SlotMaps Pushable)> _entries = [];

    public bool IsSorted { get; private set; } = true;

    public void Add(IStorageTarget storage, float distance, SlotMaps allItemsMaps, SlotMaps pushableMaps)
    {
        const string d_MethodName = nameof(Add);

        if (storage == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Null storage supplied");
            return;
        }

        _entries.Add((storage, distance, allItemsMaps, pushableMaps));
        IsSorted = false;
    }

    /// <summary>
    /// Sorts entries by distance ascending. No-op if already sorted.
    /// </summary>
    public void Sort()
    {
        if (IsSorted)
        {
            return;
        }

        _entries.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
        IsSorted = true;
    }

    public void Clear()
    {
        _entries.Clear();
        IsSorted = true;
    }

    internal IReadOnlyList<StorageTargetAdapter> GetClosestStorageSources(AllowedSourcesList allowedSourcePolicy, ItemScope filter)
    {
        Sort();

        var result = new List<StorageTargetAdapter>(_entries.Count);
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!allowedSourcePolicy.IsAllowedSource(entry.Storage.GetSourceType()))
            {
                continue;
            }

            // Clone gives each operation its own mutable copy for ReclassifySlot
            var maps = filter == ItemScope.AllItems ? entry.AllItems.Clone() : entry.Pushable.Clone();
            result.Add(new StorageTargetAdapter(entry.Storage, entry.Distance, maps));
        }

        return result;
    }
}