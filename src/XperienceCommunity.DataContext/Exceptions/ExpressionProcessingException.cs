using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace XperienceCommunity.DataContext.Exceptions;

/// <summary>
/// Exception thrown when an expression cannot be processed.
/// </summary>
[DebuggerDisplay("Expression: {ExpressionTypeName}, Context: {ProcessorContext}")]
public class ExpressionProcessingException : Exception
{
    /// <summary>
    /// Gets the expression that caused the error.
    /// </summary>
    public Expression? Expression { get; }

    /// <summary>
    /// Gets the expression type name.
    /// </summary>
    public string? ExpressionTypeName { get; }

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
    /// Gets additional context about the processor state.
    /// </summary>
    public string? ProcessorContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionProcessingException"/> class.
    /// </summary>
    public ExpressionProcessingException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionProcessingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ExpressionProcessingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionProcessingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="expression">The expression that caused the error.</param>
    public ExpressionProcessingException(string message, Expression expression) : base(message)
    {
        Expression = expression;
        ExpressionTypeName = expression.GetType().Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionProcessingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ExpressionProcessingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionProcessingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="expression">The expression that caused the error.</param>
    /// <param name="innerException">The inner exception.</param>
    public ExpressionProcessingException(string message, Expression expression, Exception innerException)
        : base(message, innerException)
    {
        Expression = expression;
        ExpressionTypeName = expression.GetType().Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionProcessingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="expression">The expression that caused the error.</param>
    /// <param name="sourceMemberName">The source member name.</param>
    /// <param name="sourceFilePath">The source file path.</param>
    /// <param name="sourceLineNumber">The source line number.</param>
    /// <param name="processorContext">The processor context.</param>
    /// <param name="innerException">The inner exception.</param>
    public ExpressionProcessingException(string message, Expression expression, string sourceMemberName, string sourceFilePath, int sourceLineNumber, string processorContext, Exception innerException)
        : base(message, innerException)
    {
        Expression = expression;
        ExpressionTypeName = expression.GetType().Name;
        SourceMemberName = sourceMemberName;
        SourceFilePath = sourceFilePath;
        SourceLineNumber = sourceLineNumber;
        ProcessorContext = processorContext;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionProcessingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="expression">The expression that caused the error.</param>
    /// <param name="processorContext">Additional context about the processor state.</param>
    /// <param name="memberName">The calling member name.</param>
    /// <param name="filePath">The calling file path.</param>
    /// <param name="lineNumber">The calling line number.</param>
    public ExpressionProcessingException(string message, Expression expression, string? processorContext = null,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0) : base(message)
    {
        Expression = expression;
        ExpressionTypeName = expression.GetType().Name;
        ProcessorContext = processorContext;
        SourceMemberName = memberName;
        SourceFilePath = filePath;
        SourceLineNumber = lineNumber == 0 ? null : lineNumber;
    }
}
