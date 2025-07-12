using System.Linq.Expressions;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Expressions.Processors;

/// <summary>
/// Enhanced collection processor that optimizes collection operations using Kentico's WhereIn API
/// </summary>
internal sealed class EnhancedCollectionProcessor : IExpressionProcessor<MethodCallExpression>
{
    private readonly IExpressionContext _context;

    public EnhancedCollectionProcessor(IExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public bool CanProcess(Expression node)
    {
        return node is MethodCallExpression methodCall &&
               IsCollectionMethod(methodCall.Method.Name) &&
               (IsEnumerableMethod(methodCall) || IsInstanceCollectionMethod(methodCall));
    }

    public void Process(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(Enumerable.Contains):
                ProcessContains(node);
                break;
            case nameof(Enumerable.Any):
                ProcessAny(node);
                break;
            case nameof(Enumerable.All):
                ProcessAll(node);
                break;
            default:
                throw new UnsupportedExpressionException($"Collection method '{node.Method.Name}' is not supported", node);
        }
    }

    private static bool IsCollectionMethod(string methodName)
        => methodName is nameof(Enumerable.Contains) or 
                        nameof(Enumerable.Any) or 
                        nameof(Enumerable.All);

    private static bool IsEnumerableMethod(MethodCallExpression node)
        => node.Method.DeclaringType == typeof(Enumerable);

    private static bool IsInstanceCollectionMethod(MethodCallExpression node)
    {
        var declaringType = node.Method.DeclaringType;
        return declaringType != null &&
               node.Method.Name == nameof(Enumerable.Contains) &&
               (typeof(System.Collections.ICollection).IsAssignableFrom(declaringType) ||
                IsGenericCollectionType(declaringType));
    }

    private static bool IsGenericCollectionType(Type type)
    {
        if (!type.IsGenericType) return false;
        
        var genericDef = type.GetGenericTypeDefinition();
        return genericDef == typeof(List<>) ||
               genericDef == typeof(HashSet<>) ||
               genericDef == typeof(Queue<>) ||
               genericDef == typeof(Stack<>) ||
               genericDef == typeof(IEnumerable<>) ||
               genericDef == typeof(ICollection<>) ||
               genericDef == typeof(IList<>);
    }

    private void ProcessContains(MethodCallExpression node)
    {
        if (node.Arguments.Count == 2)
        {
            // Static Enumerable.Contains(collection, value)
            ProcessStaticContains(node);
        }
        else if (node.Arguments.Count == 1 && node.Object != null)
        {
            // Instance collection.Contains(value)
            ProcessInstanceContains(node);
        }
        else
        {
            throw new InvalidExpressionFormatException("Contains method requires 1 or 2 arguments", node);
        }
    }

    private void ProcessStaticContains(MethodCallExpression node)
    {
        var collectionExpr = node.Arguments[0];
        var valueExpr = node.Arguments[1];

        if (valueExpr is not MemberExpression memberExpr)
        {
            throw new InvalidExpressionFormatException("Contains value must be a member expression", node);
        }

        var paramName = ExtractMemberName(memberExpr);
        var collectionValue = ExtractCollectionValue(collectionExpr);

        AddOptimizedWhereIn(paramName, collectionValue, false);
    }

    private void ProcessInstanceContains(MethodCallExpression node)
    {
        var collectionExpr = node.Object!;
        var valueExpr = node.Arguments[0];

        if (valueExpr is not MemberExpression memberExpr)
        {
            throw new InvalidExpressionFormatException("Contains value must be a member expression", node);
        }

        var paramName = ExtractMemberName(memberExpr);
        var collectionValue = ExtractCollectionValue(collectionExpr);

        AddOptimizedWhereIn(paramName, collectionValue, false);
    }

    private void ProcessAny(MethodCallExpression node)
    {
        if (node.Arguments.Count == 1)
        {
            // Simple Any() without predicate on collection
            var collectionExpr = node.Arguments[0];
            
            // This typically appears in expressions like "x.SomeCollectionProperty.Any()"
            // We need to check if the collection property has any elements
            if (collectionExpr is MemberExpression memberExpr)
            {
                var paramName = ExtractMemberName(memberExpr);
                _context.AddWhereAction(w => w.WhereNotNull(paramName));
            }
            else
            {
                throw new InvalidExpressionFormatException("Any() requires a member expression representing a collection property", node);
            }
        }
        else if (node.Arguments.Count == 2)
        {
            // Any(predicate) - would need recursive processing
            throw new NotSupportedException("Any() with predicate is not currently supported");
        }
        else
        {
            throw new InvalidExpressionFormatException("Any method requires 1 or 2 arguments", node);
        }
    }

