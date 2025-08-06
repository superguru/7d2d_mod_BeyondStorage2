using System;
using System.Collections.Generic;
using System.Text;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Multiplayer;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Handles finding and processing items from tile entity storage sources.
/// </summary>
internal static class TileEntityItemDiscovery
{
    internal static readonly PackedBoolArray s_emptyLockedSlots = new PackedBoolArray();

    public static void FindItems(StorageContext context)
    {
        const string d_MethodName = nameof(FindItems);

        var config = context.Config;
        var world = context.WorldPlayerContext;
        var sources = context.Sources;
        var playerId = world.PlayerEntityId;

        bool pullFromDewCollectors = config.PullFromDewCollectors;
        bool pullFromWorkstationOutputs = config.PullFromWorkstationOutputs;
        bool hasLockedEntities = TileEntityLockManager.LockedTileEntities.Count > 0;

        int chunksProcessed = 0;
        int nullChunks = 0;
        int tileEntitiesProcessed = 0;

        foreach (var chunk in world.ChunkCacheCopy)
        {
            if (chunk == null)
            {
                nullChunks++;
                continue;
            }

            chunksProcessed++;

            var tileEntityList = chunk.tileEntities?.list;
            if (tileEntityList == null)
            {
                continue;
            }

            foreach (var tileEntity in tileEntityList)
            {
                tileEntitiesProcessed++;

                if (tileEntity.IsRemoving)
                {
                    continue;
                }

                var tileEntityWorldPos = tileEntity.ToWorldPos();

                // Early range check to avoid unnecessary processing
                if (!world.IsWithinRange(tileEntityWorldPos, config.Range))
                {
                    continue;
                }

                // Check locks early
                if (hasLockedEntities)
                {
                    if (TileEntityLockManager.LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) && entityId != playerId)
                    {
                        continue;
                    }
                }

                // Check accessibility
                if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                {
                    if (!world.CanAccessLockable(tileLockable))
                    {
                        continue;
                    }
                }

                // Process each type separately with clear logic
                if (pullFromDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
                {
                    ProcessDewCollectorItems(context, dewCollector);
                    continue;
                }

                if (pullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                {
                    ProcessWorkstationItems(context, workstation);
                    continue;
                }

                // Process lootables (containers) - always enabled since they're primary storage
                if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable))
                {
                    ProcessLootableItems(context, lootable, tileEntity);
                    continue;
                }
            }
        }

