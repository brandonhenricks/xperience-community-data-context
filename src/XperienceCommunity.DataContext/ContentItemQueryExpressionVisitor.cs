using System.Linq.Expressions;
using System.Reflection;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Extensions;

namespace XperienceCommunity.DataContext
{
    public class ContentItemQueryExpressionVisitor : ExpressionVisitor
    {
        private readonly ContentTypeQueryParameters _queryParameters;
        private string? _currentMemberName;
        private object? _currentValue;

        public ContentItemQueryExpressionVisitor(ContentTypeQueryParameters queryParameters)
        {
            _queryParameters = queryParameters ?? throw new ArgumentNullException(nameof(queryParameters));
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    ProcessEquality(node);
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
                    ProcessLogicalAnd(node);
                    break;

                case ExpressionType.OrElse:
                    ProcessLogicalOr(node);
                    break;

                // Add support for more expression types
                default:
                    throw new NotSupportedException($"The binary expression type '{node.NodeType}' is not supported.");
            }

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _currentValue = node.Value;

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
            {
                _currentMemberName = node.Member.Name;
            }
            else
            {
                _currentValue = GetMemberValue(node);
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                switch (node.Method.Name)
                {
                    case nameof(string.Contains):
                        ProcessStringContains(node);
                        break;

                    case nameof(string.StartsWith):
                        ProcessStringStartsWith(node);
                        break;

                    default:
                        throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
                }
            }
            else if (node.Method.DeclaringType == typeof(Enumerable))
            {
                if (node.Method.Name == nameof(Enumerable.Contains))
                {
                    ProcessEnumerableContains(node);
                }
                else
                {
                    throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
                }
            }
            else
            {
                throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
            }

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert)
            {
                Visit(node.Operand);
            }
            else
            {
                throw new NotSupportedException($"The unary expression type '{node.NodeType}' is not supported.");
            }

