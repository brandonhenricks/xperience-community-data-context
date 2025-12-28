using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Expressions.Processors;

internal sealed class UnaryExpressionProcessor : IExpressionProcessor<UnaryExpression>
{
    private readonly IExpressionContext _context;

    public UnaryExpressionProcessor(IExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    public void Process(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Not:
                ProcessNot(node);
                break;

            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                ProcessConvert(node);
                break;

            case ExpressionType.Quote:
                ProcessQuote(node);
                break;

            case ExpressionType.TypeAs:
                ProcessTypeAs(node);
                break;

            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
                ProcessNegate(node);
                break;

            case ExpressionType.UnaryPlus:
                ProcessUnaryPlus(node);
                break;

            default:
                throw new UnsupportedExpressionException(node.NodeType, node);
        }
    }

    private void ProcessNot(UnaryExpression node)
    {
        switch (node.Operand)
        {
            case BinaryExpression binaryExpression:
                var binaryProcessor = new BinaryExpressionProcessor(_context);
                binaryProcessor.Process(binaryExpression);
                // TODO: Add negation logic to ExpressionContext if needed
                break;

            case UnaryExpression unaryExpression:
                // Double negation: NOT(NOT(x)) => x
                if (unaryExpression.NodeType == ExpressionType.Not)
                {
                    Process(unaryExpression.Operand as UnaryExpression ?? unaryExpression);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported unary operand for NOT operation.");
                }
                break;

            case MemberExpression memberExpression:
                // Negate a boolean member: !x.SomeBool
                // You may want to add logic to ExpressionContext to handle this
                // For now, just visit the member
                Visit(memberExpression);
                // TODO: Add negation logic to ExpressionContext if needed
                break;

            case MethodCallExpression methodCallExpression:
                // Negate a method call: !x.SomeMethod()
                Visit(methodCallExpression);
                // TODO: Add negation logic to ExpressionContext if needed
                break;

            case ConstantExpression constantExpression:
                // Negate a constant: !true => false, !false => true
                if (constantExpression.Type == typeof(bool) && constantExpression.Value is bool b)
                {
                    // You may want to push this value to ExpressionContext
                    // For now, just a placeholder
                    var negated = !b;
                    // TODO: Add logic to ExpressionContext if needed
                }
                else
                {
                    throw new InvalidOperationException("Invalid constant type for NOT operation.");
                }
                break;

            default:
                throw new InvalidOperationException($"Invalid or unsupported expression type '{node.Operand?.GetType().Name}' for NOT operation.");
        }
    }

    private void ProcessConvert(UnaryExpression node)
    {
        // Handle type conversion logic
        // This is a placeholder implementation, and you may need to adjust based on actual usage context
        // In most cases, the Convert operation may not need special handling for building query conditions
        // If the parameter 'node' is not used, consider removing it or implementing logic that uses it
        // For now, suppress the warning if no logic is needed
        _ = node; // Suppress unused parameter warning
    }

    private void ProcessQuote(UnaryExpression node)
    {
        // Handle quote logic
        // This may involve visiting the operand or other processing specific to the 'Quote' expression
        Visit(node.Operand);
        // If the parameter 'node' is not used, consider removing it or implementing logic that uses it
        // For now, suppress the warning if no logic is needed
        _ = node; // Suppress unused parameter warning
    }

    private void Visit(Expression node)
    {
        // TODO: Refactor ContentItemQueryExpressionVisitor to use ExpressionContext if needed
        // var visitor = new ContentItemQueryExpressionVisitor(_context);
        // visitor.Visit(node);
    }

    private void ProcessTypeAs(UnaryExpression node)
    {
        // TypeAs operations (x as T) would require type checking support
        // For now, we'll just process the operand and ignore the type cast
        if (node.Operand is MemberExpression member)
        {
            var memberName = member.Member.Name;
            _context.PushMember(memberName);
        }
        else
        {
            throw new NotSupportedException("TypeAs operator is only supported for member expressions.");
        }
    }

    private void ProcessNegate(UnaryExpression node)
    {
        // Negate operations (-x) would require arithmetic support
        if (node.Operand is MemberExpression member)
        {
            var memberName = member.Member.Name;
            // For negation, we might need to use database-specific functions
            throw new NotSupportedException("Negation operator requires database-specific arithmetic functions which are not currently implemented.");
        }
        else if (node.Operand is ConstantExpression constant)
        {
            // We can negate constants at compile time
            object negatedValue = constant.Type switch
            {
                Type t when t == typeof(int) => -(int)constant.Value!,
                Type t when t == typeof(long) => -(long)constant.Value!,
                Type t when t == typeof(float) => -(float)constant.Value!,
                Type t when t == typeof(double) => -(double)constant.Value!,
                Type t when t == typeof(decimal) => -(decimal)constant.Value!,
                _ => throw new NotSupportedException($"Negation is not supported for type {constant.Type}")
            };

            var paramName = $"negated_{Guid.NewGuid():N}";
            _context.AddParameter(paramName, negatedValue);
            _context.AddWhereAction(w => w.WhereEquals(paramName, negatedValue));
        }
        else
        {
            throw new NotSupportedException("Negation operator is only supported for member expressions and constants.");
        }
    }

    private void ProcessUnaryPlus(UnaryExpression node)
    {
        // Unary plus (+x) is essentially a no-op, just process the operand
        if (node.Operand is MemberExpression member)
        {
            var memberName = member.Member.Name;
            _context.PushMember(memberName);
        }
        else if (node.Operand is ConstantExpression constant)
        {
            var paramName = $"plus_{Guid.NewGuid():N}";
            _context.AddParameter(paramName, constant.Value);
            _context.AddWhereAction(w => w.WhereEquals(paramName, constant.Value));
        }
        else
        {
            throw new NotSupportedException("Unary plus operator is only supported for member expressions and constants.");
        }
    }

    public bool CanProcess(Expression node)
    {
        return node is UnaryExpression unaryExpression &&
               (unaryExpression.NodeType == ExpressionType.Not ||
                unaryExpression.NodeType == ExpressionType.Convert ||
                unaryExpression.NodeType == ExpressionType.Quote);
    }
}
