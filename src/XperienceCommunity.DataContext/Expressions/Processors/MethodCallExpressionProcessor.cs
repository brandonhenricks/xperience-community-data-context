using System.Collections.Generic;
using System.Linq.Expressions;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Expressions.Processors;

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
        if (node is not MethodCallExpression methodCallExpression)
            return false;
        
        if (!IsSupportedMethodName(methodCallExpression.Method.Name))
            return false;

        var declaringType = methodCallExpression.Method.DeclaringType;

        return IsSupportedStringMethod(declaringType)
            || IsSupportedEnumerableMethod(declaringType)
            || IsSupportedQueryableMethod(declaringType)
            || IsSupportedInstanceContains(methodCallExpression);
    }

    public void Process(MethodCallExpression node)
    {
        var declaringType = node.Method.DeclaringType;

        if (IsSupportedStringMethod(declaringType))
        {
            ProcessStringMethod(node);
        }
        else if (IsSupportedEnumerableMethod(declaringType))
        {
            ProcessEnumerableMethod(node);
        }
        else if (IsSupportedQueryableMethod(declaringType))
        {
            ProcessQueryableMethod(node);
        }
        else if (IsSupportedInstanceContains(node))
        {
            ProcessEnumerableContains(node);
        }
        else
        {
            throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
        }
    }

    private static bool IsSupportedStringMethod(Type? declaringType)
        => declaringType == typeof(string);

    private static bool IsSupportedEnumerableMethod(Type? declaringType)
        => declaringType == typeof(Enumerable);

    private static bool IsSupportedQueryableMethod(Type? declaringType)
        => declaringType == typeof(Queryable);

    private static bool IsSupportedMethodName(string methodName)
        => methodName == nameof(string.Contains) ||
           methodName == nameof(string.StartsWith) ||
           methodName == nameof(Enumerable.Contains) ||
           methodName == nameof(Queryable.Where) ||
           methodName == nameof(Queryable.Select);

    private static bool IsSupportedInstanceContains(MethodCallExpression node)
    {
        var declaringType = node.Method.DeclaringType;
        return node.Method.Name == nameof(Enumerable.Contains) &&
            declaringType != null &&
            (
                typeof(System.Collections.ICollection).IsAssignableFrom(declaringType) ||
                (declaringType.IsGenericType &&
                    (
                        declaringType.GetGenericTypeDefinition() == typeof(HashSet<>) ||
                        declaringType.GetGenericTypeDefinition() == typeof(Queue<>) ||
                        declaringType.GetGenericTypeDefinition() == typeof(Stack<>)
                    )
                )
            );
    }

    private void ProcessStringMethod(MethodCallExpression node)
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

    private void ProcessEnumerableMethod(MethodCallExpression node)
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

    private void ProcessQueryableMethod(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(Queryable.Where):
                ProcessQueryableWhere(node);
                break;

            case nameof(Queryable.Select):
                ProcessQueryableSelect(node);
                break;

            default:
                throw new NotSupportedException($"The method call '{node.Method.Name}' is not supported.");
        }
    }

    private void ProcessQueryableWhere(MethodCallExpression node)
    {
        if (node.Arguments.Count != 2)
            throw new InvalidExpressionFormatException("Queryable.Where expects two arguments.");

        var predicate = node.Arguments[1];
        var lambda = ExtractLambda(predicate);
        if (lambda == null)
            throw new InvalidExpressionFormatException("Queryable.Where expects a lambda predicate.");

        _context.PushLogicalGrouping("(");
        _context.PushMember(lambda.Parameters[0].Name ?? "x");
        _context.AddWhereAction(w => { });

        // Placeholder for recursive processing
        if (_context is IExpressionProcessor processor)
        {
            processor.CanProcess(lambda.Body);
        }

        _context.PopMember();
        _context.PopLogicalGrouping();
    }

    private void ProcessQueryableSelect(MethodCallExpression node)
    {
        if (node.Arguments.Count != 2)
            throw new InvalidExpressionFormatException("Queryable.Select expects two arguments.");

        var selector = node.Arguments[1];
        var lambda = ExtractLambda(selector);
        if (lambda == null)
            throw new InvalidExpressionFormatException("Queryable.Select expects a lambda selector.");

        _context.PushMember(lambda.Parameters[0].Name ?? "x");
        _context.PopMember();
    }

    private static LambdaExpression? ExtractLambda(Expression expr)
    {
        return expr switch
        {
            UnaryExpression unary when unary.Operand is LambdaExpression lambdaExpr => lambdaExpr,
            LambdaExpression directLambda => directLambda,
            _ => null
        };
    }

    private void ProcessStringContains(MethodCallExpression node)
    {
        if (node.Object is not MemberExpression member ||
            node.Arguments.Count == 0 ||
            node.Arguments[0] is not ConstantExpression constant)
        {
            throw new InvalidExpressionFormatException("Invalid expression format for string.Contains.");
        }

        var paramName = member.Member?.Name ?? throw new InvalidExpressionFormatException("Member name cannot be null.");
        var value = constant.Value?.ToString() ?? string.Empty;

        _context.AddParameter(paramName, value);
        _context.AddWhereAction(w => w.WhereContains(paramName, value));
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
        var (collectionExpr, valueExpr) = GetCollectionAndValueExpressions(node);

        var (collectionValue, paramName) = GetCollectionValueAndName(collectionExpr);

        var value = GetValueFromExpression(valueExpr);

        var declaredType = collectionExpr.Type;

        _context.AddParameter(paramName, collectionValue);

        AddWhereInTyped(paramName, collectionValue, declaredType);
    }

    private static (Expression collectionExpr, Expression valueExpr) GetCollectionAndValueExpressions(MethodCallExpression node)
    {
        if (node.Arguments.Count == 2)
        {
            return (node.Arguments[0], node.Arguments[1]);
        }
        else if (node.Arguments.Count == 1 && node.Object != null)
        {
            return (node.Object, node.Arguments[0]);
        }
        else
        {
            throw new InvalidExpressionFormatException("Invalid Enumerable.Contains expression format.");
        }
    }

    private static (object? collectionValue, string paramName) GetCollectionValueAndName(Expression collectionExpr)
    {
        if (collectionExpr is ConstantExpression constCollection)
        {
            return (constCollection.Value, $"p_{Guid.NewGuid():N}");
        }
        else if (collectionExpr is MemberExpression memberCollection)
        {
            var lambda = Expression.Lambda(memberCollection);
            var value = lambda.Compile().DynamicInvoke();
            return (value, memberCollection.Member.Name);
        }
        else
        {
            throw new NotSupportedException("Only constant or member collections are supported in Enumerable.Contains.");
        }
    }

    private static object? GetValueFromExpression(Expression valueExpr)
    {
        if (valueExpr is ConstantExpression constValue)
        {
            return constValue.Value;
        }
        else if (valueExpr is MemberExpression memberValue)
        {
            // If the member is a property of a parameter (e.g., x.Age), we cannot evaluate it here.
            if (memberValue.Expression is ParameterExpression)
            {
                // Return the member name or throw, depending on your use case.
                // For example, return the property name:
                return memberValue.Member.Name;
                // Or throw if you expect only constants:
                // throw new NotSupportedException("Cannot evaluate member access on parameter in Enumerable.Contains.");
            }
            else
            {
                var lambda = Expression.Lambda(memberValue);
                return lambda.Compile().DynamicInvoke();
            }
        }
        else
        {
            throw new NotSupportedException("Only constant or member values are supported in Enumerable.Contains.");
        }
    }

    private void AddWhereInTyped(string paramName, object? collectionValue, Type? declaredCollectionType = null)
    {
        // Always use declared type for element type resolution
        var collectionType = declaredCollectionType;

        Type? elementType = null;
        if (collectionType != null)
        {
            if (collectionType.IsArray)
            {
                elementType = collectionType.GetElementType();
            }
            else if (collectionType.IsGenericType)
            {
                elementType = collectionType.GetGenericArguments().FirstOrDefault();
            }
            else
            {
                var ienum = collectionType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                elementType = ienum?.GetGenericArguments().FirstOrDefault();
            }
        }

        if (elementType == null)
            throw new NotSupportedException("Collection must be a strongly-typed IEnumerable<T> for WhereIn.");

        // If the collection is empty, add an always-false where action
        if (collectionValue is System.Collections.IEnumerable enumerable && !enumerable.Cast<object>().Any())
        {
            var whereInMethod = typeof(WhereParameters)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "WhereIn" &&
                    m.GetParameters().Length == 2 &&
                    m.IsGenericMethodDefinition) ?? typeof(WhereParameters)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "WhereIn" &&
                    m.GetParameters().Length == 2);

            if (whereInMethod != null)
            {
                if (whereInMethod.IsGenericMethod)
                {
                    var genericMethod = whereInMethod.MakeGenericMethod(elementType);
                    var emptyArray = Array.CreateInstance(elementType, 0);
                    _context.AddWhereAction(w =>
                    {
                        genericMethod.Invoke(w, new object[] { paramName, emptyArray });
                    });
                    return;
                }
                else
                {
                    // Handle non-generic WhereIn method (e.g., WhereIn(string paramName, IEnumerable values))
                    var emptyList = Array.CreateInstance(elementType, 0);
                    _context.AddWhereAction(w =>
                    {
                        whereInMethod.Invoke(w, new object[] { paramName, emptyList });
                    });
                    return;
                }
            }
        }

        var whereIn = typeof(WhereParameters)
            .GetMethods()
            .FirstOrDefault(m =>
                m.Name == "WhereIn" &&
                m.GetParameters().Length == 2 &&
                m.IsGenericMethodDefinition) ?? typeof(WhereParameters)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "WhereIn" &&
                    m.GetParameters().Length == 2);

        if (whereIn != null)
        {
            if (whereIn.IsGenericMethod)
            {
                var genericMethod = whereIn.MakeGenericMethod(elementType);
                _context.AddWhereAction(w =>
                {
                    genericMethod.Invoke(w, new object[] { paramName, collectionValue });
                });
                return;
            }
            else
            {
                // Handle non-generic WhereIn method (e.g., WhereIn(string paramName, IEnumerable values))
                _context.AddWhereAction(w =>
                {
                    whereIn.Invoke(w, new object[] { paramName, collectionValue });
                });
                return;
            }
        }

        throw new NotSupportedException("Collection must be a strongly-typed IEnumerable<T> for WhereIn.");
    }
}
