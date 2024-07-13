using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace XperienceCommunity.DataContext.Extensions
{
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Extracts the values from a collection expression.
        /// </summary>
        /// <param name="collectionExpression">The collection expression.</param>
        /// <returns>The extracted values as an enumerable of objects.</returns>
        internal static IEnumerable<object>? ExtractCollectionValues(this MemberExpression collectionExpression)
        {
            if (collectionExpression.Expression != null)
            {
                var value = GetExpressionValue(collectionExpression.Expression);

                return ExtractValues(value);
            }

            return null;
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
                    switch (member)
                    {
                        case FieldInfo fieldInfo:
                            return fieldInfo.GetValue(container);

                        case PropertyInfo propertyInfo:
                            return propertyInfo.GetValue(container);

                        default:
                            throw new NotSupportedException(
                                $"The member type '{member.GetType().Name}' is not supported.");
                    }
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

        private static IEnumerable<object> ExtractValues(object? value)
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

            var collectionProperty = properties.FirstOrDefault(p =>
                p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (collectionProperty != null)
            {
                var collectionValue = collectionProperty.GetValue(value);
                return ExtractValues(collectionValue);
            }

            return [value];
        }
    }
}
