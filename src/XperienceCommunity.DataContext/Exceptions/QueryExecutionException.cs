using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace XperienceCommunity.DataContext.Exceptions;

/// <summary>
/// Exception thrown when a query execution fails.
/// </summary>
[DebuggerDisplay("ContentType: {ContentTypeName}, Message: {Message}")]
public class QueryExecutionException : Exception
{
    /// <summary>
    /// Gets the content type name that was being queried.
    /// </summary>
    public string? ContentTypeName { get; }

    /// <summary>
    /// Gets the source member name where the exception occurred.
    /// </summary>
    public string? SourceMemberName { get; }

    /// <summary>
    /// Gets the source file path where the exception occurred.
    /// </summary>
    public string? SourceFilePath { get; }

    /// <summary>
    /// Gets the source line number where the exception occurred.
    /// </summary>
    public int? SourceLineNumber { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class.
    /// </summary>
    public QueryExecutionException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public QueryExecutionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public QueryExecutionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="contentTypeName">The content type name that was being queried.</param>
    /// <param name="innerException">The inner exception.</param>
    public QueryExecutionException(string message, string contentTypeName, Exception innerException)
        : base(message, innerException)
    {
        ContentTypeName = contentTypeName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="contentTypeName">The content type name that was being queried.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="memberName">The calling member name.</param>
    /// <param name="filePath">The calling file path.</param>
    /// <param name="lineNumber">The calling line number.</param>
    public QueryExecutionException(string message, string? contentTypeName, Exception innerException,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0) : base(message, innerException)
    {
        ContentTypeName = contentTypeName;
        SourceMemberName = memberName;
        SourceFilePath = filePath;
        SourceLineNumber = lineNumber == 0 ? null : lineNumber;
    }
}
