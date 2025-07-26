using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace BeyondStorage.Scripts.Utils;

public static class PatchUtil
{
    // Finds the index of the first occurrence of a sequence of instructions (subList) within
    // another sequence (mainList) using the default equality comparer.
    // Returns the zero-based index of the first instruction of the found sequence in the mainList,
    // or -1 if the sequence is not found.
    public static bool CodesMatch(CodeInstruction a, CodeInstruction b)
    {
        if (a == null || b == null)
        {
            return a == b;
        }

        // Check if the opcodes match
        if (a.opcode != b.opcode)
        {
            return false;
        }

        // If operands are null, they match
        if (a.operand == null && b.operand == null)
        {
            return true;
        }

        // If one operand is null and the other is not, they do not match
        if (a.operand == null || b.operand == null)
        {
            return false;
        }

        // Check if operands are equal
        return a.operand.Equals(b.operand);
    }

    static int IndexOf(List<CodeInstruction> mainList, List<CodeInstruction> subList, bool extraLogging = false)
    {
        return IndexOf(mainList, subList, 0, extraLogging);
    }

    // Finds the index of the first occurrence of a sequence of instructions (subList) within
    // another sequence (mainList) starting from a specified index, using the default equality comparer.
    // Returns the zero-based index of the first instruction of the found sequence in the mainList,
    // or -1 if the sequence is not found.
    public static int IndexOf(List<CodeInstruction> mainList, List<CodeInstruction> subList, int startIndex, bool extraLogging = false)
    {
        int mainCount = mainList.Count;
        int subCount = subList.Count;

        if (extraLogging)
        {
            LogUtil.DebugLog($"IndexOf: Searching for pattern of {subCount} instructions in list of {mainCount} instructions, starting at index {startIndex}");
            for (int i = 0; i < subList.Count; i++)
            {
                LogUtil.DebugLog($"IndexOf: Pattern[{i}] = {subList[i].opcode} {subList[i].operand}");
            }
        }

        // Early validation
        if (subCount == 0 || mainCount < subCount || startIndex < 0 || startIndex >= mainCount)
        {
            if (extraLogging)
            {
                LogUtil.DebugLog($"IndexOf: Early validation failed - subCount={subCount}, mainCount={mainCount}, startIndex={startIndex}");
            }
            return -1;
        }

        // Single element optimization
        if (subCount == 1)
        {
            if (extraLogging)
            {
                LogUtil.DebugLog("IndexOf: Using single element optimization");
            }

            var target = subList[0];
            for (int i = startIndex; i < mainCount; i++)
            {
                if (extraLogging)
                {
                    LogUtil.DebugLog($"IndexOf: Checking index {i}: {mainList[i].opcode} {mainList[i].operand} vs target {target.opcode} {target.operand}");
                }

                if (CodesMatch(mainList[i], target))
                {
                    if (extraLogging)
                    {
                        LogUtil.DebugLog($"IndexOf: Single element match found at index {i}");
                    }
                    return i;
                }
            }

            if (extraLogging)
            {
                LogUtil.DebugLog("IndexOf: Single element not found");
            }
            return -1;
        }

        // Two-element optimization (very common in IL patterns)
        if (subCount == 2)
        {
            if (extraLogging)
            {
                LogUtil.DebugLog("IndexOf: Using two element optimization");
            }

            var first = subList[0];
            var second = subList[1];
            for (int i = startIndex; i <= mainCount - 2; i++)
            {
                if (extraLogging)
                {
                    LogUtil.DebugLog($"IndexOf: Checking two-element match at index {i}: [{mainList[i].opcode} {mainList[i].operand}] [{mainList[i + 1].opcode} {mainList[i + 1].operand}]");
                }

                if (CodesMatch(mainList[i], first) && CodesMatch(mainList[i + 1], second))
                {
                    if (extraLogging)
                    {
                        LogUtil.DebugLog($"IndexOf: Two element match found at index {i}");
                    }
                    return i;
                }
            }

            if (extraLogging)
            {
                LogUtil.DebugLog("IndexOf: Two element pattern not found");
            }
            return -1;
        }

        // For longer patterns, use optimized search with skip table
        if (extraLogging)
        {
            LogUtil.DebugLog("IndexOf: Using skip table optimization for longer pattern");
        }
        return IndexOfWithSkipTable(mainList, subList, startIndex, false);  // extraLogging is false here to avoid excessive logging in the optimized path
    }

