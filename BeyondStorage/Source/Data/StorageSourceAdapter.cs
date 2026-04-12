using System;
using System.Runtime.CompilerServices;
using BeyondStorage.Source.Diagnostics;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.Data;

internal class StorageSourceAdapter<T> : IStorageSource where T : class
{
    private const int HASH_MULTIPLIER = 397;

    // Replace the private readonly field and explicit property with an auto-property
    public T StorageSource { get; }

    private readonly Type _storageSourceType;

    private readonly Func<T, T, bool> _equalsFunc;
    private readonly Func<T, ItemStack[]> _getConsumableItemStacksFunc;
    private readonly Func<T, ItemStack[]> _getPushableItemStacksFunc;
    private readonly Func<T, ItemStack[]> _getAllSlotsItemStacksFunc;
    private readonly Action<T> _markModifiedAction;
    private readonly Func<T, string> _getNameFunc;

    public StorageSourceAdapter(
        T storageSource,
        Func<T, T, bool> equalsFunc,
        Func<T, ItemStack[]> getConsumableItemStacksFunc,
        Func<T, ItemStack[]> getPushableItemStacksFunc,
        Func<T, ItemStack[]> getAllSlotsItemStacksFunc,
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

        if (getConsumableItemStacksFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getConsumableItemStacksFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getConsumableItemStacksFunc), error);
        }

        if (getPushableItemStacksFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getPushableItemStacksFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getPushableItemStacksFunc), error);
        }

        if (getAllSlotsItemStacksFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getAllSlotsItemStacksFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getAllSlotsItemStacksFunc), error);
        }

        if (markModifiedAction == null)
        {
            var error = $"{d_MethodName}: {nameof(markModifiedAction)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(markModifiedAction), error);
        }

        if (getNameFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getNameFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getNameFunc), error);
        }

        StorageSource = storageSource;

        _storageSourceType = storageSource.GetType();

        _equalsFunc = equalsFunc;
        _getConsumableItemStacksFunc = getConsumableItemStacksFunc;
        _getPushableItemStacksFunc = getPushableItemStacksFunc;
        _getAllSlotsItemStacksFunc = getAllSlotsItemStacksFunc;
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
        unchecked
        {
            int hash = RuntimeHelpers.GetHashCode(StorageSource);
            hash = (hash * HASH_MULTIPLIER) ^ typeof(T).GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Helper method that safely invokes an item stack retrieval function with comprehensive error handling and logging.
    /// Provides consistent error handling across all item stack retrieval operations (pullable, pushable, and all slots).
    /// </summary>
    /// <param name="methodName">The name of the calling method, used for logging and diagnostics</param>
    /// <param name="getItemStacksFunc">The function to invoke to retrieve item stacks from the storage source</param>
    /// <returns>
    /// Array of ItemStack objects returned by the function, or an empty array if:
    /// - The function returns null
    /// - A NullReferenceException occurs (storage source may have been disposed)
    /// - Any other exception occurs during retrieval
    /// </returns>
    /// <remarks>
    /// This method ensures that item stack retrieval never throws exceptions to the caller,
    /// maintaining stability even when the underlying storage source is in an invalid state.
    /// All error conditions are logged for debugging purposes.
    /// </remarks>
    private ItemStack[] GetSpecifiedItemStacks(string methodName, Func<T, ItemStack[]> getItemStacksFunc)
    {
        var sourceTypeAbbrev = TypeNames.GetAbbrev(_storageSourceType);

        try
        {
            var items = getItemStacksFunc(StorageSource);
            if (items == null)
            {
                ModLogger.DebugLog($"{methodName}({sourceTypeAbbrev}) | Returned null items, using empty array");
                return [];
            }
            return items;
        }
        catch (NullReferenceException ex)
        {
            ModLogger.DebugLog($"{methodName}({sourceTypeAbbrev}) | Null reference accessing items: {ex.Message}. Storage source may have been disposed.");
            return [];
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"{methodName}({sourceTypeAbbrev}) | Error getting items: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Gets item stacks from the storage source that are available for consumption when repairing, upgrading, painting, etc.
    /// Filters out empty slots. Does not apply locked slot filtering for pull operations.
    /// </summary>
    /// <returns>
    /// Array of ItemStack objects from non-empty slots that can be consumed by this storage source.
    /// Returns an empty array if an error occurs or if the source has no consumable items.
    /// </returns>
    public ItemStack[] GetConsumableItemStacks()
    {
        const string d_MethodName = nameof(GetConsumableItemStacks);
        return GetSpecifiedItemStacks(d_MethodName, _getConsumableItemStacksFunc);
    }

    /// <summary>
    /// Gets item stacks from the storage source that are available to be pushed to other storage targets.
    /// Filters out items from locked slots (if the storage supports slot locking) and empty slots.
    /// </summary>
    /// <returns>
    /// Array of ItemStack objects from unlocked, non-empty slots that can be pushed to other storage targets.
    /// Returns an empty array if an error occurs or if the source has no pushable items.
    /// </returns>
    public ItemStack[] GetPushableItemStacks()
    {
        const string d_MethodName = nameof(GetPushableItemStacks);
        return GetSpecifiedItemStacks(d_MethodName, _getPushableItemStacksFunc);
    }

    /// <summary>
    /// Gets all item stacks from all slots in the storage source without any filtering.
    /// Includes both empty and non-empty slots, as well as locked and unlocked slots.
    /// </summary>
    /// <returns>
    /// Array of all ItemStack objects in the storage source, including empty slots.
    /// Returns an empty array if an error occurs or if the source has no slots.
    /// </returns>
    public ItemStack[] GetAllSlotItemsStacks()
    {
        const string d_MethodName = nameof(GetAllSlotItemsStacks);
        return GetSpecifiedItemStacks(d_MethodName, _getAllSlotsItemStacksFunc);
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
