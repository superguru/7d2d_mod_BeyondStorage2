using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BeyondStorage.Scripts.Infrastructure;

/// <summary>
/// Thread-safe class for tracking method call statistics including call count, total time, and average time.
/// Can be used by any class that needs to track performance metrics for method calls.
/// Uses high-resolution timing with microsecond precision, providing accurate measurements
/// within the actual capabilities of the underlying hardware timer.
/// Includes internal stopwatch management for convenience.
/// </summary>
/// <remarks>
/// Initializes a new instance of PerformanceProfiler.
/// </remarks>
/// <param name="trackerName">Name of the tracker for logging purposes</param>
public sealed class PerformanceProfiler(string trackerName)
{
    // Time conversion constants
    private const double MicrosecsPerMillisec = 1_000.0;
    private const double MicrosecsPerSec = 1_000_000.0;

    private readonly Dictionary<string, (int callCount, long totalTimeUs, double avgTimeUs)> _callStats = [];
    private readonly Dictionary<string, Stopwatch> _activeStopwatches = [];
    private readonly object _statsLock = new();
    private readonly string _trackerName = trackerName ?? throw new ArgumentNullException(nameof(trackerName));

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
    /// <returns>The elapsed time in microseconds, or -1 if method wasn't being timed</returns>
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

            var elapsedUs = (long)(stopwatch.ElapsedTicks * TicksToMicrosecondsRatio);
            RecordCallInternal(methodName, elapsedUs);

