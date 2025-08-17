using System;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Functions;

public class PurchasingCommon
{
    public const string PURCHASING_FUNCTION = "Purchasing_Function";

    /// <summary>
    /// Optimized implementation of CanSwapItems that fixes the original method's inefficiencies.
    /// Now includes storage-based currency checking for purchasing scenarios.
    /// </summary>
    /// <param name="inventory">The player inventory instance</param>
    /// <param name="removedStack">Items to be removed (typically currency)</param>
    /// <param name="addedStack">Items to be added (typically purchased items)</param>
    /// <param name="slotNumber">Optional specific slot number (-1 for any slot)</param>
    /// <returns>True if the swap is possible, false otherwise</returns>
    public static bool CanSwapItems(XUiM_PlayerInventory inventory, ItemStack removedStack, ItemStack addedStack, int slotNumber)
    {
        // Null checks
        if (removedStack?.itemValue?.ItemClass == null || addedStack?.itemValue?.ItemClass == null)
        {
            return false;
        }

        // Calculate how much we can remove (including from storage for currency)
        int canRemove = GetRemovableCountWithStorage(inventory, removedStack, slotNumber);
        if (canRemove < removedStack.count)
        {
#if DEBUG
            ModLogger.DebugLog($"CanSwapItems: Insufficient items to remove. Need={removedStack.count}, Available={canRemove}, Item={removedStack.itemValue.ItemClass.Name}");
#endif
            return false;
        }

        // Calculate available space for the item being added (including storage)
        // We need to account for the space that will be freed by removing items
        int currentAvailableSpace = GetAvailableSpaceWithStorage(inventory, addedStack.itemValue);
        int spaceFreedByRemoval = GetSpaceFreedByRemoval(inventory, removedStack, slotNumber);
        int totalAvailableSpace = currentAvailableSpace + spaceFreedByRemoval;

        if (totalAvailableSpace < addedStack.count)
        {
#if DEBUG
            ModLogger.DebugLog($"CanSwapItems: Insufficient space for items. Need={addedStack.count}, Available={totalAvailableSpace} (Current={currentAvailableSpace}, Freed={spaceFreedByRemoval}), Item={addedStack.itemValue.ItemClass.Name}");
#endif
            return false;
        }

#if DEBUG
        ModLogger.DebugLog($"CanSwapItems: SUCCESS - Remove {removedStack.count}x{removedStack.itemValue.ItemClass.Name}, Add {addedStack.count}x{addedStack.itemValue.ItemClass.Name}");
#endif
        return true;
    }

    /// <summary>
    /// Enhances the available space calculation by including storage containers.
    /// Takes the original player inventory space and adds available storage space.
    /// </summary>
    /// <param name="itemValue">The item to calculate space for</param>
    /// <param name="originalSpace">The original space from player inventory only</param>
    /// <param name="limitToOneStack">Whether to limit the result to one stack size</param>
    /// <param name="methodName">The calling method name for logging</param>
    /// <returns>Enhanced space count including storage containers</returns>
    public static int GetEnhancedAvailableSpace(ItemValue itemValue, int originalSpace, bool limitToOneStack, string methodName)
    {
        // Validate storage context and get additional storage space
        if (!ValidationHelper.ValidateStorageContext(methodName, out StorageContext context))
        {
            return originalSpace; // No storage available, keep original result
        }

        // Get storage space for this item type
        var storageSpace = GetStorageAvailableSpace(context, itemValue);

        // Add storage space to the original player inventory result
        var enhancedSpace = originalSpace + storageSpace;

        // Apply limitToOneStack constraint if requested
        if (limitToOneStack)
        {
            int maxStackSize = itemValue.ItemClass.Stacknumber.Value;
            if (enhancedSpace > maxStackSize)
            {
                enhancedSpace = maxStackSize;
            }
        }

#if DEBUG
        ModLogger.DebugLog($"{methodName}: Item={itemValue.ItemClass.Name}, Original={originalSpace}, Storage={storageSpace}, Final={enhancedSpace}, Limited={limitToOneStack}");
#endif

        return enhancedSpace;
    }