    // Boyer-Moore inspired algorithm with bad character skip table
    private static int IndexOfWithSkipTable(List<CodeInstruction> mainList, List<CodeInstruction> subList, int startIndex, bool extraLogging = false)
    {
        int mainCount = mainList.Count;
        int subCount = subList.Count;

        // Build skip table for last occurrence of each instruction in pattern
        var skipTable = new Dictionary<CodeInstruction, int>();
        for (int i = 0; i < subCount - 1; i++)
        {
            skipTable[subList[i]] = subCount - 1 - i;
        }

        if (extraLogging)
        {
            LogUtil.DebugLog($"IndexOf: Built skip table with {skipTable.Count} entries");
        }

        int pos = startIndex;
        int attempts = 0;
        while (pos <= mainCount - subCount)
        {
            attempts++;
            if (extraLogging)
            {
                LogUtil.DebugLog($"IndexOf: Attempt #{attempts} at position {pos}");
            }

            // Start matching from the end of the pattern
            int i = subCount - 1;
            while (i >= 0 && CodesMatch(mainList[pos + i], subList[i]))
            {
                if (extraLogging)
                {
                    LogUtil.DebugLog($"IndexOf: Match at pattern index {i}, instruction: {mainList[pos + i].opcode} {mainList[pos + i].operand}");
                }
                i--;
            }

            if (i < 0)
            {
                if (extraLogging)
                {
                    LogUtil.DebugLog($"IndexOf: Complete pattern match found at position {pos} after {attempts} attempts");
                }
                return pos; // Found match
            }

            // Calculate skip distance
            var badChar = mainList[pos + i];
            int skip = skipTable.TryGetValue(badChar, out int skipValue) ? skipValue : subCount;

            // Ensure we advance at least one position to avoid infinite loops
            int actualSkip = Math.Max(1, skip - (subCount - 1 - i));

            if (extraLogging)
            {
                LogUtil.DebugLog($"IndexOf: Mismatch at pattern index {i}, bad char: {badChar.opcode} {badChar.operand}, skip: {actualSkip}");
            }

            pos += actualSkip;
        }

        if (extraLogging)
        {
            LogUtil.DebugLog($"IndexOf: Pattern not found after {attempts} attempts");
        }
        return -1;
    }

    /// <summary>
    /// Inserts replacement instructions at the specified position in the target list.
    /// </summary>
    /// <param name="request">PatchRequest containing the instructions and replacement details</param>
    /// <param name="replacementPosition">The position where instructions should be inserted</param>
    /// <param name="replacementCount">The number of instructions to insert</param>
    /// <param name="originalMatchPosition">The original position where the pattern was found</param>
    private static void InsertInstructions(PatchRequest request, int replacementPosition, int replacementCount, int originalMatchPosition)
    {
        // Insert mode: add new instructions at the specified position
        request.NewInstructions.InsertRange(replacementPosition, request.ReplacementInstructions);

        if (request.ExtraLogging)
        {
            LogUtil.DebugLog($"Inserted {replacementCount} instructions at index {replacementPosition} (original match at {originalMatchPosition}) in {request.TargetMethodName}");
        }
    }

