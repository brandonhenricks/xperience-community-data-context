using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace XperienceCommunity.DataContext.Extensions;

internal static class ExpressionExtensions
{
    /// <summary>
    /// Extracts the values from a collection expression.
    /// </summary>
    /// <param name="collectionExpression">The collection expression.</param>
    /// <returns>The extracted values as an enumerable of objects.</returns>
    internal static IEnumerable<object>? ExtractCollectionValues(this MemberExpression collectionExpression)
    {
        if (collectionExpression.Expression == null)
        {
            return null;
        }

        var value = GetExpressionValue(collectionExpression.Expression);

        return ExtractValues(value);

    }

    /// <summary>
    /// Extracts the values from a field expression.
    /// </summary>
    /// <param name="fieldExpression">The field expression.</param>
    /// <returns>The extracted values as an enumerable of objects.</returns>
    internal static IEnumerable<object> ExtractFieldValues(this MemberExpression fieldExpression)
    {
        var value = GetExpressionValue(fieldExpression);
        return ExtractValues(value);
    }

    /// <summary>
    /// Gets the value of an expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The value of the expression.</returns>
    internal static object? GetExpressionValue(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression constantExpression:
                return constantExpression.Value!;

            case MemberExpression memberExpression:
                var container = GetExpressionValue(memberExpression.Expression!);
                var member = memberExpression.Member;
                return member switch
                {
                    FieldInfo fieldInfo => fieldInfo.GetValue(container),
                    PropertyInfo propertyInfo => propertyInfo.GetValue(container),
                    _ => throw new NotSupportedException(
                        $"The member type '{member.GetType().Name}' is not supported.")
                };
            default:
                throw new NotSupportedException(
                    $"The expression type '{expression.GetType().Name}' is not supported.");
        }
    }

    /// <summary>
    /// Gets the member name from a method call expression.
    /// </summary>
    /// <param name="methodCall">The method call expression.</param>
    /// <returns>The name of the member if it is a member expression; otherwise, null.</returns>
    internal static string? GetMemberNameFromMethodCall(this MethodCallExpression methodCall)
    {
        if (methodCall.Object is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        return null;
    }

    internal static IEnumerable<object> ExtractValues(this object? value)
    {
        if (value is null)
        {
            return [];
        }

        if (value is IEnumerable<object> objectEnumerable)
        {
            return objectEnumerable;
        }

        if (value is IEnumerable<int> intEnumerable)
        {
            return intEnumerable.Cast<object>();
        }

        if (value is IEnumerable<string> stringEnumerable)
        {
            return stringEnumerable.Cast<object>();
        }

        if (value is IEnumerable<Guid> guidEnumerable)
        {
            return guidEnumerable.Cast<object>();
        }

        if (value is IEnumerable enumerable)
        {
            var list = new List<object>();

            foreach (var item in enumerable)
            {
                var itemValues = ExtractValues(item);
                list.AddRange(itemValues);
            }

            return list;
        }

        // Check if the object has a property that is a collection
        var properties = value.GetType().GetProperties();

        var collectionProperty = Array.Find(properties, p =>
            p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (collectionProperty != null)
        {
            var collectionValue = collectionProperty.GetValue(value);
            return ExtractValues(collectionValue);
        }

        return [value];
    }

    /// <summary>
    /// Extracts the member name from various expression types, handling method calls and nested member access.
    /// </summary>
    /// <param name="expression">The expression to extract the member name from.</param>
    /// <returns>The member name, potentially with method-specific suffixes for transformations.</returns>
    internal static string ExtractMemberName(this Expression expression)
    {
        return expression switch
        {
            MemberExpression member => member.Member.Name,
            MethodCallExpression method when method.Method.Name is nameof(string.ToLower) or nameof(string.ToUpper) =>
                method.Object!.ExtractMemberName() + $"_{method.Method.Name.ToUpper()}",
            MethodCallExpression method when method.Method.Name == nameof(string.Trim) =>
                method.Object!.ExtractMemberName() + "_TRIMMED",
            ParameterExpression param => param.Name ?? "param",
            _ => throw new InvalidOperationException($"Cannot extract member name from expression type {expression.GetType().Name}")
        };
    }

    /// <summary>
    /// Extracts the member name safely, returning null if extraction fails.
    /// </summary>
    /// <param name="expression">The expression to extract the member name from.</param>
    /// <returns>The member name or null if extraction fails.</returns>
    internal static string? TryExtractMemberName(this Expression expression)
    {
        try
        {
            return expression.ExtractMemberName();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the full member access chain for nested property access (e.g., "Address.City").
    /// </summary>
    /// <param name="memberExpression">The member expression to analyze.</param>
    /// <returns>A list of member names in the access chain.</returns>
    internal static List<string> GetMemberAccessChain(this MemberExpression memberExpression)
    {
        var members = new List<string>();
        Expression? current = memberExpression;
        
        while (current is MemberExpression m)
        {
            members.Insert(0, m.Member.Name);
            current = m.Expression;
        }
        
        return members;
    }

    /// <summary>
    /// Gets the full member path as a dot-separated string (e.g., "Address.City").
    /// </summary>
    /// <param name="memberExpression">The member expression to analyze.</param>
    /// <returns>The full member path.</returns>
    internal static string GetFullMemberPath(this MemberExpression memberExpression)
    {
        var chain = memberExpression.GetMemberAccessChain();
        return string.Join(".", chain);
    }

    /// <summary>
    /// Extracts a constant value from an expression.
    /// </summary>
    /// <param name="expression">The expression to extract the value from.</param>
    /// <returns>The constant value.</returns>
    internal static object? ExtractConstantValue(this Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression member => Expression.Lambda(member).Compile().DynamicInvoke(),
            MethodCallExpression method => Expression.Lambda(method).Compile().DynamicInvoke(),
            _ => throw new InvalidOperationException($"Cannot extract constant value from expression type {expression.GetType().Name}")
        };
    }

    /// <summary>
    /// Safely extracts a constant value from an expression, returning null if extraction fails.
    /// </summary>
    /// <param name="expression">The expression to extract the value from.</param>
    /// <returns>The constant value or null if extraction fails.</returns>
    internal static object? TryExtractConstantValue(this Expression expression)
    {
        try
        {
            return expression.ExtractConstantValue();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if an expression represents a boolean member that can be used in logical operations.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression is a boolean member; otherwise, false.</returns>
    internal static bool IsBooleanMember(this Expression expression)
    {
        return expression is MemberExpression member && member.Type == typeof(bool);
    }

    /// <summary>
    /// Checks if an expression represents a boolean constant.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if the expression is a boolean constant; otherwise, false.</returns>
    internal static bool IsBooleanConstant(this Expression expression)
    {
        return expression is ConstantExpression constant && constant.Type == typeof(bool);
    }
}
