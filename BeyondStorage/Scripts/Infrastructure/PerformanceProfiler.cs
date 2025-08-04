using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BeyondStorage.Scripts.Infrastructure;

/// <summary>
/// Thread-safe class for tracking method call statistics including call count, total time, and average time.
/// Can be used by any class that needs to track performance metrics for method calls.
/// Uses explicit call recording for clarity and accuracy with nanosecond precision.
/// Includes internal stopwatch management for convenience.
/// </summary>
public sealed class PerformanceProfiler
{
    private readonly Dictionary<string, (int callCount, long totalTimeNs, double avgTimeNs)> _callStats = new();
    private readonly Dictionary<string, Stopwatch> _activeStopwatches = new();
    private readonly object _statsLock = new object();
    private readonly string _trackerName;

    // Cache the conversion factor from ticks to nanoseconds for performance
    private static readonly double s_ticksToNanoseconds = 1_000_000_000.0 / Stopwatch.Frequency;

    /// <summary>
    /// Initializes a new instance of PerformanceProfiler.
    /// </summary>
    /// <param name="trackerName">Name of the tracker for logging purposes</param>
    public PerformanceProfiler(string trackerName)
    {
        _trackerName = trackerName ?? throw new ArgumentNullException(nameof(trackerName));
    }

    /// <summary>
    /// Starts timing for a method. Must be paired with StopAndRecordCall.
    /// </summary>
    /// <param name="methodName">Name of the method being timed</param>
    /// <returns>True if timing started successfully, false if already timing this method</returns>
    public bool StartTiming(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        }

