using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Expressions.Processors;

internal sealed class LogicalExpressionProcessor : IExpressionProcessor<BinaryExpression>
{
    private readonly IExpressionContext _context;
    private readonly bool _isAnd;
    private readonly Func<Expression, Expression>? _visitFunction;

    public LogicalExpressionProcessor(IExpressionContext context, bool isAnd, Func<Expression, Expression>? visitFunction = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _isAnd = isAnd;
        _visitFunction = visitFunction;
    }

    public bool CanProcess(Expression node)
    {
        if (node is not BinaryExpression binaryExpression)
            return false;

        // Check if this is the correct logical operation type
        var isCorrectType = _isAnd && binaryExpression.NodeType == ExpressionType.AndAlso ||
                           !_isAnd && binaryExpression.NodeType == ExpressionType.OrElse;

        if (!isCorrectType)
            return false;

        // Validate that at least one operand is processable
        return IsProcessableExpression(binaryExpression.Left) || IsProcessableExpression(binaryExpression.Right);
    }

    public void Process(BinaryExpression node)
    {
        if (!CanProcess(node))
            throw new UnsupportedExpressionException(node.NodeType, node);

        var logicalOperator = _isAnd ? "AND" : "OR";
        _context.PushLogicalGrouping(logicalOperator);

        var shortCircuited = false;
        try
        {
            // Short-circuiting for boolean constants
            if (TryShortCircuit(node.Left, node.Right, isLeft: true))
            {
                shortCircuited = true;
                return;
            }
            if (TryShortCircuit(node.Right, node.Left, isLeft: false))
            {
                shortCircuited = true;
                return;
            }

            ProcessOperand(node.Left, isFirstOperand: true);
            _context.AddWhereAction(w => { if (_isAnd) w.And(); else w.Or(); });
            ProcessOperand(node.Right, isFirstOperand: false);
        }
        catch (Exception ex) when (!(ex is UnsupportedExpressionException || ex is InvalidExpressionFormatException))
        {
            throw new ExpressionProcessingException($"Failed to process logical expression: {ex.Message}", node, ex);
        }
        finally
        {
            // Always pop the logical grouping to prevent memory leaks
            _context.PopLogicalGrouping();
        }
    }

    // Implements true short-circuiting for boolean constants
    private bool TryShortCircuit(Expression first, Expression second, bool isLeft)
    {
        if (first is ConstantExpression constantExpression && constantExpression.Type == typeof(bool))
        {
            var boolValue = (bool)constantExpression.Value!;
            if (_isAnd)
            {
                if (!boolValue)
                {
                    // false && X => always false
                    // Ensure parameters for member expressions are still added
                    if (second is MemberExpression memberExpression)
                    {
                        ProcessMemberExpression(memberExpression);
                    }
                    _context.AddWhereAction(w => w.WhereEquals("1", 0));
                    return true;
                }
                // true && X => just process X
                ProcessOperand(second, isFirstOperand: !isLeft);
                return true;
            }
            else
            {
                if (boolValue)
                {
                    // true || X => always true
                    if (second is MemberExpression memberExpression)
                    {
                        ProcessMemberExpression(memberExpression);
                    }
                    _context.AddWhereAction(w => w.WhereEquals("1", 1));
                    return true;
                }
                // false || X => just process X
                ProcessOperand(second, isFirstOperand: !isLeft);
                return true;
            }
        }
        return false;
    }

    private void ProcessOperand(Expression operand, bool isFirstOperand)
    {
        switch (operand)
        {
            case ConstantExpression constantExpression when constantExpression.Type == typeof(bool):
                ProcessBooleanConstant(constantExpression, isFirstOperand);
                break;

            case MemberExpression memberExpression:
                ProcessMemberExpression(memberExpression);
                break;

            case BinaryExpression binaryExpression:
                ProcessBinaryExpression(binaryExpression);
                break;

            case UnaryExpression unaryExpression:
                ProcessUnaryExpression(unaryExpression);
                break;

            case MethodCallExpression methodCallExpression:
                ProcessMethodCallExpression(methodCallExpression);
                break;

            default:
                if (_visitFunction != null)
                {
                    _visitFunction(operand);
                }
                else
                {
                    throw new UnsupportedExpressionException($"Unsupported expression type: {operand.GetType().Name}", operand);
                }
                break;
        }
    }

    private void ProcessBooleanConstant(ConstantExpression constantExpression, bool isFirstOperand)
    {
        var boolValue = (bool)constantExpression.Value!;

        if (_isAnd)
        {
            if (!boolValue)
            {
                _context.AddWhereAction(w => w.WhereEquals("1", 0));
            }
        }
        else
        {
            if (boolValue)
            {
                _context.AddWhereAction(w => w.WhereEquals("1", 1));
            }
        }
    }

    private void ProcessMemberExpression(MemberExpression memberExpression)
    {
        if (memberExpression.Type == typeof(bool))
        {
            // Use full member access chain for parameter name to avoid collisions
            var memberNames = GetMemberAccessChain(memberExpression);
            var paramName = string.Join("_", memberNames);
            _context.AddParameter(paramName, true);
            _context.AddWhereAction(w => w.WhereEquals(paramName, true));
        }
        else
        {
            throw new InvalidExpressionFormatException($"Member expression '{memberExpression.Member.Name}' must be of type bool for logical operations.", memberExpression);
        }
    }

    private static List<string> GetMemberAccessChain(MemberExpression memberExpression)
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

    private void ProcessBinaryExpression(BinaryExpression binaryExpression)
    {
        if (_visitFunction != null)
        {
            _visitFunction(binaryExpression);
        }
        else
        {
            throw new UnsupportedExpressionException(binaryExpression.NodeType, binaryExpression);
        }
    }

    private void ProcessUnaryExpression(UnaryExpression unaryExpression)
    {
        if (_visitFunction != null)
        {
            _visitFunction(unaryExpression);
        }
        else
        {
            throw new UnsupportedExpressionException(unaryExpression.NodeType, unaryExpression);
        }
    }

    private void ProcessMethodCallExpression(MethodCallExpression methodCallExpression)
    {
        if (_visitFunction != null)
        {
            _visitFunction(methodCallExpression);
        }
        else
        {
            throw new UnsupportedExpressionException(methodCallExpression.Method.Name, methodCallExpression);
        }
    }

    private static bool IsProcessableExpression(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constantExpression => IsSupportedType(constantExpression.Type),
            MemberExpression memberExpression => IsSupportedType(memberExpression.Type),
            BinaryExpression => true,
            UnaryExpression => true,
            MethodCallExpression => true,
            _ => false
        };
    }

    private static bool IsSupportedType(Type type)
    {
        // Add more supported types as needed
        return type == typeof(bool)
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(double)
            || type == typeof(decimal)
            || type == typeof(float)
            || type == typeof(short)
            || type == typeof(byte)
            || type == typeof(uint)
            || type == typeof(ulong)
            || type == typeof(ushort)
            || type == typeof(sbyte);
    }
}
