using System.Linq.Expressions;

namespace XperienceCommunity.DataContext.Exceptions
{
    /// <summary>
    /// Exception thrown when an expression cannot be processed.
    /// </summary>
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
    }
}
