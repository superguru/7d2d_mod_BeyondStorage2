using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.Scripts.Harmony;

public static class ILPatchEngine
{
    /// <summary>
    /// Formats an IL position as hex string in the format "IL_ffff"
    /// </summary>
    /// <param name="position">The position to format</param>
    /// <returns>Formatted hex string</returns>
    private static string FormatILPosition(int position)
    {
        return $"IL_{position:x4}";
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
            ModLogger.DebugLog($"Inserted {replacementCount} instructions at {FormatILPosition(replacementPosition)} (original match at {FormatILPosition(originalMatchPosition)}) in {request.TargetMethodName}");
        }
    }

    /// <summary>
    /// Overwrites instructions in the target list with replacement instructions, preserving labels.
    /// If the replacement list is longer than available instructions, appends the remaining ones.
    /// </summary>
    /// <param name="request">PatchRequest containing the instructions and replacement details</param>
    /// <param name="replacementPosition">The starting position for overwriting</param>
    /// <param name="replacementCount">The number of instructions to replace</param>
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
                ModLogger.DebugLog($"Appended {instructionsToAppend} additional instructions to end of method in {request.TargetMethodName}");
            }
        }

        if (request.ExtraLogging)
        {
            ModLogger.DebugLog($"Overwrote {instructionsToOverwrite} instructions starting at {FormatILPosition(replacementPosition)}" +
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
        ModLogger.Info($"Transpiling {request.TargetMethodName}");

        int searchIndex = 0;
        var response = new PatchResponse();

        while (searchIndex < request.NewInstructions.Count)
        {
            if (ShouldStopPatching(request, response))
            {
                break;
            }

            int matchIndex = ILCodeMatcher.IndexOf(request.NewInstructions, request.SearchPattern, searchIndex, request.ExtraLogging);
            if (matchIndex < 0)
            {
                break; // No more matches found
            }

            ModLogger.DebugLog($"Found patch point at {FormatILPosition(matchIndex)} in {request.TargetMethodName}");

            var patchResult = TryApplyPatch(request, response, matchIndex);
            if (patchResult.success)
            {
                searchIndex = patchResult.nextSearchIndex;
                ModLogger.DebugLog($"Applied {request.TargetMethodName} patch #{response.Count} at {FormatILPosition(patchResult.replacementPosition)} (original match at {FormatILPosition(matchIndex)})");
            }
            else
            {
                searchIndex = matchIndex + 1; // Move past failed match
            }
        }

        LogPatchingResults(request, response);
        return response;
    }

    private static bool ShouldStopPatching(PatchRequest request, PatchResponse response)
    {
        if (request.MaxPatches > 0 && response.Count >= request.MaxPatches)
        {
            ModLogger.DebugLog($"Reached maximum patches ({request.MaxPatches}) for {request.TargetMethodName}. Stopping further patches.");
            return true;
        }
        return false;
    }

    private static (bool success, int replacementPosition, int nextSearchIndex) TryApplyPatch(PatchRequest request, PatchResponse response, int matchIndex)
    {
        int replacementPosition = matchIndex + request.ReplacementOffset;
        int replacementCount = request.ReplacementInstructions.Count;

        var validation = ValidatePatchPosition(request, replacementPosition);
        if (!validation.isValid)
        {
            LogValidationFailure(request, replacementPosition, validation.reason);
            return (false, replacementPosition, matchIndex + 1); // ✅ Advance past failed match
        }

        // Apply the appropriate patch type
        ApplyPatchByMode(request, response, replacementPosition, replacementCount, matchIndex);

        int nextSearchIndex = request.IsInsertMode
            ? replacementPosition + replacementCount + 1
            : replacementPosition + replacementCount;

        return (true, replacementPosition, nextSearchIndex);
    }

    private static (bool isValid, string reason) ValidatePatchPosition(PatchRequest request, int replacementPosition)
    {
        if (replacementPosition < request.MinimumSafetyOffset)
        {
            return (false, $"below minimum safety offset {request.MinimumSafetyOffset}");
        }

        if (replacementPosition < 0)
        {
            return (false, "negative position");
        }

        // Mode-specific validation
        if (request.IsInsertMode)
        {
            if (replacementPosition > request.NewInstructions.Count)
            {
                return (false, "insert position out of bounds");
            }
        }
        else // Overwrite mode
        {
            if (replacementPosition >= request.NewInstructions.Count)
            {
                return (false, "overwrite position out of bounds");
            }
        }

        return (true, null);
    }

    private static void LogValidationFailure(PatchRequest request, int replacementPosition, string reason)
    {
        if (request.ExtraLogging)
        {
            ModLogger.DebugLog($"Replacement position {FormatILPosition(replacementPosition)} is {reason}. Skipping patch of {request.TargetMethodName}");
        }
    }

    private static void ApplyPatchByMode(PatchRequest request, PatchResponse response, int replacementPosition, int replacementCount, int matchIndex)
    {
        if (request.IsInsertMode)
        {
            InsertInstructions(request, replacementPosition, replacementCount, matchIndex);
        }
        else
        {
            OverwriteInstructions(request, replacementPosition, replacementCount);
        }

        response.RegisterPatch(replacementPosition, matchIndex);
    }

    private static void LogPatchingResults(PatchRequest request, PatchResponse response)
    {
        if (response.Count > 0)
        {
            ModLogger.Info($"Successfully patched {request.TargetMethodName} in {response.Count} places");

            // Log detailed position information for successful patches
            if (request.ExtraLogging && response.Positions.Count > 0)
            {
                var positionDetails = response.Positions
                    .Select((pos, index) => $"{FormatILPosition(pos)} (match: {FormatILPosition(response.OriginalPositions[index])})")
                    .ToArray();
                ModLogger.DebugLog($"Patch positions for {request.TargetMethodName}: {string.Join(", ", positionDetails)}");
            }
        }
        else
        {
            ModLogger.Warning($"No patches applied to {request.TargetMethodName}");
        }
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
            get
            {
                if (_newInstructions == null)
                {
                    _newInstructions = _originalInstructions?.ToList() ?? [];
                }
                return _newInstructions;
            }
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

        /// <summary>
        /// Gets a formatted string of all patch positions in hex format
        /// </summary>
        /// <returns>Comma-separated list of hex-formatted positions</returns>
        public string GetFormattedPositions()
        {
            return string.Join(", ", Positions.Select(pos => FormatILPosition(pos)));
        }

        /// <summary>
        /// Gets a formatted string of all original match positions in hex format
        /// </summary>
        /// <returns>Comma-separated list of hex-formatted original positions</returns>
        public string GetFormattedOriginalPositions()
        {
            return string.Join(", ", OriginalPositions.Select(pos => FormatILPosition(pos)));
        }
    }
}