using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace XperienceCommunity.DataContext.Diagnostics;

/// <summary>
/// Represents a single diagnostic entry.
/// </summary>
[DebuggerDisplay("{Level} [{Category}] {Message} @ {MemberName}")]
[Description("Individual diagnostic log entry with context information")]
public sealed class DiagnosticEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the diagnostic message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the calling member name.
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string? SourceFilePath { get; set; }

    /// <summary>
    /// Gets or sets the source line number.
    /// </summary>
    public int SourceLineNumber { get; set; }

    /// <summary>
    /// Gets or sets the thread ID where the entry was created.
    /// </summary>
    public int ThreadId { get; set; }
}
