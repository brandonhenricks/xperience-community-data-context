using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal sealed class BinaryExpressionProcessor : IExpressionProcessor<BinaryExpression>
    {
        private readonly QueryParameterManager _parameterManager;

        public BinaryExpressionProcessor(QueryParameterManager parameterManager)
        {
            _parameterManager = parameterManager;
        }

        public void Process(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    ProcessEquality(node, isEqual: true);
                    break;

                case ExpressionType.NotEqual:
                    ProcessEquality(node, isEqual: false);
                    break;

                case ExpressionType.GreaterThan:
                    ProcessComparison(node, isGreaterThan: true);
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    ProcessComparison(node, isGreaterThan: true, isEqual: true);
                    break;

                case ExpressionType.LessThan:
                    ProcessComparison(node, isGreaterThan: false);
                    break;

                case ExpressionType.LessThanOrEqual:
                    ProcessComparison(node, isGreaterThan: false, isEqual: true);
                    break;

                case ExpressionType.AndAlso:
                    ProcessLogical(node, isAnd: true);
                    break;

                case ExpressionType.OrElse:
                    ProcessLogical(node, isAnd: false);
                    break;

                case ExpressionType.And:
                    ProcessLogical(node, true);
                    break;

                case ExpressionType.Or:
                    ProcessLogical(node, false);
                    break;

                default:
                    throw new NotSupportedException($"The binary expression type '{node.NodeType}' is not supported.");
            }
        }

        private void ProcessEquality(BinaryExpression node, bool isEqual)
        {
            if (node is { Left: MemberExpression member, Right: ConstantExpression constant })
            {
                if (isEqual)
                {
                    _parameterManager.AddEqualsCondition(member.Member.Name, constant.Value);
                }
                else
                {
                    _parameterManager.AddNotEqualsCondition(member.Member.Name, constant.Value);
                }
            }
            else
            {
                throw new InvalidOperationException("Invalid expression format for equality comparison.");
            }
        }

        private void ProcessComparison(BinaryExpression node, bool isGreaterThan, bool isEqual = false)
        {
            if (node is { Left: MemberExpression member, Right: ConstantExpression constant })
            {
                var comparisonOperator = isGreaterThan ? ">" : "<";
                if (isEqual)
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

        private void ProcessLogical(BinaryExpression node, bool isAnd)
        {
            var logicalOperator = isAnd ? "AND" : "OR";

            _parameterManager.AddLogicalCondition(logicalOperator);
        }

        public bool CanProcess(Expression node)
        {
            return node is BinaryExpression;
        }
    }
}
