namespace BeyondStorage.Source.Data;

/// <summary>
/// Raw slot data extracted from a storage source in a single read.
/// Contains no pre-filtering — classification of consumable, pushable, loadout,
/// and empty slots is entirely the responsibility of <see cref="StorageSourceItemDataStore"/>.
/// </summary>
internal sealed class SourceSlotData
{
    /// <summary>
    /// All slots from the source without any filtering, including empty slots.
    /// This is the unfiltered source of truth from which all views are derived.
    /// </summary>
    internal ItemStack[] AllSlots { get; }

    /// <summary>
    /// Packed lock state indexed by slot position.
    /// Null when the source has no slot lock support, which means all slots are treated as unlocked.
    /// </summary>
    internal PackedBoolArray LockedSlots { get; }

    internal SourceSlotData(ItemStack[] allSlots, PackedBoolArray lockedSlots)
    {
        AllSlots = allSlots ?? [];
        LockedSlots = lockedSlots;
    }
}