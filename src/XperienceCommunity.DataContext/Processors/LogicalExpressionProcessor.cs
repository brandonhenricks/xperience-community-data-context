using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal class LogicalExpressionProcessor : IExpressionProcessor<BinaryExpression>
    {
        private readonly bool _isAnd;
        private readonly QueryParameterManager _parameterManager;

        public LogicalExpressionProcessor(QueryParameterManager parameterManager, bool isAnd)
        {
            _parameterManager = parameterManager;
            _isAnd = isAnd;
        }

        public void Process(BinaryExpression node)
        {
            var logicalOperator = _isAnd ? "AND" : "OR";
            _parameterManager.AddLogicalOperator(logicalOperator);
        }
    }
}
