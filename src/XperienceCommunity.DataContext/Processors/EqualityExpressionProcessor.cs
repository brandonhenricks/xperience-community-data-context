using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal class EqualityExpressionProcessor : IExpressionProcessor<BinaryExpression>
    {
        private readonly QueryParameterManager _parameterManager;
        private readonly bool _isEqual;

        public EqualityExpressionProcessor(QueryParameterManager parameterManager, bool isEqual = true)
        {
            _parameterManager = parameterManager;
            _isEqual = isEqual;
        }

        public void Process(BinaryExpression node)
        {
            if (node.Left is MemberExpression member && node.Right is ConstantExpression constant)
            {
                _parameterManager.AddParameter(member.Member.Name, (_isEqual ? constant.Value : $"!{constant.Value}")!);
            }
            else
            {
                throw new InvalidOperationException("Invalid expression format for equality comparison.");
            }
        }
    }
}
