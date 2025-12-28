using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace XperienceCommunity.DataContext.Diagnostics;

/// <summary>
/// Tracks performance metrics for query executors in DEBUG builds only.
/// This class has zero runtime cost in Release builds due to Conditional compilation.
/// </summary>
[DebuggerDisplay("Enabled: DEBUG-only, Tracked Types: {_performanceData.Count}")]
public static class QueryExecutorPerformanceTracker
{
    private static readonly ConcurrentDictionary<string, PerformanceMetrics> _performanceData = new();

    /// <summary>
    /// Records a query execution for performance tracking (DEBUG builds only).
    /// </summary>
    /// <param name="executorTypeName">The full name of the executor type.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordExecution(string executorTypeName, long executionTimeMs)
    {
        var metrics = _performanceData.GetOrAdd(executorTypeName, _ => new PerformanceMetrics());
        metrics.RecordExecution(executionTimeMs);
    }

    /// <summary>
    /// Gets performance metrics for a specific executor type (DEBUG builds only).
    /// Returns default values in Release builds.
    /// </summary>
    /// <param name="executorTypeName">The full name of the executor type.</param>
    /// <returns>Performance metrics for the executor type.</returns>
    public static PerformanceMetrics GetMetrics(string executorTypeName)
    {
#if DEBUG
        return _performanceData.TryGetValue(executorTypeName, out var metrics) 
            ? metrics 
            : new PerformanceMetrics();
#else
        return new PerformanceMetrics();
#endif
    }

    /// <summary>
    /// Gets all tracked executor type names (DEBUG builds only).
    /// </summary>
    public static IEnumerable<string> GetTrackedExecutorTypes()
    {
#if DEBUG
        return _performanceData.Keys;
#else
        return Enumerable.Empty<string>();
#endif
    }

    /// <summary>
    /// Clears all performance data (DEBUG builds only).
    /// </summary>
    [Conditional("DEBUG")]
    public static void Clear()
    {
        _performanceData.Clear();
    }

    /// <summary>
    /// Clears performance data for a specific executor type (DEBUG builds only).
    /// </summary>
    [Conditional("DEBUG")]
    public static void Clear(string executorTypeName)
    {
        _performanceData.TryRemove(executorTypeName, out _);
    }
}

/// <summary>
/// Performance metrics for a query executor type.
/// </summary>
[DebuggerDisplay("Executions: {TotalExecutions}, Avg: {AverageExecutionTimeMs:F2}ms")]
public sealed class PerformanceMetrics
{
    private long _totalExecutions;
    private long _totalExecutionTimeMs;

    /// <summary>
    /// Gets the total number of executions.
    /// </summary>
    public long TotalExecutions => Interlocked.Read(ref _totalExecutions);

    /// <summary>
    /// Gets the total execution time in milliseconds.
    /// </summary>
    public long TotalExecutionTimeMs => Interlocked.Read(ref _totalExecutionTimeMs);

    /// <summary>
    /// Gets the average execution time in milliseconds.
    /// Values are approximate under concurrency and intended for diagnostics only.
    /// </summary>
    public double AverageExecutionTimeMs
    {
        get
        {
            var executions = Interlocked.Read(ref _totalExecutions);
            if (executions <= 0)
            {
                return 0;
            }

            var totalTime = Interlocked.Read(ref _totalExecutionTimeMs);
            return (double)totalTime / executions;
        }
    }
    /// <summary>
    /// Records a single execution.
    /// </summary>
    internal void RecordExecution(long executionTimeMs)
    {
        Interlocked.Increment(ref _totalExecutions);
        Interlocked.Add(ref _totalExecutionTimeMs, executionTimeMs);
    }
}
