using System.Linq.Expressions;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors;

internal sealed class MethodCallExpressionProcessor : IExpressionProcessor<MethodCallExpression>
{
    private readonly IExpressionContext _context;

    public MethodCallExpressionProcessor(IExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    public bool CanProcess(Expression node)
    {
        return node is MethodCallExpression methodCallExpression &&
               (methodCallExpression.Method.DeclaringType == typeof(string) ||
                methodCallExpression.Method.DeclaringType == typeof(Enumerable) ||
                methodCallExpression.Method.DeclaringType == typeof(Queryable));
    }

    public void Process(MethodCallExpression node)
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
        else if (node.Method.DeclaringType == typeof(Queryable))
        {
            switch (node.Method.Name)
            {
                case nameof(Queryable.Where):
                    // TODO: Implement Queryable.Where support with ExpressionContext
                    break;

                case nameof(Queryable.Select):
                    // TODO: Implement Queryable.Select support with ExpressionContext
                    break;

                default:
                    throw new NotSupportedException($"The method call '{node.Method.Name}' is not supported.");
            }
        }
        else
        {
            throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
        }
    }

    private void ProcessStringContains(MethodCallExpression node)
    {
        if (node.Object is MemberExpression member && node.Arguments[0] is ConstantExpression constant)
        {
            var paramName = member.Member.Name;
            _context.AddParameter(paramName, constant.Value);
            _context.AddWhereAction(w => w.WhereContains(paramName, constant.Value?.ToString()));
        }
        else
        {
            throw new InvalidExpressionFormatException("Invalid expression format for string.Contains.");
        }
    }

    private void ProcessStringStartsWith(MethodCallExpression node)
    {
        if (node.Object is MemberExpression member && node.Arguments[0] is ConstantExpression constant)
        {
            var paramName = member.Member.Name;
            _context.AddParameter(paramName, constant.Value);
            _context.AddWhereAction(w => w.WhereStartsWith(paramName, constant.Value?.ToString()));
        }
        else if (node.Object is ConstantExpression constantObj && node.Arguments[0] is ConstantExpression constantArg)
        {
            // Handle case where the object is a constant (e.g., "Hello".StartsWith("He"))
            var paramName = constantObj.Value?.ToString();

            if (!string.IsNullOrEmpty(paramName))
            {
                paramName = $"p_{Guid.NewGuid():N}";

                _context.AddParameter(paramName, constantArg.Value);
                _context.AddWhereAction(w => w.WhereStartsWith(paramName, constantArg.Value?.ToString()));
            }
        }
        else
        {
            throw new InvalidExpressionFormatException("Invalid expression format for string.StartsWith.");
        }
    }

    private void ProcessEnumerableContains(MethodCallExpression node)
    {
        // Handles Enumerable.Contains(collection, value)
        // or collection.Contains(value)
        Expression? collectionExpr = null;
        Expression? valueExpr = null;

        if (node.Arguments.Count == 2)
        {
            // Static call: Enumerable.Contains(collection, value)
            collectionExpr = node.Arguments[0];
            valueExpr = node.Arguments[1];
        }
        else if (node.Arguments.Count == 1 && node.Object != null)
        {
            // Instance call: collection.Contains(value)
            collectionExpr = node.Object;
            valueExpr = node.Arguments[0];
        }
        else
        {
            throw new InvalidExpressionFormatException("Invalid Enumerable.Contains expression format.");
        }

        // Only support collection as ConstantExpression or MemberExpression for now
        object? collectionValue = null;
        string? paramName = null;

        if (collectionExpr is ConstantExpression constCollection)
        {
            collectionValue = constCollection.Value;
            paramName = $"p_{Guid.NewGuid():N}";
        }
        else if (collectionExpr is MemberExpression memberCollection)
        {
            // Try to evaluate the member expression
            var lambda = Expression.Lambda(memberCollection);
            collectionValue = lambda.Compile().DynamicInvoke();
            paramName = memberCollection.Member.Name;
        }
        else
        {
            throw new NotSupportedException("Only constant or member collections are supported in Enumerable.Contains.");
        }

        // Value to check for
        object? value = null;
        if (valueExpr is ConstantExpression constValue)
        {
            value = constValue.Value;
        }
        else if (valueExpr is MemberExpression memberValue)
        {
            var lambda = Expression.Lambda(memberValue);
            value = lambda.Compile().DynamicInvoke();
        }
        else
        {
            throw new NotSupportedException("Only constant or member values are supported in Enumerable.Contains.");
        }

        // The WhereIn method expects the collection as the second argument (ICollection<T>)
        // and the field/column name as the first argument (string)
        _context.AddParameter(paramName, collectionValue);

        // Try to cast collectionValue to a strongly-typed ICollection<T>
        void AddWhereInTyped(string paramName, object? collectionValue)
        {
            if (collectionValue is System.Collections.IEnumerable enumerable)
            {
                var elementType = collectionValue.GetType().IsArray
                    ? collectionValue.GetType().GetElementType()
                    : collectionValue.GetType().GetGenericArguments().FirstOrDefault();

                if (elementType != null)
                {
                    var whereInMethod = typeof(WhereParameters)
                        .GetMethods()
                        .FirstOrDefault(m => m.Name == "WhereIn" && m.GetParameters().Length == 2);

                    if (whereInMethod != null)
                    {
                        var genericMethod = whereInMethod.MakeGenericMethod(elementType);
                        _context.AddWhereAction(w =>
                        {
                            genericMethod.Invoke(w, new object[] { paramName, collectionValue });
                        });
                        return;
                    }
                }
            }
            throw new NotSupportedException("Collection must be a strongly-typed IEnumerable<T> for WhereIn.");
        }

        AddWhereInTyped(paramName, collectionValue);
    }

    // Removed ProcessContainsMethod as it used QueryParameterManager. Refactor or reimplement as needed for ExpressionContext.
}
