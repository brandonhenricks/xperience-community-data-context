using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal sealed class UnaryExpressionProcessor: IExpressionProcessor<UnaryExpression>
    {
        private readonly QueryParameterManager _parameterManager;

        public UnaryExpressionProcessor(QueryParameterManager parameterManager)
        {
            _parameterManager = parameterManager;
        }

        public void Process(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    ProcessNot(node);
                    break;
                case ExpressionType.Convert:
                    ProcessConvert(node);
                    break;
                case ExpressionType.Quote:
                    ProcessQuote(node);
                    break;
                default:
                    throw new NotSupportedException($"The unary expression type '{node.NodeType}' is not supported.");
            }
        }
        private void ProcessNot(UnaryExpression node)
        {
            if (node.Operand is BinaryExpression binaryExpression)
            {
                var binaryProcessor = new BinaryExpressionProcessor(_parameterManager);
                binaryProcessor.Process(binaryExpression);

                // Apply negation logic
                // Assuming we are handling a NOT operation by wrapping the condition in a NOT statement
                // This is a simplified example and may need adjustment based on the actual usage context
                //_parameterManager.AddLogicalCondition("AND", new WhereCondition()
                    //.Where(w => w.Not(_parameterManager.GetWhereParameters())));
            }
            else
            {
                throw new InvalidOperationException("Invalid expression format for NOT operation.");
            }
        }

        private void ProcessConvert(UnaryExpression node)
        {
            // Handle type conversion logic
            // This is a placeholder implementation, and you may need to adjust based on actual usage context
            // In most cases, the Convert operation may not need special handling for building query conditions
        }
        private void ProcessQuote(UnaryExpression node)
        {
            // Handle quote logic
            // This may involve visiting the operand or other processing specific to the 'Quote' expression
            Visit(node.Operand);
        }

        private void Visit(Expression node)
        {
            var visitor = new ContentItemQueryExpressionVisitor(_parameterManager);
            visitor.Visit(node);
        }
    }
}
