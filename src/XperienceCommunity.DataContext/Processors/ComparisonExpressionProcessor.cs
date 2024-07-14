using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal class ComparisonExpressionProcessor : IExpressionProcessor<BinaryExpression>
    {
        private readonly QueryParameterManager _parameterManager;
        private readonly bool _isGreaterThan;
        private readonly bool _isEqual;

        public ComparisonExpressionProcessor(QueryParameterManager parameterManager, bool isGreaterThan, bool isEqual = false)
        {
            _parameterManager = parameterManager;
            _isGreaterThan = isGreaterThan;
            _isEqual = isEqual;
        }

        public void Process(BinaryExpression node)
        {
            if (node.Left is MemberExpression member && node.Right is ConstantExpression constant)
            {
                var comparisonOperator = _isGreaterThan ? ">" : "<";

                if (_isEqual)
                {
                    comparisonOperator += "=";
                }

                _parameterManager.AddComparisonCondition(member.Member.Name, comparisonOperator, constant.Value);
            }
            else
            {
                throw new InvalidOperationException("Invalid expression format for comparison.");
            }
        }
    }
}
