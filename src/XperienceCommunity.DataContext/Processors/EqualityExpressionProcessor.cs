using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors;

internal sealed class EqualityExpressionProcessor : IExpressionProcessor<BinaryExpression>
{
    private readonly ExpressionContext _context;
    private readonly bool _isEqual;

    public EqualityExpressionProcessor(ExpressionContext context, bool isEqual = true)
    {
        _context = context;
        _isEqual = isEqual;
    }

    public void Process(BinaryExpression node)
    {
        if (node is { Left: MemberExpression leftMember, Right: ConstantExpression rightConstant })
        {
            ProcessMemberToConstant(leftMember, rightConstant);
        }
        else if (node is { Left: ConstantExpression leftConstant, Right: MemberExpression rightMember })
        {
            ProcessMemberToConstant(rightMember, leftConstant);
        }
        else if (node is { Left: MemberExpression memberLeft, Right: UnaryExpression unaryRight })
        {
            ProcessMemberToUnary(memberLeft, unaryRight);
        }
        else if (node is { Left: UnaryExpression unaryLeft, Right: MemberExpression memberRight })
        {
            ProcessMemberToUnary(memberRight, unaryLeft);
        }
        else if (node is { Left: MemberExpression leftMemberExpression, Right: MemberExpression rightMemberExpression })
        {
            ProcessMemberToMember(leftMemberExpression, rightMemberExpression);
        }
        else if (node is { Left: ConstantExpression leftConstantExpression, Right: ConstantExpression rightConstantExpression })
        {
            ProcessConstantToConstant(leftConstantExpression, rightConstantExpression);
        }
        else
        {
            throw new InvalidOperationException("Invalid expression format for equality comparison.");
        }
    }

    private void ProcessConstantToConstant(ConstantExpression leftConstant, ConstantExpression rightConstant)
    {
        bool result = Equals(leftConstant.Value, rightConstant.Value);
        _context.AddWhereAction(w =>
        {
            if (_isEqual)
            {
                w.WhereEquals(result.ToString(), true);
            }
            else
            {
                w.WhereNotEquals(result.ToString(), false);
            }
        });
    }

    private void ProcessMemberToConstant(MemberExpression member, ConstantExpression constant)
    {
        var paramName = member.Member.Name;
        _context.AddParameter(paramName, constant.Value);
        if (_isEqual)
        {
            _context.AddWhereAction(w => w.WhereEquals(paramName, constant.Value));
        }
        else
        {
            _context.AddWhereAction(w => w.WhereNotEquals(paramName, constant.Value));
        }
    }

    private void ProcessMemberToUnary(MemberExpression member, UnaryExpression unary)
    {
        if (unary.Operand is ConstantExpression constant)
        {
            ProcessMemberToConstant(member, constant);
        }
        else
        {
            throw new InvalidOperationException("Invalid unary expression format for equality comparison.");
        }
    }

    private void ProcessMemberToMember(MemberExpression leftMember, MemberExpression rightMember)
    {
        // Evaluate the right member to get its value
        var lambda = Expression.Lambda(rightMember);
        var compiled = lambda.Compile();
        var rightValue = compiled.DynamicInvoke();

        var paramName = leftMember.Member.Name;
        _context.AddParameter(paramName, rightValue);
        if (_isEqual)
        {
            _context.AddWhereAction(w => w.WhereEquals(paramName, rightValue));
        }
        else
        {
            _context.AddWhereAction(w => w.WhereNotEquals(paramName, rightValue));
        }
    }

    public bool CanProcess(Expression node)
    {
        // Align logic with Process: only BinaryExpressions with supported left/right types
        if (node is BinaryExpression binary)
        {
            return
                (binary.Left is MemberExpression && binary.Right is ConstantExpression) ||
                (binary.Left is ConstantExpression && binary.Right is MemberExpression) ||
                (binary.Left is MemberExpression && binary.Right is UnaryExpression) ||
                (binary.Left is UnaryExpression && binary.Right is MemberExpression) ||
                (binary.Left is MemberExpression && binary.Right is MemberExpression);
        }
        return false;
    }
}
