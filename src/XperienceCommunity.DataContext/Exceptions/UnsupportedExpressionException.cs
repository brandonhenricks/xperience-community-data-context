using System.Linq.Expressions;

namespace XperienceCommunity.DataContext.Exceptions;

/// <summary>
/// Exception thrown when an expression type is not supported.
/// </summary>
public sealed class UnsupportedExpressionException : ExpressionProcessingException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedExpressionException"/> class.
    /// </summary>
    /// <param name="expressionType">The unsupported expression type.</param>
    public UnsupportedExpressionException(Type expressionType)
        : base($"The expression type '{expressionType.Name}' is not supported.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedExpressionException"/> class.
    /// </summary>
    /// <param name="expressionType">The unsupported expression type.</param>
    /// <param name="expression">The expression that caused the error.</param>
    public UnsupportedExpressionException(ExpressionType expressionType, Expression expression)
        : base($"The expression type '{expressionType}' is not supported.", expression)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedExpressionException"/> class.
    /// </summary>
    /// <param name="methodName">The unsupported method name.</param>
    /// <param name="expression">The expression that caused the error.</param>
    public UnsupportedExpressionException(string methodName, Expression expression)
        : base($"The method '{methodName}' is not supported.", expression)
    {
    }

    public UnsupportedExpressionException() : base()
    {
    }

    public UnsupportedExpressionException(string message) : base(message)
    {
    }

    public UnsupportedExpressionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public UnsupportedExpressionException(string message, Expression expression, Exception innerException) : base(message, expression, innerException)
    {
    }
}
