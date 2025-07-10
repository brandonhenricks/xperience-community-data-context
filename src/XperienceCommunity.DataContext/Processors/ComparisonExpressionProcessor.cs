using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors;

internal sealed class ComparisonExpressionProcessor : IExpressionProcessor<BinaryExpression>
{
    private readonly ExpressionContext _context;
    private readonly bool _isGreaterThan;
    private readonly bool _isEqual;

    public ComparisonExpressionProcessor(ExpressionContext context, bool isGreaterThan, bool isEqual = false)
    {
        _context = context;
        _isGreaterThan = isGreaterThan;
        _isEqual = isEqual;
    }

    public bool CanProcess(Expression node)
    {
        if (node is BinaryExpression binaryNode)
        {
            return binaryNode.Left is MemberExpression && binaryNode.Right is ConstantExpression;
        }

        return false;
    }

    public void Process(BinaryExpression node)
    {
        // Helper to extract member and constant from either side
        static (MemberExpression member, ConstantExpression constant, bool memberOnLeft) ExtractMemberAndConstant(BinaryExpression expr)
        {
            if (expr.Left is MemberExpression leftMember && expr.Right is ConstantExpression rightConstant)
                return (leftMember, rightConstant, true);
            if (expr.Right is MemberExpression rightMember && expr.Left is ConstantExpression leftConstant)
                return (rightMember, leftConstant, false);
            throw new InvalidOperationException("Expression must have a MemberExpression and a ConstantExpression.");
        }

        var (member, constant, memberOnLeft) = ExtractMemberAndConstant(node);
        var paramName = member.Member.Name;

        _context.AddParameter(paramName, constant.Value);

        // Determine comparison direction based on which side the member is on
        if (memberOnLeft)
        {
            if (_isGreaterThan && _isEqual)
            {
                _context.AddWhereAction(w => w.WhereGreaterOrEquals(paramName, constant.Value));
            }
            else if (_isGreaterThan)
            {
                _context.AddWhereAction(w => w.WhereGreater(paramName, constant.Value));
            }
            else if (_isEqual)
            {
                _context.AddWhereAction(w => w.WhereLessOrEquals(paramName, constant.Value));
            }
            else
            {
                _context.AddWhereAction(w => w.WhereLess(paramName, constant.Value));
            }
        }
        else
        {
            // Reverse the comparison if the member is on the right
            if (_isGreaterThan && _isEqual)
            {
                _context.AddWhereAction(w => w.WhereLessOrEquals(paramName, constant.Value));
            }
            else if (_isGreaterThan)
            {
                _context.AddWhereAction(w => w.WhereLess(paramName, constant.Value));
            }
            else if (_isEqual)
            {
                _context.AddWhereAction(w => w.WhereGreaterOrEquals(paramName, constant.Value));
            }
            else
            {
                _context.AddWhereAction(w => w.WhereGreater(paramName, constant.Value));
            }
        }
    }
}
