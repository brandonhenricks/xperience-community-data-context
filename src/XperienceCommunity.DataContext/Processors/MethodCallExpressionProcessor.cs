using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal class MethodCallExpressionProcessor : IExpressionProcessor<MethodCallExpression>
    {
        private readonly QueryParameterManager _parameterManager;

        public MethodCallExpressionProcessor(QueryParameterManager parameterManager)
        {
            _parameterManager = parameterManager;
        }

        public void Process(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                switch (node.Method.Name)
                {
                    case nameof(string.Contains):
                        _parameterManager.AddStringContains(node);
                        break;

                    case nameof(string.StartsWith):
                        _parameterManager.AddStringStartsWith(node);
                        break;

                    default:
                        throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
                }
            }
            else if (node.Method.DeclaringType == typeof(Enumerable))
            {
                if (node.Method.Name == nameof(Enumerable.Contains))
                {
                    _parameterManager.AddEnumerableContains(node);
                }
                else
                {
                    throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
                }
            }
            else if (node.Method.DeclaringType == typeof(Queryable))
            {
                switch (node.Method.Name)
                {
                    case nameof(Queryable.Where):
                        _parameterManager.AddQueryableWhere(node);
                        break;
                    case nameof(Queryable.Select):
                        _parameterManager.AddQueryableSelect(node);
                        break;
                    // Add other Queryable methods as needed
                    default:
                        throw new NotSupportedException($"The method call '{node.Method.Name}' is not supported.");
                }
            }
            else if (node.Method.Name == nameof(Enumerable.Contains))
            {
                _parameterManager.AddEnumerableContains(node);
            }
            else
            {
                throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
            }

        }

        private void ProcessContainsMethod(MethodCallExpression node, object?[] arguments)
        {
            if (node.Object is MemberExpression memberExpression)
            {
                if (arguments[0] == null)
                {
                    return;
                }
                // Handle cases like "x.Name.Contains("test")"
                _parameterManager.AddMethodCall("Contains", memberExpression.Member.Name, arguments[0]!);
            }
            else if (arguments.Length == 2 && arguments[1] is MemberExpression collectionMember)
            {
                if (arguments[0] == null)
                {
                    return;
                }
                // Handle cases like "collection.Contains(x.Name)"
                _parameterManager.AddMethodCall("Contains", collectionMember.Member.Name, arguments[0]!);
            }
            else
            {
                throw new InvalidOperationException("Invalid expression format for Contains method.");
            }
        }
    }
}