    /// <summary>
    /// Calculates how many items of the specified type can be removed from inventory + storage.
    /// This is the enhanced version that includes storage containers for currency transactions.
    /// </summary>
    /// <param name="inventory">The player inventory instance</param>
    /// <param name="removedStack">The stack to be removed</param>
    /// <param name="slotNumber">Optional specific slot number (-1 for any slot)</param>
    /// <returns>Total removable count including storage</returns>
    public static int GetRemovableCountWithStorage(XUiM_PlayerInventory inventory, ItemStack removedStack, int slotNumber)
    {
        // Start with player inventory count
        int removableCount = GetRemovableCount(inventory, removedStack, slotNumber);

        // Add storage count if available
        if (ValidationHelper.ValidateStorageContext(nameof(GetRemovableCountWithStorage), out StorageContext context))
        {
            int storageCount = context.GetItemCount(removedStack.itemValue);
            removableCount += storageCount;

#if DEBUG
            ModLogger.DebugLog($"GetRemovableCountWithStorage: Item={removedStack.itemValue.ItemClass.Name}, Player={removableCount - storageCount}, Storage={storageCount}, Total={removableCount}");
#endif
        }

        return removableCount;
    }

    /// <summary>
    /// Calculates available space for the specified item type in inventory + storage.
    /// This is the enhanced version that includes storage containers for purchased items.
    /// </summary>
    /// <param name="inventory">The player inventory instance</param>
    /// <param name="itemValue">The item type to calculate space for</param>
    /// <returns>Available space count including storage</returns>
    public static int GetAvailableSpaceWithStorage(XUiM_PlayerInventory inventory, ItemValue itemValue)
    {
        // Start with player inventory space
        int availableSpace = GetAvailableSpace(inventory, itemValue);

        // Add storage space if available
        if (ValidationHelper.ValidateStorageContext(nameof(GetAvailableSpaceWithStorage), out StorageContext context))
        {
            int storageSpace = GetStorageAvailableSpace(context, itemValue);
            availableSpace += storageSpace;

#if DEBUG
            ModLogger.DebugLog($"GetAvailableSpaceWithStorage: Item={itemValue.ItemClass.Name}, Player={availableSpace - storageSpace}, Storage={storageSpace}, Total={availableSpace}");
#endif
        }

        return availableSpace;
    }

    /// <summary>
    /// Calculates how many items of the specified type can be removed from inventory only.
    /// This is the player-inventory-only version for specific use cases.
    /// </summary>
    /// <param name="inventory">The player inventory instance</param>
    /// <param name="removedStack">The stack to be removed</param>
    /// <param name="slotNumber">Optional specific slot number (-1 for any slot)</param>
    /// <returns>Total removable count from player inventory only</returns>
    public static int GetRemovableCount(XUiM_PlayerInventory inventory, ItemStack removedStack, int slotNumber)
    {
        int removableCount = 0;
        ItemValue itemValue = removedStack.itemValue;

        // Check backpack slots
        ItemStack[] backpackSlots = inventory.Backpack.GetSlots();
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            if (slotNumber != -1 && slotNumber != i)
            {
                continue; // Skip if specific slot requested and this isn't it
            }

            if (backpackSlots[i]?.itemValue?.type == itemValue.type && backpackSlots[i].count > 0)
            {
                removableCount += backpackSlots[i].count;
            }
        }

        // Check toolbelt slots (only public slots)
        ItemStack[] toolbeltSlots = inventory.Toolbelt.GetSlots();
        int publicSlots = inventory.Toolbelt.PUBLIC_SLOTS;
        for (int i = 0; i < publicSlots && i < toolbeltSlots.Length; i++)
        {
            // Adjust slot number for toolbelt (assuming backpack slots come first)
            int adjustedSlotNumber = slotNumber - backpackSlots.Length;
            if (slotNumber != -1 && adjustedSlotNumber != i)
            {
                continue;
            }

            if (toolbeltSlots[i]?.itemValue?.type == itemValue.type && toolbeltSlots[i].count > 0)
            {
                removableCount += toolbeltSlots[i].count;
            }
        }

