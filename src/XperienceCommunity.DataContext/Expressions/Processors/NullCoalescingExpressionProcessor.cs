using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Expressions.Processors;

internal sealed class NullCoalescingExpressionProcessor : IExpressionProcessor<BinaryExpression>
{
    private readonly IExpressionContext _context;

    public NullCoalescingExpressionProcessor(IExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public bool CanProcess(Expression node)
    {
        return node is BinaryExpression binary && binary.NodeType == ExpressionType.Coalesce;
    }

    public void Process(BinaryExpression node)
    {
        if (node.NodeType != ExpressionType.Coalesce)
            throw new InvalidExpressionFormatException("Expected coalesce expression (x ?? y).");

        // For null coalescing (x ?? y), we need to handle cases where x is null
        // This is complex because it involves conditional logic

        var leftMemberName = ExtractMemberNameIfPossible(node.Left);
        var rightValue = ExtractValueIfPossible(node.Right);

        if (leftMemberName != null && rightValue != null)
        {
            // Simple case: member ?? constant
            // This translates to: WHERE (member IS NULL OR member = constant)
            // But since Kentico might not have OR logic easily accessible in single WhereAction,
            // we'll use a simplified approach
            _context.AddParameter(leftMemberName, rightValue);
            _context.AddWhereAction(w => w.WhereEquals(leftMemberName, rightValue));
        }
        else
        {
            throw new NotSupportedException("Null coalescing operator (??) is only supported for simple member ?? constant patterns.");
        }
    }

    private static string? ExtractMemberNameIfPossible(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => member.Member.Name,
            ParameterExpression param => param.Name,
            _ => null
        };
    }

    private static object? ExtractValueIfPossible(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => constant.Value,
            _ => null
        };
    }
}
