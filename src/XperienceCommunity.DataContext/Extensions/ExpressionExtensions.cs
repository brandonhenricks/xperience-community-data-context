using System.Linq.Expressions;

namespace XperienceCommunity.DataContext.Extensions
{
    internal static class ExpressionExtensions
    {
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
    }
}
