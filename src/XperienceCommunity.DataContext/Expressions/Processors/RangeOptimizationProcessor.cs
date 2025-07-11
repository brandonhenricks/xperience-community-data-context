using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Expressions.Processors;

/// <summary>
/// Optimizes range expressions (x &gt;= min &amp;&amp; x &lt;= max) into WhereBetween operations when possible
/// </summary>
internal sealed class RangeOptimizationProcessor : IExpressionProcessor<BinaryExpression>
{
    private readonly IExpressionContext _context;
    private readonly Func<Expression, Expression> _visitFunction;

    public RangeOptimizationProcessor(IExpressionContext context, Func<Expression, Expression> visitFunction)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(visitFunction);
        
        _context = context;
        _visitFunction = visitFunction;
    }

    public bool CanProcess(Expression node)
    {
        return node is BinaryExpression { NodeType: ExpressionType.AndAlso } binary &&
               IsRangeExpression(binary);
    }

    public void Process(BinaryExpression node)
    {
        if (!IsRangeExpression(node))
        {
            // Fall back to normal processing
            _visitFunction(node);
            return;
        }

        var rangeInfo = ExtractRangeInfo(node);
        if (rangeInfo == null)
        {
            // Fall back to normal processing
            _visitFunction(node);
            return;
        }

        // Create optimized between operation
        CreateBetweenOperation(rangeInfo);
    }

    private static bool IsRangeExpression(BinaryExpression node)
    {
        if (node.NodeType != ExpressionType.AndAlso)
            return false;

        // Check if this is a range pattern: (x >= min && x <= max) or (x > min && x < max)
        return (IsComparisonExpression(node.Left) && IsComparisonExpression(node.Right)) &&
               HaveSameMember(node.Left as BinaryExpression, node.Right as BinaryExpression);
    }

    private static bool IsComparisonExpression(Expression expr)
    {
        return expr is BinaryExpression binary &&
               binary.NodeType is ExpressionType.GreaterThan or 
                                  ExpressionType.GreaterThanOrEqual or 
                                  ExpressionType.LessThan or 
                                  ExpressionType.LessThanOrEqual;
    }

    private static bool HaveSameMember(BinaryExpression? left, BinaryExpression? right)
    {
        if (left == null || right == null) return false;

        var leftMember = ExtractMember(left);
        var rightMember = ExtractMember(right);

        return leftMember != null && rightMember != null &&
               leftMember.Member.Name == rightMember.Member.Name;
    }

    private static MemberExpression? ExtractMember(BinaryExpression binary)
    {
        return binary.Left as MemberExpression ?? binary.Right as MemberExpression;
    }

    private RangeInfo? ExtractRangeInfo(BinaryExpression node)
    {
        if (node.Left is not BinaryExpression leftComp || node.Right is not BinaryExpression rightComp)
            return null;

        var leftMember = ExtractMember(leftComp);
        var rightMember = ExtractMember(rightComp);

        if (leftMember == null || rightMember == null || leftMember.Member.Name != rightMember.Member.Name)
            return null;

        var memberName = leftMember.Member.Name;

        // Determine min and max values based on comparison types
        var leftValue = ExtractConstantValue(leftComp);
        var rightValue = ExtractConstantValue(rightComp);

        if (leftValue == null || rightValue == null)
            return null;

        // Determine which is min and which is max based on comparison operators
        var (minValue, maxValue, isMinInclusive, isMaxInclusive) = DetermineRange(
            leftComp, rightComp, leftValue, rightValue, memberName);

        return new RangeInfo(memberName, minValue, maxValue, isMinInclusive, isMaxInclusive);
    }

    private static (object min, object max, bool isMinInclusive, bool isMaxInclusive) DetermineRange(
        BinaryExpression leftComp, BinaryExpression rightComp, 
        object leftValue, object rightValue, string memberName)
    {
        // Determine the order based on comparison operators
        bool leftIsLower = IsLowerBoundComparison(leftComp, memberName);
        bool rightIsLower = IsLowerBoundComparison(rightComp, memberName);

        if (leftIsLower && !rightIsLower)
        {
            // Left is lower bound, right is upper bound
            return (leftValue, rightValue, 
                    IsInclusiveComparison(leftComp), 
                    IsInclusiveComparison(rightComp));
        }
        else if (!leftIsLower && rightIsLower)
        {
            // Right is lower bound, left is upper bound
            return (rightValue, leftValue, 
                    IsInclusiveComparison(rightComp), 
                    IsInclusiveComparison(leftComp));
        }
        else
        {
            // Both are same type, compare values to determine order
            var comparer = Comparer<object>.Default;
            if (comparer.Compare(leftValue, rightValue) <= 0)
            {
                return (leftValue, rightValue, 
                        IsInclusiveComparison(leftComp), 
                        IsInclusiveComparison(rightComp));
            }
            else
            {
                return (rightValue, leftValue, 
                        IsInclusiveComparison(rightComp), 
                        IsInclusiveComparison(leftComp));
            }
        }
    }

    private static bool IsLowerBoundComparison(BinaryExpression binary, string memberName)
    {
        // Check if this is a lower bound comparison (x >= value or x > value)
        if (binary.Left is MemberExpression leftMember && leftMember.Member.Name == memberName)
        {
            return binary.NodeType is ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual;
        }
        else if (binary.Right is MemberExpression rightMember && rightMember.Member.Name == memberName)
        {
            return binary.NodeType is ExpressionType.LessThan or ExpressionType.LessThanOrEqual;
        }
        
        return false;
    }

    private static bool IsInclusiveComparison(BinaryExpression binary)
    {
        return binary.NodeType is ExpressionType.GreaterThanOrEqual or ExpressionType.LessThanOrEqual;
    }

    private static object? ExtractConstantValue(BinaryExpression binary)
    {
        return binary.Left switch
        {
            ConstantExpression leftConstant => leftConstant.Value,
            _ => binary.Right switch
            {
                ConstantExpression rightConstant => rightConstant.Value,
                _ => null
            }
        };
    }

    private void CreateBetweenOperation(RangeInfo rangeInfo)
    {
        _context.AddParameter($"{rangeInfo.MemberName}_min", rangeInfo.MinValue);
        _context.AddParameter($"{rangeInfo.MemberName}_max", rangeInfo.MaxValue);

        _context.AddWhereAction(w =>
        {
            try
            {
                // Try to use WhereBetween if available in Kentico
                if (TryUseBetween(w, rangeInfo))
                    return;

                // Fallback to separate comparisons
                UseSeparateComparisons(w, rangeInfo);
            }
            catch (Exception)
            {
                // Fallback to separate comparisons
                UseSeparateComparisons(w, rangeInfo);
            }
        });
    }

    private static bool TryUseBetween(dynamic w, RangeInfo rangeInfo)
    {
        try
        {
            // Check if both bounds are inclusive
            if (rangeInfo.IsMinInclusive && rangeInfo.IsMaxInclusive)
            {
                // Try WhereBetween method
                w.WhereBetween(rangeInfo.MemberName, rangeInfo.MinValue, rangeInfo.MaxValue);
                return true;
            }
        }
        catch (Exception)
        {
            // WhereBetween might not be available
        }

        return false;
    }

    private static void UseSeparateComparisons(dynamic w, RangeInfo rangeInfo)
    {
        // Create separate comparison operations
        if (rangeInfo.IsMinInclusive)
            w.WhereGreaterOrEquals(rangeInfo.MemberName, rangeInfo.MinValue);
        else
            w.WhereGreater(rangeInfo.MemberName, rangeInfo.MinValue);

        w.And();

        if (rangeInfo.IsMaxInclusive)
            w.WhereLessOrEquals(rangeInfo.MemberName, rangeInfo.MaxValue);
        else
            w.WhereLess(rangeInfo.MemberName, rangeInfo.MaxValue);
    }

    private sealed record RangeInfo(
        string MemberName, 
        object MinValue, 
        object MaxValue, 
        bool IsMinInclusive, 
        bool IsMaxInclusive);
}
