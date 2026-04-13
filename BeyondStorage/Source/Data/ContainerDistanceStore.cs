using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.Storage;

namespace BeyondStorage.Source.Data;

/// <summary>
/// Stores (container, distance) pairs with on-demand distance sorting.
/// Accepts any <see cref="IStorageTargetSource"/>, allowing mixed storage source types.
/// Callers are responsible for ensuring each StorageSource is registered at most once.
/// </summary>
internal sealed class ContainerDistanceStore
{
    private readonly List<(IStorageTargetSource Container, float Distance)> _entries = [];

    public bool IsSorted { get; private set; } = true;
    public int Count => _entries.Count;
    public IReadOnlyList<(IStorageTargetSource Container, float Distance)> Entries => _entries;

    public void Add(IStorageTargetSource container, float distance)
    {
        const string d_MethodName = nameof(Add);

        if (container == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Null container supplied");
            return;
        }

        _entries.Add((container, distance));
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

    internal IReadOnlyList<StorageTargetAdapter> GetClosestTargetContainers(ItemScope filter)
    {
        Sort();

        return Entries.Select(entry => new StorageTargetAdapter(entry.Container, entry.Distance, filter)).ToList();
    }
}