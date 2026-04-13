using System.Collections.Generic;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.Storage;

namespace BeyondStorage.Source.Data;

/// <summary>
/// Stores (container, distance) pairs with on-demand distance sorting.
/// Accepts any <see cref="IStorageTargetSource"/>, allowing mixed storage source types.
/// Callers are responsible for ensuring each StorageSource is registered at most once.
/// </summary>
internal sealed class TargetDistanceStore
{
    private readonly List<(IStorageTargetSource Container, float Distance)> _entries = [];

    public bool IsSorted { get; private set; } = true;
    public int Count => _entries.Count;
    public IReadOnlyList<(IStorageTargetSource Storage, float Distance)> Entries => _entries;

    public void Add(IStorageTargetSource storage, float distance)
    {
        const string d_MethodName = nameof(Add);

        if (storage == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Null storage supplied");
            return;
        }

        _entries.Add((storage, distance));
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

        var result = new List<StorageTargetAdapter>(Entries.Count); // pre-sized to avoid resizes
        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (allowedSourcePolicy.IsAllowedSource(entry.Storage.GetSourceType()))
            {
                result.Add(new StorageTargetAdapter(entry.Storage, entry.Distance, filter));
            }
        }

        return result;
    }
}