            return elapsedUs;
        }
    }

    /// <summary>
    /// Gets the current elapsed time for a method being timed, without stopping the timer.
    /// </summary>
    /// <param name="methodName">Name of the method being timed</param>
    /// <returns>Current elapsed time in microseconds, or -1 if method isn't being timed</returns>
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
                return (long)(stopwatch.ElapsedTicks * TicksToMicrosecondsRatio);
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
    /// Records a method call with its execution time in microseconds.
    /// </summary>
    /// <param name="methodName">Name of the method that was called</param>
    /// <param name="elapsedUs">Execution time in microseconds</param>
    public void RecordCall(string methodName, long elapsedUs)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        }

        lock (_statsLock)
        {
            RecordCallInternal(methodName, elapsedUs);
        }
    }

    /// <summary>
    /// Records a method call with its execution time in milliseconds (for backward compatibility).
    /// </summary>
    /// <param name="methodName">Name of the method that was called</param>
    /// <param name="elapsedMs">Execution time in milliseconds</param>
    public void RecordCallMs(string methodName, long elapsedMs)
    {
        RecordCall(methodName, (long)(elapsedMs * MicrosecsPerMillisec)); // Convert ms to μs
    }

    /// <summary>
    /// Records a method call using a stopwatch with microsecond precision.
    /// </summary>
    /// <param name="methodName">Name of the method that was called</param>
    /// <param name="stopwatch">Stopwatch that was used to time the method</param>
    public void RecordCall(string methodName, Stopwatch stopwatch)
    {
        if (stopwatch == null)
        {
            throw new ArgumentNullException(nameof(stopwatch));
        }

        // Convert ticks to microseconds for high-resolution precision
        var elapsedUs = (long)(stopwatch.ElapsedTicks * TicksToMicrosecondsRatio);
        RecordCall(methodName, elapsedUs);
    }

    private void RecordCallInternal(string methodName, long elapsedUs)
    {
        if (_callStats.TryGetValue(methodName, out var stats))
        {
            var newCallCount = stats.callCount + 1;
            var newTotalTime = stats.totalTimeUs + elapsedUs;
            var newAvgTime = (double)newTotalTime / newCallCount;

            _callStats[methodName] = (newCallCount, newTotalTime, newAvgTime);
        }
        else
        {
            _callStats[methodName] = (1, elapsedUs, (double)elapsedUs);
        }
    }

    /// <summary>
    /// Gets statistics for a specific method with microsecond precision.
    /// </summary>
    /// <param name="methodName">Name of the method</param>
    /// <returns>Statistics tuple or null if method not found</returns>
    public (int callCount, long totalTimeUs, double avgTimeUs)? GetMethodStats(string methodName)
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
            var (callCount, totalTimeUs, avgTimeUs) = stats.Value;
            return (callCount, (long)(totalTimeUs / MicrosecsPerMillisec), avgTimeUs / MicrosecsPerMillisec);
        }
        return null;
    }

    /// <summary>
    /// Gets all call statistics with microsecond precision.
    /// </summary>
    /// <returns>Dictionary of method names and their timing statistics</returns>
    public Dictionary<string, (int callCount, long totalTimeUs, double avgTimeUs)> GetAllStatistics()
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
                var (callCount, totalTimeUs, avgTimeUs) = kvp.Value;
                result[kvp.Key] = (callCount, (long)(totalTimeUs / MicrosecsPerMillisec), avgTimeUs / MicrosecsPerMillisec);
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
                var (callCount, totalTimeUs, avgTimeUs) = kvp.Value;

                // Intelligently choose units based on magnitude
                var (avgDisplay, totalDisplay) = FormatTime(avgTimeUs, totalTimeUs);
                stats.Add($"{method}: {callCount} calls, avg {avgDisplay}, total {totalDisplay}");
            }

            return $"{_trackerName}_Stats: [{string.Join(", ", stats)}]";
        }
    }

    /// <summary>
    /// Formats time values with appropriate units (μs, ms, s).
    /// </summary>
    private static (string avg, string total) FormatTime(double avgTimeUs, long totalTimeUs)
    {
        string avgDisplay, totalDisplay;

        // Format average time
        if (avgTimeUs < MicrosecsPerMillisec) // Less than 1 millisecond
        {
            avgDisplay = $"{avgTimeUs:F3}μs";
        }
        else if (avgTimeUs < MicrosecsPerSec) // Less than 1 second
        {
            avgDisplay = $"{avgTimeUs / MicrosecsPerMillisec:F3}ms";
        }
        else
        {
            avgDisplay = $"{avgTimeUs / MicrosecsPerSec:F3}s";
        }

        // Format total time
        if (totalTimeUs < MicrosecsPerMillisec) // Less than 1 millisecond
        {
            totalDisplay = $"{totalTimeUs}μs";
        }
        else if (totalTimeUs < MicrosecsPerSec) // Less than 1 second
        {
            totalDisplay = $"{totalTimeUs / MicrosecsPerMillisec:F3}ms";
        }
        else
        {
            totalDisplay = $"{totalTimeUs / MicrosecsPerSec:F3}s";
        }

        return (avgDisplay, totalDisplay);
    }

    /// <summary>
    /// Formats a single microsecond value with appropriate units for readability.
    /// </summary>
    /// <param name="microseconds">Time in microseconds</param>
    /// <returns>Formatted string with appropriate unit</returns>
    public static string FormatMicroseconds(double microseconds)
    {
        if (microseconds < MicrosecsPerMillisec) // Less than 1 millisecond
        {
            return $"{microseconds:F3}μs";
        }
        else if (microseconds < MicrosecsPerSec) // Less than 1 second
        {
            return $"{microseconds / MicrosecsPerMillisec:F3}ms";
        }
        else
        {
            return $"{microseconds / MicrosecsPerSec:F3}s";
        }
    }

    /// <summary>
    /// Converts stopwatch ticks to microseconds for precise timing calculations.
    /// </summary>
    /// <param name="ticks">Stopwatch ticks</param>
    /// <returns>Time in microseconds</returns>
    public static long TicksToMicroseconds(long ticks)
    {
        return (long)(ticks * TicksToMicrosecondsRatio);
    }

    /// <summary>
    /// Gets the microsecond conversion factor for external timing calculations.
    /// </summary>
    public static double TicksToMicrosecondsRatio { get; } = MicrosecsPerSec / Stopwatch.Frequency;

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
    /// Gets the total execution time across all methods in microseconds.
    /// </summary>
    public long TotalTimeUs
    {
        get
        {
            lock (_statsLock)
            {
                return _callStats.Values.Sum(s => s.totalTimeUs);
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
            return (long)(TotalTimeUs / MicrosecsPerMillisec);
        }
    }

    /// <summary>
    /// Gets timing resolution information.
    /// </summary>
    public static string GetTimingInfo()
    {
        return $"Stopwatch Frequency: {Stopwatch.Frequency:N0} Hz, " +
               $"Resolution: {TicksToMicrosecondsRatio:F3}μs per tick, " +
               $"High Resolution: {Stopwatch.IsHighResolution}";
    }
}