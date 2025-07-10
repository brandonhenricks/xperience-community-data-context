using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors;

internal sealed class MethodCallExpressionProcessor : IExpressionProcessor<MethodCallExpression>
{
    private readonly ExpressionContext _context;

    public MethodCallExpressionProcessor(ExpressionContext context)
    {
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
            throw new InvalidOperationException("Invalid expression format for string.Contains.");
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
            _context.AddParameter(paramName, constantArg.Value);
            _context.AddWhereAction(w => w.WhereStartsWith(paramName, constantArg.Value?.ToString()));
        }
        else
        {
            throw new InvalidOperationException("Invalid expression format for string.StartsWith.");
        }
    }

    private void ProcessEnumerableContains(MethodCallExpression node)
    {
        // TODO: Implement collection Contains logic using ExpressionContext
        throw new NotSupportedException("Enumerable.Contains is not yet implemented in ExpressionContext.");
    }

    // Removed ProcessContainsMethod as it used QueryParameterManager. Refactor or reimplement as needed for ExpressionContext.
}
