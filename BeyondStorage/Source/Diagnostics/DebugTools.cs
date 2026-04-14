using System.Text;
using BeyondStorage.Source.Data;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.Source.Diagnostics;

/// <summary>
/// Provides diagnostic utility methods for inspecting game storage state.
/// </summary>
public static class DebugTools
{
    /// <summary>
    /// Logs the contents of a lootable tile entity as a CSV matrix using its known dimensions.
    /// </summary>
    /// <param name="methodName">Calling method name, prepended to the log line</param>
    /// <param name="lootable">The lootable tile entity to inspect</param>
    public static void LogLootContainerContents(string methodName, ITileEntityLootable lootable)
    {
        ModLogger.DebugLog($"{methodName}: Lootable contents:\n{GetLootContainerContents(lootable)}");
    }

    /// <summary>
    /// Logs a flat item array as a CSV matrix.
    /// When <paramref name="width"/> or <paramref name="height"/> is 0, dimensions are
    /// guessed via <see cref="GuessMatrixDimensions"/>.
    /// </summary>
    /// <param name="methodName">Calling method name, prepended to the log line</param>
    /// <param name="stacks">The flat array of item stacks to display</param>
    /// <param name="width">Number of columns. Pass 0 to guess from item count.</param>
    /// <param name="height">Number of rows. Pass 0 to guess from item count.</param>
    public static void LogLootContainerContents(string methodName, ItemStack[] stacks, int width = 0, int height = 0)
    {
        ModLogger.DebugLog($"{methodName}: Stacks contents:\n{GetItemsMatrix(stacks, width, height)}");
    }

    /// <summary>
    /// Builds a CSV matrix representation of an <see cref="ITileEntityLootable"/>'s contents.
    /// The top-left cell contains the total slot count. The header row contains 1-based column
    /// numbers and the left column contains 1-based row numbers.
    /// Each data cell uses <see cref="ItemX.Info(ItemStack)"/> formatting.
    /// </summary>
    /// <param name="lootable">The lootable tile entity to inspect</param>
    /// <returns>A CSV-formatted string matrix of the container contents</returns>
    public static string GetLootContainerContents(ITileEntityLootable lootable)
    {
        if (lootable == null)
        {
            return "null lootable";
        }

        var containerSize = lootable.GetContainerSize();

        return GetItemsMatrix(lootable.items, containerSize.x, containerSize.y);
    }

    private static void GuessMatrixDimensions(int count, out int width, out int height)
    {
        int sqrtCount = (int)System.Math.Sqrt(count);

        // Try whole numbers <= sqrt(count) as divisors, largest first for squarest result
        for (int d = sqrtCount; d >= 2; d--)
        {
            if (count % d == 0)
            {
                height = d;
                width = count / d;
                return;
            }
        }

        // No valid divisor found: treat as a 1-dimensional matrix
        width = count;
        height = 1;
    }

    /// <summary>
    /// Builds a CSV matrix representation of a flat item array laid out as a grid.
    /// The top-left cell contains the total slot count. The header row contains 1-based column
    /// numbers and the left column contains 1-based row numbers.
    /// Each data cell uses <see cref="ItemX.Info(ItemStack)"/> formatting.
    /// Items are indexed row-major: slotIndex = row * width + column.
    /// </summary>
    /// <param name="items">The flat array of item stacks</param>
    /// <param name="width">Number of columns. Pass 0 to guess from item count.</param>
    /// <param name="height">Number of rows. Pass 0 to guess from item count.</param>
    /// <returns>A CSV-formatted string matrix</returns>
    public static string GetItemsMatrix(ItemStack[] items, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            GuessMatrixDimensions(items?.Length ?? 0, out width, out height);
        }

        int totalCells = width * height;

        var sb = new StringBuilder();

        // Header row: total cell count in top-left, then 1-based column numbers
        sb.Append(totalCells);
        for (int x = 1; x <= width; x++)
        {
            sb.Append(',').Append(x);
        }
        sb.AppendLine();

        // Data rows: 1-based row number in left column, then cell contents
        for (int y = 0; y < height; y++)
        {
            sb.Append(y + 1);

            for (int x = 0; x < width; x++)
            {
                int slotIndex = y * width + x;
                var stack = (items != null && slotIndex < items.Length) ? items[slotIndex] : null;
                sb.Append(',').Append(ItemX.Info(stack));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}