        ModLogger.DebugLog($"{d_MethodName}: Processed {chunksProcessed} chunks, {nullChunks} null chunks, {tileEntitiesProcessed} tile entities");
    }

    private static int ProcessDewCollectorItems(StorageContext context, TileEntityDewCollector dewCollector)
    {
        if (dewCollector.bUserAccessing)
        {
            return 0;
        }

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<TileEntityDewCollector>(
            dewCollector,
            sources.EqualsDewCollectorFunc,
            sources.GetItemsDewCollectorFunc,
            sources.MarkModifiedDewCollectorFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        return validStacksRegistered;
    }

    private static int ProcessWorkstationItems(StorageContext context, TileEntityWorkstation workstation)
    {
        if (!workstation.IsPlayerPlaced)
        {
            return 0;
        }

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<TileEntityWorkstation>(
            workstation,
            sources.EqualsWorkstationFunc,
            sources.GetItemsWorkstationFunc,
            sources.MarkModifiedWorkstationFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        return validStacksRegistered;
    }

    private static int ProcessLootableItems(StorageContext context, ITileEntityLootable lootable, TileEntity tileEntity)
    {
        const string d_MethodName = nameof(ProcessLootableItems);

        if (!lootable.bPlayerStorage)
        {
            return 0;
        }

        var config = context.Config;
        if (config.OnlyStorageCrates)
        {
            if (!tileEntity.TryGetSelfOrFeature(out TEFeatureStorage storage) && storage != null)
            {
                return 0;
            }
        }

        LogLootableSlotLocks(context, lootable, tileEntity, d_MethodName);

        var sources = context.Sources;
        var sourceAdapter = new StorageSourceAdapter<ITileEntityLootable>(
            lootable,
            sources.EqualsLootableFunc,
            sources.GetItemsLootableFunc,
            sources.MarkModifiedLootableFunc
        );

        sources.DataStore.RegisterSource(sourceAdapter, out int validStacksRegistered);
        return validStacksRegistered;
    }

    public static ItemStack[] GetLootableItems(ITileEntityLootable lootable)
    {
        if (lootable == null)
        {
            return [];
        }

        var items = lootable.items;
        if (items == null || items.Length == 0)
        {
            return [];
        }

        if (!lootable.HasSlotLocksSupport)
        {
            return items;
        }

        PackedBoolArray lockedSlots = lootable.SlotLocks ?? s_emptyLockedSlots;
        var containerSize = lootable.GetContainerSize();
        int cx = containerSize.x;
        int cy = containerSize.y;
        int totalSlots = cx * cy;
        int lockedSlotsLength = lockedSlots.Length;
        int itemsLength = items.Length;
        int maxSlots = Math.Min(totalSlots, itemsLength);

        // Pre-calculate result capacity to minimize reallocations
        int estimatedUnlockedSlots = Math.Min(maxSlots, Math.Max(0, lockedSlotsLength == 0 ? maxSlots : maxSlots - CountLockedSlots(lockedSlots, maxSlots)));
        var result = new List<ItemStack>(estimatedUnlockedSlots);

        // Single loop optimization - avoid nested loops
        for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
        {
            // Skip locked slots early
            if (slotIndex < lockedSlotsLength && lockedSlots[slotIndex])
            {
                continue;
            }

            var stack = items[slotIndex];
            if (stack != null)
            {
                result.Add(stack);
            }
        }

        return result.ToArray();
    }

    private static int CountLockedSlots(PackedBoolArray lockedSlots, int maxSlots)
    {
        int count = 0;
        int checkSlots = Math.Min(lockedSlots.Length, maxSlots);

        for (int i = 0; i < checkSlots; i++)
        {
            if (lockedSlots[i])
            {
                count++;
            }
        }

        return count;
    }

    private static void LogLootableSlotLocks(StorageContext context, ITileEntityLootable lootable, TileEntity tileEntity, string d_MethodName)
    {
        if (!context?.Config?.IsDebugLogSettingsAccess ?? false)
        {
            return;
        }

        var hasLockSlotSupport = lootable.HasSlotLocksSupport;
        if (hasLockSlotSupport)
        {
            ModLogger.DebugLog($"{d_MethodName}: Found lootable {lootable} with slot locks support {tileEntity.ToWorldPos()}");

            var containerSize = lootable.GetContainerSize();
            int cx = containerSize.x;
            int cy = containerSize.y;

            PackedBoolArray lockedSlots = lootable.SlotLocks;

            // Create grid representation: 1 for locked, 0 for unlocked
            var sb = new StringBuilder(cx * cy * 2);
            for (int y = 0; y < cy; y++)
            {
                for (int x = 0; x < cx; x++)
                {
                    int slotIndex = y * cx + x;
                    bool isLocked = lockedSlots != null && lockedSlots[slotIndex];
                    sb.Append(isLocked ? '1' : '0');

                    // Add space between columns (except for last column)
                    if (x < containerSize.x - 1)
                    {
                        sb.Append(' ');
                    }
                }

                // Add newline between rows (except for last row)
                if (y < cy - 1)
                {
                    sb.AppendLine();
                }
            }

            var lockSlotMap = sb.ToString();
            ModLogger.DebugLog($"{d_MethodName}: Locked slots for {lootable} {containerSize.x}x{containerSize.y}:\n{lockSlotMap}");
        }
    }
}