    /// <summary>
    /// Overwrites instructions in the target list with replacement instructions, preserving labels.
    /// </summary>
    /// <param name="request">PatchRequest containing the instructions and replacement details</param>
    /// <param name="replacementPosition">The starting position for overwriting</param>
    /// <param name="replacementCount">The number of instructions to replace</param>
    private static void OverwriteInstructions(PatchRequest request, int replacementPosition, int replacementCount)
    {
        for (int i = 0; i < replacementCount; i++)
        {
            var newInstruction = request.ReplacementInstructions[i];
            var targetIndex = replacementPosition + i;

            // Preserve labels from the original instruction if it exists
            if (targetIndex < request.NewInstructions.Count)
            {
                var originalInstruction = request.NewInstructions[targetIndex];
                if (originalInstruction.labels.Count > 0)
                {
                    newInstruction = newInstruction.Clone();
                    foreach (var label in originalInstruction.labels)
                    {
                        newInstruction.labels.Add(label);
                    }
                }
            }

            request.NewInstructions[targetIndex] = newInstruction;
        }

        if (request.ExtraLogging)
        {
            LogUtil.DebugLog($"Overwrote {replacementCount} instructions starting at index {replacementPosition} in {request.TargetMethodName}");
        }
    }

    /// <summary>
    /// Generic patch method that finds instruction patterns and applies replacements.
    /// Can insert new instructions or overwrite existing ones.
    /// </summary>
    /// <param name="request">PatchRequest containing all patch parameters</param>
    /// <returns>PatchResults indicating if any patches were applied</returns>
    public static PatchResponse ApplyPatches(PatchRequest request)
    {
        LogUtil.Info($"Transpiling {request.TargetMethodName}");

        int searchIndex = 0;
        var response = new PatchResponse();

        while (searchIndex < request.NewInstructions.Count)
        {
            if (request.MaxPatches > 0 && response.Count >= request.MaxPatches)
            {
                LogUtil.DebugLog($"Reached maximum patches ({request.MaxPatches}) for {request.TargetMethodName}. Stopping further patches.");
                break;
            }

            int matchIndex = IndexOf(request.NewInstructions, request.SearchPattern, searchIndex, request.ExtraLogging);
            if (matchIndex < 0)
            {
                // No more matches found
                break;
            }

            LogUtil.DebugLog($"Found patch point at index {matchIndex} in {request.TargetMethodName}");

            // Calculate the actual replacement position
            int replacementPosition = matchIndex + request.ReplacementOffset;
            int replacementCount = request.ReplacementInstructions.Count;

            // Safety checks
            if (replacementPosition < request.MinimumSafetyOffset)
            {
                if (request.ExtraLogging)
                {
                    LogUtil.DebugLog($"Replacement position {replacementPosition} is below minimum safety offset {request.MinimumSafetyOffset}. Skipping patch of {request.TargetMethodName}");
                }
                searchIndex = matchIndex + 1;
                continue;
            }

            if (replacementPosition < 0 || replacementPosition > request.NewInstructions.Count)
            {
                if (request.ExtraLogging)
                {
                    LogUtil.DebugLog($"Replacement position {replacementPosition} is out of bounds. Skipping patch of {request.TargetMethodName}");
                }

                searchIndex = matchIndex + 1;
                continue;
            }

            if (!request.IsInsertMode && replacementPosition + replacementCount > request.NewInstructions.Count)
            {
                if (request.ExtraLogging)
                {
                    LogUtil.DebugLog($"Not enough instructions to overwrite at position {replacementPosition}. Skipping patch of {request.TargetMethodName}");
                }

                searchIndex = matchIndex + 1;
                continue;
            }

            // Apply the patch
            if (request.IsInsertMode)
            {
                InsertInstructions(request, replacementPosition, replacementCount, matchIndex);
                searchIndex = replacementPosition + replacementCount + 1;
            }
            else
            {
                OverwriteInstructions(request, replacementPosition, replacementCount);
                searchIndex = replacementPosition + replacementCount;
            }

            response.RegisterPatch(replacementPosition, matchIndex);
            LogUtil.DebugLog($"Applied {request.TargetMethodName} patch #{response.Count} at index {replacementPosition} (original match at {matchIndex})");
        }

        if (response.Count > 0)
        {
            LogUtil.Info($"Successfully patched {request.TargetMethodName} in {response.Count} places");
        }
        else
        {
            LogUtil.Warning($"No patches applied to {request.TargetMethodName}");
        }

        return response;
    }

