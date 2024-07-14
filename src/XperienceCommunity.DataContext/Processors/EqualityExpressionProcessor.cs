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
            if (node.Left is MemberExpression leftMember && node.Right is ConstantExpression rightConstant)
            {
                ProcessMemberToConstant(leftMember, rightConstant);
            }
            else if (node.Left is ConstantExpression leftConstant && node.Right is MemberExpression rightMember)
            {
                ProcessMemberToConstant(rightMember, leftConstant);
            }
            else if (node.Left is MemberExpression memberLeft && node.Right is UnaryExpression unaryRight)
            {
                ProcessMemberToUnary(memberLeft, unaryRight);
            }
            else if (node.Left is UnaryExpression unaryLeft && node.Right is MemberExpression memberRight)
            {
                ProcessMemberToUnary(memberRight, unaryLeft);
            }
            else if (node.Left is MemberExpression leftMemberExpression && node.Right is MemberExpression rightMemberExpression)
            {
                ProcessMemberToMember(leftMemberExpression, rightMemberExpression);
            }
            else
            {
                throw new InvalidOperationException("Invalid expression format for equality comparison.");
            }
        }

        private void ProcessMemberToConstant(MemberExpression member, ConstantExpression constant)
        {
            if (_isEqual)
            {
                _parameterManager.AddEqualsCondition(member.Member.Name, constant.Value);
            }
            else
            {
                _parameterManager.AddNotEqualsCondition(member.Member.Name, constant.Value);
            }
        }

        private void ProcessMemberToUnary(MemberExpression member, UnaryExpression unary)
        {
            if (unary.Operand is ConstantExpression constant)
            {
                ProcessMemberToConstant(member, constant);
            }
            else
            {
                throw new InvalidOperationException("Invalid unary expression format for equality comparison.");
            }
        }

        private void ProcessMemberToMember(MemberExpression leftMember, MemberExpression rightMember)
        {
            // Evaluate the right member to get its value
            var lambda = Expression.Lambda(rightMember);
            var compiled = lambda.Compile();
            var rightValue = compiled.DynamicInvoke();

            if (_isEqual)
            {
                _parameterManager.AddEqualsCondition(leftMember.Member.Name, rightValue);
            }
            else
            {
                _parameterManager.AddNotEqualsCondition(leftMember.Member.Name, rightValue);
            }
        }
    }
}