            return node;
        }


        private static object? GetMemberValue(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression constantExpression:
                    return constantExpression.Value;

                case MemberExpression memberExpression:
                    var member = memberExpression.Member;
                    var objectValue = GetMemberValue(memberExpression.Expression!); // Recursively process the expression

                    if (objectValue == null)
                        throw new InvalidOperationException("The target object for the member expression is null.");

                    return member switch
                    {
                        System.Reflection.FieldInfo fieldInfo => fieldInfo.GetValue(objectValue),
                        PropertyInfo propertyInfo => propertyInfo.GetValue(objectValue),
                        _ => throw new NotSupportedException(
                            $"The member type '{member.GetType().Name}' is not supported.")
                    };

                case UnaryExpression unaryExpression when unaryExpression.NodeType == ExpressionType.Convert:
                    return GetMemberValue(unaryExpression.Operand);

                case ParameterExpression parameterExpression:
                    // Handle ParameterExpression by returning null as it should be handled in its context
                    return null;

                default:
                    throw new NotSupportedException(
                        $"The expression type '{expression.GetType().Name}' is not supported.");
            }
        }

        private static object? GetMethodCallValue(MethodCallExpression methodCall)
        {
            // Evaluate the method call expression to get the resulting value
            var lambda = Expression.Lambda(methodCall).Compile();

            return lambda.DynamicInvoke();
        }

        private void ProcessComparison(BinaryExpression node, bool isGreaterThan, bool isEqual = false)
        {
            if (node.Left is MemberExpression left)
            {
                if (node.Right is ConstantExpression right)
                {
                    if (isGreaterThan)
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where => where.WhereGreaterOrEquals(left.Member.Name, right.Value));
                        }
                        else
                        {
                            _queryParameters.Where(where => where.WhereGreater(left.Member.Name, right.Value));
                        }
                    }
                    else
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where => where.WhereLessOrEquals(left.Member.Name, right.Value));
                        }
                        else
                        {
                            _queryParameters.Where(where => where.WhereLess(left.Member.Name, right.Value));
                        }
                    }
                }
                else if (node.Right is MemberExpression rightMember)
                {
                    if (isGreaterThan)
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where =>
                                where.WhereGreaterOrEquals(left.Member.Name, GetMemberValue(rightMember)));
                        }
                        else
                        {
                            _queryParameters.Where(where =>
                                where.WhereGreater(left.Member.Name, GetMemberValue(rightMember)));
                        }
                    }
                    else
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where =>
                                where.WhereLessOrEquals(left.Member.Name, GetMemberValue(rightMember)));
                        }
                        else
                        {
                            _queryParameters.Where(where =>
                                where.WhereLess(left.Member.Name, GetMemberValue(rightMember)));
                        }
                    }
                }
                else if (node.Right is UnaryExpression rightUnary && rightUnary.NodeType == ExpressionType.Convert)
                {
                    var value = GetMemberValue(rightUnary.Operand);
                    if (isGreaterThan)
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where => where.WhereGreaterOrEquals(left.Member.Name, value));
                        }
                        else
                        {
                            _queryParameters.Where(where => where.WhereGreater(left.Member.Name, value));
                        }
                    }
                    else
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where => where.WhereLessOrEquals(left.Member.Name, value));
                        }
                        else
                        {
                            _queryParameters.Where(where => where.WhereLess(left.Member.Name, value));
                        }
                    }
                }
                else if (node.Right is MethodCallExpression rightMethod)
                {
                    var value = GetMethodCallValue(rightMethod);
                    if (isGreaterThan)
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where => where.WhereGreaterOrEquals(left.Member.Name, value));
                        }
                        else
                        {
                            _queryParameters.Where(where => where.WhereGreater(left.Member.Name, value));
                        }
                    }
                    else
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where => where.WhereLessOrEquals(left.Member.Name, value));
                        }
                        else
                        {
                            _queryParameters.Where(where => where.WhereLess(left.Member.Name, value));
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException(
                        $"The right expression type '{node.Right.GetType().Name}' is not supported.");
                }
            }
            else if (node.Left is MethodCallExpression leftMethod && node.Right is ConstantExpression rightConst)
            {
                var memberName = leftMethod.GetMemberNameFromMethodCall();

                if (memberName != null)
                {
                    if (isGreaterThan)
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where => where.WhereGreaterOrEquals(memberName, rightConst.Value));
                        }
                        else
                        {
                            _queryParameters.Where(where => where.WhereGreater(memberName, rightConst.Value));
                        }
                    }
                    else
                    {
                        if (isEqual)
                        {
                            _queryParameters.Where(where => where.WhereLessOrEquals(memberName, rightConst.Value));
                        }
                        else
                        {
                            _queryParameters.Where(where => where.WhereLess(memberName, rightConst.Value));
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException(
                        $"The left method call expression '{leftMethod}' is not supported.");
                }
            }
            else
            {
                throw new NotSupportedException(
                    $"The left expression type '{node.Left.GetType().Name}' is not supported.");
            }
        }

        private void ProcessEnumerableContains(MethodCallExpression node)
        {
            if (node.Arguments[0] is MemberExpression memberExpression &&
                node.Arguments[1] is ConstantExpression listExpression)
            {
                var columnName = memberExpression.Member.Name;
                var values = (IEnumerable<object>)listExpression.Value!;

                if (listExpression.Type.GenericTypeArguments[0] == typeof(int))
                {
                    _queryParameters.Where(where => where.WhereIn(columnName, values.Cast<int>().ToArray()));
                }
                else if (listExpression.Type.GenericTypeArguments[0] == typeof(string))
                {
                    _queryParameters.Where(where => where.WhereIn(columnName, values.Cast<string>().ToArray()));
                }
                else if (listExpression.Type.GenericTypeArguments[0] == typeof(Guid))
                {
                    _queryParameters.Where(where => where.WhereIn(columnName, values.Cast<Guid>().ToArray()));
                }
            }
        }

        private void ProcessEquality(BinaryExpression node)
        {
            if (node.Left is MemberExpression left)
            {
                if (node.Right is ConstantExpression right)
                {
                    _queryParameters.Where(where => where.WhereEquals(left.Member.Name, right.Value));
                }
                else if (node.Right is MemberExpression rightMember)
                {
                    _queryParameters.Where(where => where.WhereEquals(left.Member.Name, GetMemberValue(rightMember)));
                }
                else if (node.Right is UnaryExpression rightUnary && rightUnary.NodeType == ExpressionType.Convert)
                {
                    var value = GetMemberValue(rightUnary.Operand);
                    _queryParameters.Where(where => where.WhereEquals(left.Member.Name, value));
                }
                else if (node.Right is MethodCallExpression rightMethod)
                {
                    var value = GetMethodCallValue(rightMethod);
                    _queryParameters.Where(where => where.WhereEquals(left.Member.Name, value));
                }
                else
                {
                    throw new NotSupportedException(
                        $"The right expression type '{node.Right.GetType().Name}' is not supported.");
                }
            }
            else if (node.Left is MethodCallExpression leftMethod && node.Right is ConstantExpression rightConst)
            {
                var value = GetMethodCallValue(leftMethod);

                _queryParameters.Where(where => where.WhereEquals(value?.ToString(), rightConst.Value));
            }
            else
            {
                throw new NotSupportedException(
                    $"The left expression type '{node.Left.GetType().Name}' is not supported.");
            }
        }

        private void ProcessLogicalAnd(BinaryExpression node)
        {
            Visit(node.Left);
            _queryParameters.Where(where => where.And());
            Visit(node.Right);
        }

        private void ProcessLogicalOr(BinaryExpression node)
        {
            Visit(node.Left);
            _queryParameters.Where(where => where.Or());
            Visit(node.Right);
        }

        private void ProcessStringContains(MethodCallExpression node)
        {
            if (node.Object is MemberExpression member && node.Arguments[0] is ConstantExpression constant)
            {
                _queryParameters.Where(where => where.WhereContains(member.Member.Name, constant.Value?.ToString()));
            }
        }

        private void ProcessStringStartsWith(MethodCallExpression node)
        {
            if (node.Object is MemberExpression member && node.Arguments[0] is ConstantExpression constant)
            {
                _queryParameters.Where(where => where.WhereStartsWith(member.Member.Name, constant.Value?.ToString()));
            }
        }
    }
}
