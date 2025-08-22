using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.Scripts.Harmony;

public static class ILCodeMatcher
{
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
        return OperandsMatch(a.operand, b.operand);
    }

    private static void LogCodesMatchScenario(CodeInstruction a, CodeInstruction b, string scenario)
    {
        const string d_methodName = nameof(LogCodesMatchScenario);

        if (a == null || b == null)
        {
            ModLogger.DebugLog($"{d_methodName}: {scenario} - One of the instructions is null: a={a}, b={b}");
            return;
        }

        // Check if the opcodes match
        if (a.opcode != b.opcode)
        {
            ModLogger.DebugLog($"{d_methodName}: {scenario} - Opcodes do not match: a={a.opcode}, b={b.opcode}");
            return;
        }

        // If operands are null, they match
        if (a.operand == null && b.operand == null)
        {
            ModLogger.DebugLog($"{d_methodName}: {scenario} - Both operands are null, they match");
            return;
        }

        // If one operand is null and the other is not, they do not match
        if (a.operand == null || b.operand == null)
        {
            ModLogger.DebugLog($"{d_methodName}: {scenario} - One operand is null: a={a.operand}, b={b.operand}");
            return;
        }

        // Check if operands are equal
        var operandsMatch = OperandsMatch(a.operand, b.operand);
        ModLogger.DebugLog($"{d_methodName}: {scenario} - OperandsMatch is {operandsMatch}");
    }

    private static bool OperandsMatch(object a, object b)
    {
        // Fast path for exact equality
        if (a.Equals(b))
        {
            return true;
        }

        // Handle numeric operand matching (local variable indices)
        var aIndex = GetNumericValue(a);
        var bIndex = GetNumericValue(b);

        if (aIndex.HasValue && bIndex.HasValue)
        {
            return aIndex.Value == bIndex.Value;
        }

        // Handle Label matching
        if (a is Label labelA && b is Label labelB)
        {
            return labelA.Equals(labelB);
        }

        return false;
    }

    private static int? GetNumericValue(object operand)
    {
        return operand switch
        {
            LocalBuilder lb => lb.LocalIndex,
            int i => i,
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            _ => null
        };
    }

    // Finds the index of the first occurrence of a sequence of instructions (subList) within
    // another sequence (mainList) using the default equality comparer.
    // Returns the zero-based index of the first instruction of the found sequence in the mainList,
    // or -1 if the sequence is not found.
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
            ModLogger.DebugLog($"IndexOf: Searching for pattern of {subCount} instructions in list of {mainCount} instructions, starting at index {startIndex}");
            for (int i = 0; i < subList.Count; i++)
            {
                ModLogger.DebugLog($"IndexOf: Pattern[{i}] = {subList[i].opcode} {subList[i].operand}");
            }
        }

        // Early validation
        if (subCount == 0 || mainCount < subCount || startIndex < 0 || startIndex >= mainCount)
        {
            if (extraLogging)
            {
                ModLogger.DebugLog($"IndexOf: Early validation failed - subCount={subCount}, mainCount={mainCount}, startIndex={startIndex}");
            }
            return -1;
        }

        // Single element optimization
        if (subCount == 1)
        {
            if (extraLogging)
            {
                ModLogger.DebugLog("IndexOf: Using single element optimization");
            }

            var target = subList[0];
            for (int i = startIndex; i < mainCount; i++)
            {
                if (extraLogging)
                {
                    ModLogger.DebugLog($"IndexOf: Checking index {i}: {mainList[i].opcode} {mainList[i].operand} vs target {target.opcode} {target.operand}");
                }

                if (CodesMatch(mainList[i], target))
                {
                    if (extraLogging)
                    {
                        ModLogger.DebugLog($"IndexOf: Single element match found at index {i}");
                    }
                    return i;
                }
            }

            if (extraLogging)
            {
                ModLogger.DebugLog("IndexOf: Single element not found");
            }
            return -1;
        }

        // Two-element optimization (very common in IL patterns)
        if (subCount == 2)
        {
            if (extraLogging)
            {
                ModLogger.DebugLog("IndexOf: Using two element optimization");
            }

            var first = subList[0];
            var second = subList[1];
            for (int i = startIndex; i <= mainCount - 2; i++)
            {
                if (extraLogging)
                {
                    ModLogger.DebugLog($"IndexOf: Checking two-element match at index {i}: [{mainList[i].opcode} {mainList[i].operand}] [{mainList[i + 1].opcode} {mainList[i + 1].operand}]");
                }

                var codesMatchFirst = CodesMatch(mainList[i], first);
                var codesMatchSecond = CodesMatch(mainList[i + 1], second);
                if (codesMatchFirst && codesMatchSecond)
                {
                    if (extraLogging)
                    {
                        ModLogger.DebugLog($"IndexOf: Two element match found at index {i}");
                    }
                    return i;
                }
            }

            if (extraLogging)
            {
                ModLogger.DebugLog("IndexOf: Two element pattern not found");
            }
            return -1;
        }

        // For longer patterns, use optimized search with skip table
        if (extraLogging)
        {
            ModLogger.DebugLog("IndexOf: Using skip table optimization for longer pattern");
        }
        return IndexOfWithSkipTable(mainList, subList, startIndex, extraLogging);
    }


    // Boyer-Moore inspired algorithm with bad character skip table
    private static int IndexOfWithSkipTable(List<CodeInstruction> mainList, List<CodeInstruction> subList, int startIndex, bool extraLogging = false)
    {
        int mainCount = mainList.Count;
        int subCount = subList.Count;

        // Build skip table and search for pattern
        var skipTable = BuildSkipTable(subList, extraLogging);
        return SearchWithSkipTable(mainList, subList, skipTable, startIndex, mainCount, subCount, extraLogging);
    }

    private static Dictionary<CodeInstruction, int> BuildSkipTable(List<CodeInstruction> subList, bool extraLogging)
    {
        var comparer = new CodeInstructionEqualityComparer();
        var skipTable = new Dictionary<CodeInstruction, int>(comparer);
        int subCount = subList.Count;

        for (int i = 0; i < subCount - 1; i++)
        {
            skipTable[subList[i]] = subCount - 1 - i;
        }

        LogSkipTableInfo(skipTable, extraLogging);
        return skipTable;
    }

    private static int SearchWithSkipTable(List<CodeInstruction> mainList, List<CodeInstruction> subList,
        Dictionary<CodeInstruction, int> skipTable, int startIndex, int mainCount, int subCount, bool extraLogging)
    {
        int pos = startIndex;
        int attempts = 0;

        while (pos <= mainCount - subCount)
        {
            attempts++;
            LogSearchAttempt(pos, attempts, extraLogging);

            int matchResult = TryMatchAtPosition(mainList, subList, pos, subCount, extraLogging);

            if (matchResult == -1) // Complete match found
            {
                LogMatchFound(pos, attempts, extraLogging);
                return pos;
            }

            // Calculate and apply skip
            pos += CalculateSkip(mainList[pos + matchResult], skipTable, subCount, matchResult, extraLogging);
        }

        LogSearchComplete(attempts, extraLogging);
        return -1;
    }

    private static int TryMatchAtPosition(List<CodeInstruction> mainList, List<CodeInstruction> subList, int pos, int subCount, bool extraLogging)
    {
        int i = subCount - 1;
        while (i >= 0 && CodesMatch(mainList[pos + i], subList[i]))
        {
            LogMatchProgress(i, mainList[pos + i], extraLogging);
            i--;
        }
        return i; // Returns -1 if complete match, otherwise index of first mismatch
    }

    private static int CalculateSkip(CodeInstruction badChar, Dictionary<CodeInstruction, int> skipTable, int subCount, int mismatchIndex, bool extraLogging)
    {
        int skip = skipTable.TryGetValue(badChar, out int skipValue) ? skipValue : subCount;
        int actualSkip = Math.Max(1, skip - (subCount - 1 - mismatchIndex));

        LogSkipCalculation(badChar, actualSkip, extraLogging);
        return actualSkip;
    }

    // Logging helper methods to reduce complexity
    private static void LogSkipTableInfo(Dictionary<CodeInstruction, int> skipTable, bool extraLogging)
    {
        if (extraLogging)
        {
            ModLogger.DebugLog($"IndexOf: Built skip table with {skipTable.Count} entries using custom comparer");
        }
    }

    private static void LogSearchAttempt(int pos, int attempts, bool extraLogging)
    {
        if (extraLogging)
        {
            ModLogger.DebugLog($"IndexOf: Attempt #{attempts} at position {pos}");
        }
    }

    private static void LogMatchProgress(int i, CodeInstruction instruction, bool extraLogging)
    {
        if (extraLogging)
        {
            ModLogger.DebugLog($"IndexOf: Match at pattern index {i}, instruction: {instruction.opcode} {instruction.operand}");
        }
    }

    private static void LogMatchFound(int pos, int attempts, bool extraLogging)
    {
        if (extraLogging)
        {
            ModLogger.DebugLog($"IndexOf: Complete pattern match found at position {pos} after {attempts} attempts");
        }
    }

    private static void LogSkipCalculation(CodeInstruction badChar, int actualSkip, bool extraLogging)
    {
        if (extraLogging)
        {
            ModLogger.DebugLog($"IndexOf: Mismatch, bad char: {badChar.opcode} {badChar.operand}, skip: {actualSkip}");
        }
    }

    private static void LogSearchComplete(int attempts, bool extraLogging)
    {
        if (extraLogging)
        {
            ModLogger.DebugLog($"IndexOf: Pattern not found after {attempts} attempts");
        }
    }

    private class CodeInstructionEqualityComparer : IEqualityComparer<CodeInstruction>
    {
        public bool Equals(CodeInstruction x, CodeInstruction y)
        {
            return CodesMatch(x, y);
        }

        public int GetHashCode(CodeInstruction obj)
        {
            if (obj == null)
            {
                return 0;
            }

            var numericValue = GetNumericValue(obj.operand);
            if (numericValue.HasValue)
            {
                return Tuple.Create(obj.opcode, numericValue.Value).GetHashCode();
            }
            else if (obj.operand != null)
            {
                return Tuple.Create(obj.opcode, obj.operand.GetType(), obj.operand.ToString()).GetHashCode();
            }
            else
            {
                return obj.opcode.GetHashCode();
            }
        }
    }
}