    public class PatchRequest
    {
        private List<CodeInstruction> _originalInstructions;
        private List<CodeInstruction> _newInstructions;

        /// <summary>
        /// The original IL instructions to patch.
        /// </summary>
        public List<CodeInstruction> OriginalInstructions
        {
            get => _originalInstructions;
            set
            {
                _originalInstructions = value;
                // Default NewInstructions to a copy of OriginalInstructions for safety
                _newInstructions = value?.ToList() ?? [];
            }
        }

        /// <summary>
        /// The resulting patched instructions (defaults to OriginalInstructions for safety).
        /// Will be populated by ApplyPatches, but falls back to original if patching fails.
        /// </summary>
        public List<CodeInstruction> NewInstructions
        {
            get => _newInstructions ?? _originalInstructions?.ToList() ?? [];
            set => _newInstructions = value;
        }

        /// <summary>
        /// The pattern of instructions to search for.
        /// </summary>
        public List<CodeInstruction> SearchPattern { get; set; }

        /// <summary>
        /// The instructions to use as replacement.
        /// </summary>
        public List<CodeInstruction> ReplacementInstructions { get; set; }

        /// <summary>
        /// The name of the method being patched (for logging).
        /// </summary>
        public string TargetMethodName { get; set; }

        /// <summary>
        /// Offset from match start where replacement begins (can be negative).
        /// </summary>
        public int ReplacementOffset { get; set; } = 0;

        /// <summary>
        /// If true, insert instructions; if false, overwrite existing instructions.
        /// </summary>
        public bool IsInsertMode { get; set; } = false;

        /// <summary>
        /// Maximum number of patches to apply (0 = unlimited).
        /// </summary>
        public int MaxPatches { get; set; } = 1;

        /// <summary>
        /// Minimum number of instructions required before the match for safe patching.
        /// </summary>
        public int MinimumSafetyOffset { get; set; } = 0;

        /// <summary>
        /// Enable extra detailed logging for debugging.
        /// </summary>
        public bool ExtraLogging { get; set; } = false;
    }

    public class PatchResponse
    {
        /// <summary>
        /// Indicates whether any patches were applied.
        /// </summary>
        public bool IsPatched { get { return Count > 0; } }

        /// <summary>
        /// The number of patches that were successfully applied.
        /// </summary>
        public int Count { get; set; } = 0;

        /// <summary>
        /// The list of positions (indices) where patches were applied.
        /// </summary>
        public List<int> Positions { get; set; } = [];

        /// <summary>
        /// The list of original positions (indices) where matches were found.
        /// </summary>
        public List<int> OriginalPositions { get; set; } = [];

        public PatchResponse() { }

        /// <summary>
        /// Adds a patch record with the replacement position and original match position.
        /// Increments the patch count automatically.
        /// </summary>
        /// <param name="replacementPosition">The position where the patch was applied</param>
        /// <param name="originalPosition">The original position where the match was found</param>
        public void RegisterPatch(int replacementPosition, int originalPosition)
        {
            Positions.Add(replacementPosition);
            OriginalPositions.Add(originalPosition);
            Count++;
        }

        /// <summary>
        /// Returns the best available instructions: NewInstructions if patches were applied, 
        /// otherwise OriginalInstructions.
        /// </summary>
        /// <param name="request">The PatchRequest containing the instruction sets</param>
        /// <returns>The most appropriate instruction list based on patch success</returns>
        public List<CodeInstruction> BestInstructions(PatchRequest request)
        {
            return IsPatched ? request.NewInstructions : request.OriginalInstructions;
        }
    }
}