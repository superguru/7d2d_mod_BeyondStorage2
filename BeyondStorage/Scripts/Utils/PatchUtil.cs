using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace BeyondStorage.Scripts.Utils;

public static class PatchUtil
{
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
    /// If the replacement list is longer than available instructions, appends the remaining ones.
    /// </summary>
    /// <param name="request">PatchRequest containing the instructions and replacement details</param>
    /// <param name="replacementPosition">The starting position for overwriting</param>
    /// <param name="replacementCount">The number of instructions to replace</param>
    /// <param name="instructionsToAppend">Number of instructions that were appended beyond the available space</param>
    private static void OverwriteInstructions(PatchRequest request, int replacementPosition, int replacementCount)
    {
        int availableInstructions = request.NewInstructions.Count - replacementPosition;
        int instructionsToOverwrite = Math.Min(replacementCount, availableInstructions);
        int instructionsToAppend = Math.Max(0, replacementCount - availableInstructions);

        // Overwrite existing instructions
        for (int i = 0; i < instructionsToOverwrite; i++)
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

        // Append remaining replacement instructions if any
        if (instructionsToAppend > 0)
        {
            var instructionsToAdd = request.ReplacementInstructions
                .Skip(instructionsToOverwrite)
                .Take(instructionsToAppend)
                .ToList();

            request.NewInstructions.AddRange(instructionsToAdd);

            if (request.ExtraLogging)
            {
                LogUtil.DebugLog($"Appended {instructionsToAppend} additional instructions to end of method in {request.TargetMethodName}");
            }
        }

        if (request.ExtraLogging)
        {
            LogUtil.DebugLog($"Overwrote {instructionsToOverwrite} instructions starting at index {replacementPosition}" +
                            (instructionsToAppend > 0 ? $" and appended {instructionsToAppend} additional instructions" : "") +
                            $" in {request.TargetMethodName}");
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

            int matchIndex = CodesUtil.IndexOf(request.NewInstructions, request.SearchPattern, searchIndex, request.ExtraLogging);
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

            // Apply the patch
            if (request.IsInsertMode)
            {
                InsertInstructions(request, replacementPosition, replacementCount, matchIndex);
                response.RegisterPatch(replacementPosition, matchIndex);
                searchIndex = replacementPosition + replacementCount + 1;
            }
            else
            {
                OverwriteInstructions(request, replacementPosition, replacementCount);
                response.RegisterPatch(replacementPosition, matchIndex);
                searchIndex = replacementPosition + replacementCount;
            }

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