using System.Linq.Expressions;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors;

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
        var isCorrectType = (_isAnd && binaryExpression.NodeType == ExpressionType.AndAlso) ||
                           (!_isAnd && binaryExpression.NodeType == ExpressionType.OrElse);

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

        // Push logical grouping for proper SQL generation
        _context.PushLogicalGrouping(logicalOperator);

        try
        {
            // Process left operand
            ProcessOperand(node.Left, isFirstOperand: true);

            // Add the logical operator
            _context.AddWhereAction(w =>
            {
                if (_isAnd)
                    w.And();
                else
                    w.Or();
            });

            // Process right operand
            ProcessOperand(node.Right, isFirstOperand: false);
        }
        catch (Exception ex) when (!(ex is UnsupportedExpressionException || ex is InvalidExpressionFormatException))
        {
            // Pop the logical grouping if an error occurs to maintain context consistency
            _context.PopLogicalGrouping();
            throw new ExpressionProcessingException($"Failed to process logical expression: {ex.Message}", ex);
        }
    }

    private void ProcessOperand(Expression operand, bool isFirstOperand)
    {
        // Handle different types of operands
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
                // Try to use the visit function if provided, otherwise throw
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

        // For boolean constants in logical expressions, we can optimize:
        // - true && X => X
        // - false && X => false
        // - true || X => true
        // - false || X => X

        if (_isAnd)
        {
            if (!boolValue) // false && X => always false
            {
                _context.AddWhereAction(w => w.WhereEquals("1", 0)); // Always false condition
            }
            // true && X => just process X (no additional condition needed for true)
        }
        else // OR operation
        {
            if (boolValue) // true || X => always true
            {
                _context.AddWhereAction(w => w.WhereEquals("1", 1)); // Always true condition
            }
            // false || X => just process X (no additional condition needed for false)
        }
    }

    private void ProcessMemberExpression(MemberExpression memberExpression)
    {
        // For boolean member expressions like x.IsActive
        if (memberExpression.Type == typeof(bool))
        {
            var paramName = memberExpression.Member.Name;
            _context.AddParameter(paramName, true);
            _context.AddWhereAction(w => w.WhereEquals(paramName, true));
        }
        else
        {
            throw new InvalidExpressionFormatException($"Member expression '{memberExpression.Member.Name}' must be of type bool for logical operations.");
        }
    }

    private void ProcessBinaryExpression(BinaryExpression binaryExpression)
    {
        // Delegate to visit function if available, otherwise throw
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
        // Delegate to visit function if available, otherwise throw
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
        // Delegate to visit function if available, otherwise throw
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
            ConstantExpression constantExpression => constantExpression.Type == typeof(bool),
            MemberExpression memberExpression => memberExpression.Type == typeof(bool) ||
                                                 CanBeProcessedAsBinaryExpression(memberExpression),
            BinaryExpression => true, // Binary expressions can be processed recursively
            UnaryExpression => true,  // Unary expressions can be processed
            MethodCallExpression => true, // Method calls can be processed
            _ => false
        };
    }

    private static bool CanBeProcessedAsBinaryExpression(MemberExpression memberExpression)
    {
        // Check if this member expression is likely to be part of a comparison
        // This is a heuristic - in practice, the actual processing will validate this
        return memberExpression.Type.IsPrimitive ||
               memberExpression.Type == typeof(string) ||
               memberExpression.Type == typeof(DateTime) ||
               memberExpression.Type == typeof(Guid) ||
               Nullable.GetUnderlyingType(memberExpression.Type) != null;
    }
}
