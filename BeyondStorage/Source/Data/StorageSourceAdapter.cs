using System;
using System.Runtime.CompilerServices;
using BeyondStorage.Diagnostics;
using BeyondStorage.Infrastructure;

namespace BeyondStorage.Data;

internal class StorageSourceAdapter<T> : IStorageSource, IStorageTarget where T : class
{
    private const int HASH_MULTIPLIER = 419;

    // Replace the private readonly field and explicit property with an auto-property
    public T StorageSource { get; }

    private readonly Type _storageSourceType;

    private readonly Func<T, T, bool> _equalsFunc;
    private readonly Func<T, ItemStack[]> _getAllItemsFunc;
    private readonly Func<T, PackedBoolArray> _getLockedSlotsFunc;
    private readonly Action<T> _markModifiedAction;
    private readonly Func<T, string> _getNameFunc;

    public StorageSourceAdapter(
        T storageSource,
        Func<T, T, bool> equalsFunc,
        Func<T, ItemStack[]> getAllItemsFunc,
        Func<T, PackedBoolArray> getLockedSlotsFunc,
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

        if (getAllItemsFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getAllItemsFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getAllItemsFunc), error);
        }

        if (getLockedSlotsFunc == null)
        {
            var error = $"{d_MethodName}: {nameof(getLockedSlotsFunc)} cannot be null";
            ModLogger.DebugLog(error);
            throw new ArgumentNullException(nameof(getLockedSlotsFunc), error);
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
        _getAllItemsFunc = getAllItemsFunc;
        _getLockedSlotsFunc = getLockedSlotsFunc;
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
    /// Provides consistent error handling across all item stack retrieval operations.
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
            ModLogger.DebugLog($"{methodName}({sourceTypeAbbrev}) | Null reference accessing items: {ex.Message}. Storage source may have been disposed.", ex);
            return [];
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"{methodName}({sourceTypeAbbrev}) | Error getting items: {ex.Message}", ex);
            return [];
        }
    }

    /// <summary>
    /// Returns all slots without any filtering, including empty slots.
    /// Classification of consumable, pushable, and empty stacks is the responsibility
    /// of <see cref="StorageSourceItemDataStore"/>, not the source.
    /// </summary>
    /// <returns>
    /// Array of all ItemStack objects in the storage source, including empty slots.
    /// Returns an empty array if an error occurs or if the source has no slots.
    /// </returns>
    public ItemStack[] GetAllItemStacks()
    {
        const string d_MethodName = nameof(GetAllItemStacks);
        return GetSpecifiedItemStacks(d_MethodName, _getAllItemsFunc);
    }

    /// <summary>
    /// Returns raw slot data in a single read for registration as a push/pull target.
    /// The data store uses <see cref="SourceSlotData.AllSlots"/> and
    /// <see cref="SourceSlotData.LockedSlots"/> to classify slots without any pre-filtering
    /// by the source.
    /// </summary>
    public SourceSlotData GetSlotData()
    {
        const string d_MethodName = nameof(GetSlotData);
        var sourceTypeAbbrev = TypeNames.GetAbbrev(_storageSourceType);

        var allSlots = GetSpecifiedItemStacks(d_MethodName, _getAllItemsFunc);

        PackedBoolArray lockedSlots = null;
        try
        {
            // Null is a valid return value — it means the source has no slot lock support
            lockedSlots = _getLockedSlotsFunc(StorageSource);
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"{d_MethodName}({sourceTypeAbbrev}) | Error getting locked slots: {ex.Message}. Treating all slots as unlocked.");
        }

        return new SourceSlotData(allSlots, lockedSlots);
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
