using System.Collections.Generic;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Data;

/// <summary>
/// Stores (container, distance) pairs with on-demand distance sorting.
/// Callers are responsible for ensuring each StorageSource is registered at most once.
/// </summary>
internal sealed class ContainerDistanceStore
{
    private readonly List<(IStorageSource Container, float Distance)> _entries = [];

    public bool IsSorted { get; private set; } = true;
    public int Count => _entries.Count;
    public IReadOnlyList<(IStorageSource Container, float Distance)> Entries => _entries;

    public void Add<T>(StorageSourceAdapter<T> container, float distance) where T : class
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
}