        return removableCount;
    }

    /// <summary>
    /// Calculates available space for the specified item type in inventory only.
    /// This is the player-inventory-only version for specific use cases.
    /// </summary>
    /// <param name="inventory">The player inventory instance</param>
    /// <param name="itemValue">The item type to calculate space for</param>
    /// <returns>Available space count in player inventory only</returns>
    public static int GetAvailableSpace(XUiM_PlayerInventory inventory, ItemValue itemValue)
    {
        int maxStackSize = itemValue.ItemClass.Stacknumber.Value;
        int availableSpace = 0;

        // Check backpack slots
        ItemStack[] backpackSlots = inventory.Backpack.GetSlots();
        foreach (var slot in backpackSlots)
        {
            if (slot.IsEmpty())
            {
                availableSpace += maxStackSize;
            }
            else if (slot.itemValue.type == itemValue.type)
            {
                availableSpace += maxStackSize - slot.count;
            }
        }

        // Check toolbelt slots (only public slots)
        ItemStack[] toolbeltSlots = inventory.Toolbelt.GetSlots();
        int publicSlots = inventory.Toolbelt.PUBLIC_SLOTS;
        for (int i = 0; i < publicSlots && i < toolbeltSlots.Length; i++)
        {
            if (toolbeltSlots[i].IsEmpty())
            {
                availableSpace += maxStackSize;
            }
            else if (toolbeltSlots[i].itemValue.type == itemValue.type)
            {
                availableSpace += maxStackSize - toolbeltSlots[i].count;
            }
        }

        return availableSpace;
    }

    /// <summary>
    /// Calculates how much space will be freed by removing the specified items from player inventory.
    /// Note: This only considers player inventory since storage removal doesn't free player inventory space.
    /// </summary>
    /// <param name="inventory">The player inventory instance</param>
    /// <param name="removedStack">The stack to be removed</param>
    /// <param name="slotNumber">Optional specific slot number (-1 for any slot)</param>
    /// <returns>Space that will be freed in player inventory</returns>
    public static int GetSpaceFreedByRemoval(XUiM_PlayerInventory inventory, ItemStack removedStack, int slotNumber)
    {
        if (removedStack.itemValue.type == 0)
        {
            return 0; // Invalid item type
        }

        int maxStackSize = removedStack.itemValue.ItemClass.Stacknumber.Value;
        int spaceFreed = 0;
        int remainingToRemove = removedStack.count;

        // Check backpack slots
        ItemStack[] backpackSlots = inventory.Backpack.GetSlots();
        for (int i = 0; i < backpackSlots.Length && remainingToRemove > 0; i++)
        {
            if (slotNumber != -1 && slotNumber != i)
            {
                continue;
            }

            if (backpackSlots[i]?.itemValue?.type == removedStack.itemValue.type && backpackSlots[i].count > 0)
            {
                int currentCount = backpackSlots[i].count;
                int toRemoveFromSlot = Math.Min(currentCount, remainingToRemove);

                if (toRemoveFromSlot == currentCount)
                {
                    // Slot will become empty
                    spaceFreed += maxStackSize;
                }
                else
                {
                    // Slot will have partial items removed
                    spaceFreed += toRemoveFromSlot;
                }

                remainingToRemove -= toRemoveFromSlot;
            }
        }

        // Check toolbelt slots
        ItemStack[] toolbeltSlots = inventory.Toolbelt.GetSlots();
        int publicSlots = inventory.Toolbelt.PUBLIC_SLOTS;
        for (int i = 0; i < publicSlots && i < toolbeltSlots.Length && remainingToRemove > 0; i++)
        {
            int adjustedSlotNumber = slotNumber - backpackSlots.Length;
            if (slotNumber != -1 && adjustedSlotNumber != i)
            {
                continue;
            }

            if (toolbeltSlots[i]?.itemValue?.type == removedStack.itemValue.type && toolbeltSlots[i].count > 0)
            {
                int currentCount = toolbeltSlots[i].count;
                int toRemoveFromSlot = Math.Min(currentCount, remainingToRemove);

                if (toRemoveFromSlot == currentCount)
                {
                    // Slot will become empty
                    spaceFreed += maxStackSize;
                }
                else
                {
                    // Slot will have partial items removed
                    spaceFreed += toRemoveFromSlot;
                }

                remainingToRemove -= toRemoveFromSlot;
            }
        }

        return spaceFreed;
    }

    /// <summary>
    /// Calculates available space for the specified item type in storage containers
    /// </summary>
    private static int GetStorageAvailableSpace(StorageContext context, ItemValue itemValue)
    {
        int maxStackSize = itemValue.ItemClass.Stacknumber.Value;
        int availableSpace = 0;

        try
        {
            // Get all storage containers
            var allStorageStacks = context.GetAllAvailableItemStacks(UniqueItemTypes.Unfiltered);

            // Count available space in storage
            foreach (var storageStack in allStorageStacks)
            {
                if (storageStack.IsEmpty())
                {
                    availableSpace += maxStackSize;
                }
                else if (storageStack.itemValue.type == itemValue.type)
                {
                    // Existing stack with same item type - calculate remaining space
                    int remainingSpace = maxStackSize - storageStack.count;
                    if (remainingSpace > 0)
                    {
                        availableSpace += remainingSpace;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModLogger.DebugLog($"GetStorageAvailableSpace: Error calculating storage space: {ex.Message}");
            return 0; // Return 0 on error to maintain original behavior
        }

        return availableSpace;
    }
}
