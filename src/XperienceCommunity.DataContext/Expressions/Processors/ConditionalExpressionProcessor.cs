using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;

namespace XperienceCommunity.DataContext.Expressions.Processors;

internal sealed class ConditionalExpressionProcessor : IExpressionProcessor<ConditionalExpression>
{
    private readonly IExpressionContext _context;

    public ConditionalExpressionProcessor(IExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public bool CanProcess(Expression node)
    {
        return node is ConditionalExpression;
    }

    public void Process(ConditionalExpression node)
    {
        // Conditional expressions (x ? y : z) are complex and would typically require
        // CASE WHEN support in the underlying query system
        // For now, we'll only support simple constant scenarios

        if (node.Test is ConstantExpression testConstant && testConstant.Value is bool testValue)
        {
            // Simple case: constant ? x : y
            var selectedBranch = testValue ? node.IfTrue : node.IfFalse;

            if (selectedBranch is ConstantExpression resultConstant)
            {
                // The result is a constant, so we can evaluate it directly
                var paramName = $"conditional_{Guid.NewGuid():N}";
                _context.AddParameter(paramName, resultConstant.Value);
                _context.AddWhereAction(w => w.WhereEquals(paramName, resultConstant.Value));
            }
            else if (selectedBranch is MemberExpression memberResult)
            {
                // The result is a member access
                var memberName = memberResult.Member.Name;
                _context.PushMember(memberName);
            }
            else
            {
                throw new NotSupportedException("Conditional expression result must be a constant or member access.");
            }
        }
        else
        {
            throw new NotSupportedException("Conditional expressions (?:) are only supported with constant test conditions.");
        }
    }
}