    private void ProcessAll(MethodCallExpression node)
    {
        // All() is complex to implement efficiently in SQL
        // For now, throw not supported
        throw new NotSupportedException("All() method is not currently supported due to SQL complexity");
    }

    /// <summary>
    /// Optimized WhereIn implementation that uses Kentico's native WhereIn when possible
    /// </summary>
    private void AddOptimizedWhereIn(string paramName, object? collectionValue, bool isNegated)
    {
        _context.AddParameter(paramName, collectionValue);

        if (collectionValue is not System.Collections.IEnumerable enumerable)
        {
            _context.AddWhereAction(w =>
            {
                if (isNegated)
                    w.WhereNotEquals(paramName, collectionValue);
                else
                    w.WhereEquals(paramName, collectionValue);
            });
            return;
        }

        var values = enumerable.Cast<object>().ToArray();

        if (values.Length == 0)
        {
            // Empty collection
            _context.AddWhereAction(w => w.WhereEquals("1", isNegated ? 1 : 0));
            return;
        }

        if (values.Length == 1)
        {
            // Single value
            _context.AddWhereAction(w =>
            {
                if (isNegated)
                    w.WhereNotEquals(paramName, values[0]);
                else
                    w.WhereEquals(paramName, values[0]);
            });
            return;
        }

        // Multiple values - use native WhereIn if available
        _context.AddWhereAction(w =>
        {
            try
            {
                // Try to use Kentico's native WhereIn based on collection type
                if (TryUseNativeWhereIn(w, paramName, values, isNegated))
                    return;
                
                // Fallback to chained OR/AND conditions
                UseFallbackChaining(w, paramName, values, isNegated);
            }
            catch (Exception)
            {
                // If native WhereIn fails, use fallback
                UseFallbackChaining(w, paramName, values, isNegated);
            }
        });
    }

    private static bool TryUseNativeWhereIn(WhereParameters w, string paramName, object[] values, bool isNegated)
    {
        if (values.Length == 0) return false;
        
        var firstValue = values[0];

        if (firstValue == null) return false;

        try
        {
            switch (firstValue)
            {
                case int when values.All(v => v is int):
                    var intValues = values.Cast<int>().ToList();
                    if (isNegated)
                    {
                        // Try WhereNotIn if available, otherwise use negated logic
                        w.WhereNotIn(paramName, intValues);
                    }
                    else
                    {
                        w.WhereIn(paramName, intValues);
                    }
                    return true;

                case string when values.All(v => v is string):
                    var stringValues = values.Cast<string>().ToList();
                    if (isNegated)
                    {
                        w.WhereNotIn(paramName, stringValues);
                    }
                    else
                    {
                        w.WhereIn(paramName, stringValues);
                    }
                    return true;

                case Guid when values.All(v => v is Guid):
                    var guidValues = values.Cast<Guid>().ToList();
                    if (isNegated)
                    {
                        w.WhereNotIn(paramName, guidValues);
                    }
                    else
                    {
                        w.WhereIn(paramName, guidValues);
                    }
                    return true;

                default:
                    return false; // Unsupported type, use fallback
            }
        }
        catch (Exception)
        {
            return false; // WhereNotIn might not be available, use fallback
        }
    }

    private static void UseFallbackChaining(WhereParameters w, string paramName, object[] values, bool isNegated)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i == 0)
            {
                if (isNegated)
                    w.WhereNotEquals(paramName, values[i]);
                else
                    w.WhereEquals(paramName, values[i]);
            }
            else
            {
                if (isNegated)
                    w.And().WhereNotEquals(paramName, values[i]);
                else
                    w.Or().WhereEquals(paramName, values[i]);
            }
        }
    }

    private static string ExtractMemberName(Expression expr)
    {
        return expr switch
        {
            MemberExpression member => member.Member.Name,
            _ => throw new InvalidExpressionFormatException($"Expected member expression, got {expr.GetType().Name}")
        };
    }

    private static object? ExtractCollectionValue(Expression expr)
    {
        return expr switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression member => Expression.Lambda(member).Compile().DynamicInvoke(),
            MethodCallExpression method => Expression.Lambda(method).Compile().DynamicInvoke(),
            _ => throw new InvalidExpressionFormatException($"Unsupported collection expression type: {expr.GetType().Name}")
        };
    }
}
