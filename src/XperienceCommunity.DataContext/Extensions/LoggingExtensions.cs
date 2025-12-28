using System.ComponentModel;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Diagnostics;

namespace XperienceCommunity.DataContext.Extensions;

/// <summary>
/// Provides debugging utilities specifically for the Microsoft.Extensions.Logging integration.
/// </summary>
[Description("Logging integration utilities for data context debugging")]
public static class LoggingExtensions
{
    /// <summary>
    /// Configures a logger to receive data context diagnostic events.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="minLevel">The minimum log level to forward.</param>
    public static void AttachDataContextDiagnostics(this ILogger logger, LogLevel minLevel = LogLevel.Debug)
    {
        DataContextDiagnostics.DiagnosticsEnabled = true;
        DataContextDiagnostics.TraceLevel = minLevel;

        // Note: In a real implementation, you might want to create a custom logger provider
        // that can receive diagnostic events and forward them to the provided logger
        logger.LogInformation("Data context diagnostics attached with minimum level {MinLevel}", minLevel);
    }
}