        lock (_statsLock)
        {
            if (_activeStopwatches.ContainsKey(methodName))
            {
                return false; // Already timing this method
            }

            _activeStopwatches[methodName] = Stopwatch.StartNew();
            return true;
        }
    }

    /// <summary>
    /// Stops timing and records the call for a method. Must be paired with StartTiming.
    /// </summary>
    /// <param name="methodName">Name of the method that was being timed</param>
    /// <returns>The elapsed time in nanoseconds, or -1 if method wasn't being timed</returns>
    public long StopAndRecordCall(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        }

        lock (_statsLock)
        {
            if (!_activeStopwatches.TryGetValue(methodName, out var stopwatch))
            {
                return -1; // Method wasn't being timed
            }

            stopwatch.Stop();
            _activeStopwatches.Remove(methodName);

            var elapsedNs = (long)(stopwatch.ElapsedTicks * s_ticksToNanoseconds);
            RecordCallInternal(methodName, elapsedNs);

            return elapsedNs;
        }
    }

    /// <summary>
    /// Gets the current elapsed time for a method being timed, without stopping the timer.
    /// </summary>
    /// <param name="methodName">Name of the method being timed</param>
    /// <returns>Current elapsed time in nanoseconds, or -1 if method isn't being timed</returns>
    public long GetCurrentElapsed(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return -1;
        }

        lock (_statsLock)
        {
            if (_activeStopwatches.TryGetValue(methodName, out var stopwatch))
            {
                return (long)(stopwatch.ElapsedTicks * s_ticksToNanoseconds);
            }
            return -1;
        }
    }

    /// <summary>
    /// Checks if a method is currently being timed.
    /// </summary>
    /// <param name="methodName">Name of the method to check</param>
    /// <returns>True if the method is currently being timed</returns>
    public bool IsTimingActive(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return false;
        }

        lock (_statsLock)
        {
            return _activeStopwatches.ContainsKey(methodName);
        }
    }

    /// <summary>
    /// Records a method call with its execution time in nanoseconds.
    /// </summary>
    /// <param name="methodName">Name of the method that was called</param>
    /// <param name="elapsedNs">Execution time in nanoseconds</param>
    public void RecordCall(string methodName, long elapsedNs)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        }

        lock (_statsLock)
        {
            RecordCallInternal(methodName, elapsedNs);
        }
    }

    /// <summary>
    /// Records a method call with its execution time in milliseconds (for backward compatibility).
    /// </summary>
    /// <param name="methodName">Name of the method that was called</param>
    /// <param name="elapsedMs">Execution time in milliseconds</param>
    public void RecordCallMs(string methodName, long elapsedMs)
    {
        RecordCall(methodName, elapsedMs * 1_000_000); // Convert ms to ns
    }

    /// <summary>
    /// Records a method call using a stopwatch with nanosecond precision.
    /// </summary>
    /// <param name="methodName">Name of the method that was called</param>
    /// <param name="stopwatch">Stopwatch that was used to time the method</param>
    public void RecordCall(string methodName, Stopwatch stopwatch)
    {
        if (stopwatch == null)
        {
            throw new ArgumentNullException(nameof(stopwatch));
        }

        // Convert ticks to nanoseconds for maximum precision
        var elapsedNs = (long)(stopwatch.ElapsedTicks * s_ticksToNanoseconds);
        RecordCall(methodName, elapsedNs);
    }

    private void RecordCallInternal(string methodName, long elapsedNs)
    {
        if (_callStats.TryGetValue(methodName, out var stats))
        {
            var newCallCount = stats.callCount + 1;
            var newTotalTime = stats.totalTimeNs + elapsedNs;
            var newAvgTime = (double)newTotalTime / newCallCount;

            _callStats[methodName] = (newCallCount, newTotalTime, newAvgTime);
        }
        else
        {
            _callStats[methodName] = (1, elapsedNs, (double)elapsedNs);
        }
    }

    /// <summary>
    /// Gets statistics for a specific method with nanosecond precision.
    /// </summary>
    /// <param name="methodName">Name of the method</param>
    /// <returns>Statistics tuple or null if method not found</returns>
    public (int callCount, long totalTimeNs, double avgTimeNs)? GetMethodStats(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        lock (_statsLock)
        {
            if (_callStats.TryGetValue(methodName, out var stats))
            {
                return stats;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets statistics for a specific method in milliseconds (for backward compatibility).
    /// </summary>
    /// <param name="methodName">Name of the method</param>
    /// <returns>Statistics tuple or null if method not found</returns>
    public (int callCount, long totalTimeMs, double avgTimeMs)? GetMethodStatsMs(string methodName)
    {
        var stats = GetMethodStats(methodName);
        if (stats.HasValue)
        {
            var (callCount, totalTimeNs, avgTimeNs) = stats.Value;
            return (callCount, totalTimeNs / 1_000_000, avgTimeNs / 1_000_000.0);
        }
        return null;
    }

    /// <summary>
    /// Gets all call statistics with nanosecond precision.
    /// </summary>
    /// <returns>Dictionary of method names and their timing statistics</returns>
    public Dictionary<string, (int callCount, long totalTimeNs, double avgTimeNs)> GetAllStatistics()
    {
        lock (_statsLock)
        {
            return new Dictionary<string, (int, long, double)>(_callStats);
        }
    }

    /// <summary>
    /// Gets all call statistics in milliseconds (for backward compatibility).
    /// </summary>
    /// <returns>Dictionary of method names and their timing statistics in milliseconds</returns>
    public Dictionary<string, (int callCount, long totalTimeMs, double avgTimeMs)> GetAllStatisticsMs()
    {
        lock (_statsLock)
        {
            var result = new Dictionary<string, (int, long, double)>();
            foreach (var kvp in _callStats)
            {
                var (callCount, totalTimeNs, avgTimeNs) = kvp.Value;
                result[kvp.Key] = (callCount, totalTimeNs / 1_000_000, avgTimeNs / 1_000_000.0);
            }
            return result;
        }
    }

    /// <summary>
    /// Gets formatted call statistics for logging/debugging with intelligent unit selection.
    /// </summary>
    /// <returns>Formatted string with call statistics</returns>
    public string GetFormattedStatistics()
    {
        lock (_statsLock)
        {
            if (_callStats.Count == 0)
            {
                return $"{_trackerName}: No calls recorded";
            }

            var stats = new List<string>();
            foreach (var kvp in _callStats.OrderByDescending(x => x.Value.callCount))
            {
                var method = kvp.Key;
                var (callCount, totalTimeNs, avgTimeNs) = kvp.Value;

                // Intelligently choose units based on magnitude
                var (avgDisplay, totalDisplay) = FormatTime(avgTimeNs, totalTimeNs);
                stats.Add($"{method}: {callCount} calls, avg {avgDisplay}, total {totalDisplay}");
            }

            return $"{_trackerName} Stats: [{string.Join(", ", stats)}]";
        }
    }

    /// <summary>
    /// Formats time values with appropriate units (ns, μs, ms, s).
    /// </summary>
    private static (string avg, string total) FormatTime(double avgTimeNs, long totalTimeNs)
    {
        string avgDisplay, totalDisplay;

        // Format average time
        if (avgTimeNs < 1_000) // Less than 1 microsecond
        {
            avgDisplay = $"{avgTimeNs:F3}ns";
        }
        else if (avgTimeNs < 1_000_000) // Less than 1 millisecond
        {
            avgDisplay = $"{avgTimeNs / 1_000.0:F3}μs";
        }
        else if (avgTimeNs < 1_000_000_000) // Less than 1 second
        {
            avgDisplay = $"{avgTimeNs / 1_000_000.0:F3}ms";
        }
        else
        {
            avgDisplay = $"{avgTimeNs / 1_000_000_000.0:F3}s";
        }

        // Format total time
        if (totalTimeNs < 1_000) // Less than 1 microsecond
        {
            totalDisplay = $"{totalTimeNs}ns";
        }
        else if (totalTimeNs < 1_000_000) // Less than 1 millisecond
        {
            totalDisplay = $"{totalTimeNs / 1_000.0:F3}μs";
        }
        else if (totalTimeNs < 1_000_000_000) // Less than 1 second
        {
            totalDisplay = $"{totalTimeNs / 1_000_000.0:F3}ms";
        }
        else
        {
            totalDisplay = $"{totalTimeNs / 1_000_000_000.0:F3}s";
        }

        return (avgDisplay, totalDisplay);
    }

    /// <summary>
    /// Formats a single nanosecond value with appropriate units for readability.
    /// </summary>
    /// <param name="nanoseconds">Time in nanoseconds</param>
    /// <returns>Formatted string with appropriate unit</returns>
    public static string FormatNanoseconds(double nanoseconds)
    {
        if (nanoseconds < 1_000) // Less than 1 microsecond
        {
            return $"{nanoseconds:F3}ns";
        }
        else if (nanoseconds < 1_000_000) // Less than 1 millisecond
        {
            return $"{nanoseconds / 1_000.0:F3}μs";
        }
        else if (nanoseconds < 1_000_000_000) // Less than 1 second
        {
            return $"{nanoseconds / 1_000_000.0:F3}ms";
        }
        else
        {
            return $"{nanoseconds / 1_000_000_000.0:F3}s";
        }
    }

    /// <summary>
    /// Converts stopwatch ticks to nanoseconds for precise timing calculations.
    /// </summary>
    /// <param name="ticks">Stopwatch ticks</param>
    /// <returns>Time in nanoseconds</returns>
    public static long TicksToNanoseconds(long ticks)
    {
        return (long)(ticks * s_ticksToNanoseconds);
    }

    /// <summary>
    /// Gets the nanosecond conversion factor for external timing calculations.
    /// </summary>
    public static double TicksToNanosecondsRatio => s_ticksToNanoseconds;

    /// <summary>
    /// Clears all statistics.
    /// </summary>
    public void Clear()
    {
        lock (_statsLock)
        {
            _callStats.Clear();
            _activeStopwatches.Clear();
        }
    }

    /// <summary>
    /// Gets the number of different methods being tracked.
    /// </summary>
    public int TrackedMethodCount
    {
        get
        {
            lock (_statsLock)
            {
                return _callStats.Count;
            }
        }
    }

    /// <summary>
    /// Gets the total number of calls across all methods.
    /// </summary>
    public int TotalCalls
    {
        get
        {
            lock (_statsLock)
            {
                return _callStats.Values.Sum(s => s.callCount);
            }
        }
    }

    /// <summary>
    /// Gets the total execution time across all methods in nanoseconds.
    /// </summary>
    public long TotalTimeNs
    {
        get
        {
            lock (_statsLock)
            {
                return _callStats.Values.Sum(s => s.totalTimeNs);
            }
        }
    }

    /// <summary>
    /// Gets the total execution time across all methods in milliseconds (for backward compatibility).
    /// </summary>
    public long TotalTimeMs
    {
        get
        {
            return TotalTimeNs / 1_000_000;
        }
    }

    /// <summary>
    /// Gets timing resolution information.
    /// </summary>
    public static string GetTimingInfo()
    {
        return $"Stopwatch Frequency: {Stopwatch.Frequency:N0} Hz, " +
               $"Resolution: {s_ticksToNanoseconds:F3}ns per tick, " +
               $"High Resolution: {Stopwatch.IsHighResolution}";
    }
}