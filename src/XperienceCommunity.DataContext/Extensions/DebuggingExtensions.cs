using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Diagnostics;

namespace XperienceCommunity.DataContext.Extensions;

/// <summary>
/// Extension methods for debugging and diagnostics in data contexts.
/// </summary>
[Description("Extension methods for enhanced debugging of data context operations")]
public static class DebuggingExtensions
{
    /// <summary>
    /// Enables diagnostic logging for this data context instance.
    /// </summary>
    /// <typeparam name="T">The content item type.</typeparam>
    /// <param name="context">The data context instance.</param>
    /// <param name="logLevel">The minimum log level to capture.</param>
    /// <returns>The same data context instance for method chaining.</returns>
    [DebuggerStepThrough]
    public static IDataContext<T> EnableDiagnostics<T>(this IDataContext<T> context, LogLevel logLevel = LogLevel.Debug)
    {
        DataContextDiagnostics.DiagnosticsEnabled = true;
        DataContextDiagnostics.TraceLevel = logLevel;

        DataContextDiagnostics.LogDiagnostic(
            "ContextConfiguration",
            $"Diagnostics enabled for {typeof(T).Name} with level {logLevel}");

        return context;
    }

    /// <summary>
    /// Disables diagnostic logging for this data context instance.
    /// </summary>
    /// <typeparam name="T">The content item type.</typeparam>
    /// <param name="context">The data context instance.</param>
    /// <returns>The same data context instance for method chaining.</returns>
    [DebuggerStepThrough]
    public static IDataContext<T> DisableDiagnostics<T>(this IDataContext<T> context)
    {
        DataContextDiagnostics.DiagnosticsEnabled = false;
        return context;
    }

    /// <summary>
    /// Gets a diagnostic report for the current session.
    /// </summary>
    /// <typeparam name="T">The content item type.</typeparam>
    /// <param name="context">The data context instance.</param>
    /// <param name="category">Optional category filter.</param>
    /// <returns>A formatted diagnostic report.</returns>
    public static string GetDiagnosticReport<T>(this IDataContext<T> context, string? category = null)
    {
        return DataContextDiagnostics.GetDiagnosticReport(category);
    }

    /// <summary>
    /// Gets performance statistics for the current session.
    /// </summary>
    /// <typeparam name="T">The content item type.</typeparam>
    /// <param name="context">The data context instance.</param>
    /// <returns>A dictionary containing performance metrics.</returns>
    public static Dictionary<string, object> GetPerformanceStats<T>(this IDataContext<T> context)
    {
        return DataContextDiagnostics.GetPerformanceStats();
    }

    /// <summary>
    /// Logs a custom diagnostic entry in the context of this data context.
    /// </summary>
    /// <typeparam name="T">The content item type.</typeparam>
    /// <param name="context">The data context instance.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="level">The log level.</param>
    /// <param name="memberName">The calling member name (automatically populated).</param>
    /// <returns>The same data context instance for method chaining.</returns>
    [DebuggerStepThrough]
    public static IDataContext<T> LogDiagnostic<T>(
        this IDataContext<T> context,
        string message,
        LogLevel level = LogLevel.Information,
        [CallerMemberName] string? memberName = null)
    {
        DataContextDiagnostics.LogDiagnostic(
            $"UserContext_{typeof(T).Name}",
            message,
            level,
            memberName);

        return context;
    }

    /// <summary>
    /// Executes a query with detailed timing diagnostics.
    /// </summary>
    /// <typeparam name="T">The content item type.</typeparam>
    /// <typeparam name="TResult">The result type of the query operation.</typeparam>
    /// <param name="context">The data context instance.</param>
    /// <param name="operation">The operation name for diagnostics.</param>
    /// <param name="queryFunc">The query function to execute.</param>
    /// <returns>The query results with timing information logged.</returns>
    public static async Task<TResult> ExecuteWithDiagnostics<T, TResult>(
        this IDataContext<T> context,
        string operation,
        Func<IDataContext<T>, Task<TResult>> queryFunc)
    {
        var stopwatch = Stopwatch.StartNew();

        DataContextDiagnostics.LogDiagnostic(
            "QueryTiming",
            $"Starting {operation} for {typeof(T).Name}",
            LogLevel.Debug);

        try
        {
            var result = await queryFunc(context).ConfigureAwait(false);

            stopwatch.Stop();

            DataContextDiagnostics.LogDiagnostic(
                "QueryTiming",
                $"Completed {operation} for {typeof(T).Name} in {stopwatch.ElapsedMilliseconds}ms",
                LogLevel.Information);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            DataContextDiagnostics.LogDiagnostic(
                "QueryTiming",
                $"Failed {operation} for {typeof(T).Name} after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}",
                LogLevel.Error);

            throw;
        }
    }

    /// <summary>
    /// Creates a debug-friendly string representation of the current context state.
    /// </summary>
    /// <typeparam name="T">The content item type.</typeparam>
    /// <param name="context">The data context instance.</param>
    /// <returns>A string representation suitable for debugging.</returns>
    public static string ToDebugString<T>(this IDataContext<T> context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"DataContext<{typeof(T).Name}>");
        sb.AppendLine($"  Type: {context.GetType().Name}");
        sb.AppendLine($"  Hash: {context.GetHashCode():X8}");
        sb.AppendLine($"  Diagnostics Enabled: {DataContextDiagnostics.DiagnosticsEnabled}");

        var stats = DataContextDiagnostics.GetPerformanceStats();
        sb.AppendLine($"  Session Queries: {stats.GetValueOrDefault("TotalQueries", 0)}");
        sb.AppendLine($"  Session Errors: {stats.GetValueOrDefault("ErrorCount", 0)}");

        return sb.ToString();
    }
}
