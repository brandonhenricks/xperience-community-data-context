namespace XperienceCommunity.DataContext.Exceptions
{
    public sealed class InvalidExpressionFormatException : ExpressionProcessingException
    {
        public InvalidExpressionFormatException() : base()
        {
        }

        public InvalidExpressionFormatException(string message) : base(message)
        {
        }

        public InvalidExpressionFormatException(string message, System.Linq.Expressions.Expression expression) : base(message, expression)
        {
        }

        public InvalidExpressionFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public InvalidExpressionFormatException(string message, System.Linq.Expressions.Expression expression, Exception innerException) : base(message, expression, innerException)
        {
        }
    }
}
