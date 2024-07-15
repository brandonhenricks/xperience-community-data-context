using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal sealed class LogicalExpressionProcessor : IExpressionProcessor<BinaryExpression>
    {
        private readonly QueryParameterManager _parameterManager;
        private readonly bool _isAnd;

        public LogicalExpressionProcessor(QueryParameterManager parameterManager, bool isAnd)
        {
            _parameterManager = parameterManager;
            _isAnd = isAnd;
        }

        public void Process(BinaryExpression node)
        {

            if (_isAnd)
            {
                _parameterManager.AddLogicalCondition("AND");
            }
            else
            {
                _parameterManager.AddLogicalCondition("OR");
            }
        }
    }
}
