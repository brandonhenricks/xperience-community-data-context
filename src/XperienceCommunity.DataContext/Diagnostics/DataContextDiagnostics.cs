using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace XperienceCommunity.DataContext.Diagnostics;

/// <summary>
/// Provides diagnostic and debugging utilities for the XperienceCommunity.DataContext library.
/// Use this class to gain insights into query execution, expression processing, and performance.
/// </summary>
[DebuggerDisplay("DiagnosticsEnabled: {DiagnosticsEnabled}, TraceLevel: {TraceLevel}")]
[Description("Diagnostic utilities for debugging data context operations")]
public static class DataContextDiagnostics
{
    private static bool _diagnosticsEnabled;
    private static LogLevel _traceLevel = LogLevel.Information;
    private static readonly ConcurrentQueue<DiagnosticEntry> _diagnosticLog = new();
    private const int MAX_LOG_ENTRIES = 1000;

    /// <summary>
    /// Gets or sets whether diagnostics are enabled.
    /// </summary>
    public static bool DiagnosticsEnabled
    {
        get => _diagnosticsEnabled;
        set => _diagnosticsEnabled = value;
    }

    /// <summary>
    /// Gets or sets the minimum trace level for diagnostic output.
    /// </summary>
    public static LogLevel TraceLevel
    {
        get => _traceLevel;
        set => _traceLevel = value;
    }

    /// <summary>
    /// Gets a readonly collection of diagnostic entries.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public static IReadOnlyList<DiagnosticEntry> DiagnosticEntries
    {
        get
        {
            return _diagnosticLog.ToList();
        }
    }

    /// <summary>
    /// Logs a diagnostic entry if diagnostics are enabled.
    /// </summary>
    /// <param name="category">The diagnostic category (e.g., "ExpressionProcessing", "QueryExecution").</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="level">The log level.</param>
    /// <param name="memberName">The calling member name (automatically populated).</param>
    /// <param name="sourceFilePath">The source file path (automatically populated).</param>
    /// <param name="sourceLineNumber">The source line number (automatically populated).</param>
    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void LogDiagnostic(
        string category,
        string message,
        LogLevel level = LogLevel.Information,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!_diagnosticsEnabled || level < _traceLevel)
            return;

        var entry = new DiagnosticEntry
        {
            Timestamp = DateTime.UtcNow,
            Category = category,
            Message = message,
            Level = level,
            MemberName = memberName,
            SourceFilePath = sourceFilePath,
            SourceLineNumber = sourceLineNumber,
            ThreadId = Environment.CurrentManagedThreadId
        };

        _diagnosticLog.Enqueue(entry);

        // Efficiently manage memory by removing excess entries
        while (_diagnosticLog.Count > MAX_LOG_ENTRIES)
        {
            _diagnosticLog.TryDequeue(out _);
        }

        // Also output to debug console
        Debug.WriteLine($"[{category}] {message} ({memberName} at line {sourceLineNumber})");
    }

    /// <summary>
    /// Clears all diagnostic entries.
    /// </summary>
    public static void ClearDiagnostics()
    {
        while (_diagnosticLog.TryDequeue(out _))
        {
            // Clear all entries efficiently
        }
    }

    /// <summary>
    /// Gets a formatted diagnostic report as a string.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="minLevel">Minimum log level to include.</param>
    /// <returns>A formatted diagnostic report.</returns>
    public static string GetDiagnosticReport(string? category = null, LogLevel minLevel = LogLevel.Debug)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== XperienceCommunity.DataContext Diagnostic Report ===");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Diagnostics Enabled: {_diagnosticsEnabled}");
        sb.AppendLine($"Trace Level: {_traceLevel}");
        sb.AppendLine();

        var filteredEntries = _diagnosticLog
            .Where(e => e.Level >= minLevel)
            .Where(e => category == null || e.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (filteredEntries.Count == 0)
        {
            sb.AppendLine("No diagnostic entries found matching criteria.");
        }
        else
        {
            sb.AppendLine($"Entries: {filteredEntries.Count}");
            sb.AppendLine(new string('-', 80));

            foreach (var entry in filteredEntries)
            {
                sb.AppendLine($"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] [{entry.Category}] Thread {entry.ThreadId}");
                sb.AppendLine($"  {entry.Message}");
                sb.AppendLine($"  @ {entry.MemberName} (line {entry.SourceLineNumber})");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets performance statistics for query execution.
    /// </summary>
    /// <returns>A dictionary containing performance metrics.</returns>
    public static Dictionary<string, object> GetPerformanceStats()
    {
        var queryEntries = _diagnosticLog
            .Where(e => e.Category.Equals("QueryExecution", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new Dictionary<string, object>
        {
            ["TotalQueries"] = queryEntries.Count,
            ["QueriesLast5Minutes"] = queryEntries.Count(e => e.Timestamp > DateTime.UtcNow.AddMinutes(-5)),
            ["QueriesLast1Hour"] = queryEntries.Count(e => e.Timestamp > DateTime.UtcNow.AddHours(-1)),
            ["ErrorCount"] = queryEntries.Count(e => e.Level >= LogLevel.Error),
            ["WarningCount"] = queryEntries.Count(e => e.Level == LogLevel.Warning),
            ["LastQueryTime"] = queryEntries.LastOrDefault()?.Timestamp ?? DateTime.UtcNow,
            ["DiagnosticsEnabled"] = _diagnosticsEnabled,
            ["TraceLevel"] = _traceLevel.ToString()
        };
    }
}
