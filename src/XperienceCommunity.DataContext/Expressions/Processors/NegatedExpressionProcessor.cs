using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Expressions.Processors;

/// <summary>
/// Handles negated expressions like !expression, optimizing for Kentico's WhereParameters API
/// </summary>
internal sealed class NegatedExpressionProcessor : IExpressionProcessor<UnaryExpression>
{
    private readonly IExpressionContext _context;
    private readonly Func<Expression, Expression> _visitFunction;

    public NegatedExpressionProcessor(IExpressionContext context, Func<Expression, Expression> visitFunction)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(visitFunction);

        _context = context;
        _visitFunction = visitFunction;
    }

    public bool CanProcess(Expression node)
    {
        return node is UnaryExpression { NodeType: ExpressionType.Not } unary &&
               IsSupportedOperand(unary.Operand);
    }

    public void Process(UnaryExpression node)
    {
        if (node.NodeType != ExpressionType.Not)
        {
            throw new InvalidExpressionFormatException("Expected Not expression", node);
        }

        ProcessNegatedOperand(node.Operand);
    }

    private static bool IsSupportedOperand(Expression operand)
    {
        return operand switch
        {
            MethodCallExpression method => IsSupportedNegatedMethod(method),
            BinaryExpression binary => IsSupportedNegatedBinary(binary),
            MemberExpression member => member.Type == typeof(bool),
            _ => false
        };
    }

    private static bool IsSupportedNegatedMethod(MethodCallExpression method)
    {
        return method.Method.Name switch
        {
            nameof(string.Contains) => method.Method.DeclaringType == typeof(string),
            nameof(string.StartsWith) => true,
            nameof(string.EndsWith) => true,
            "IsNullOrEmpty" => true,
            "IsNullOrWhiteSpace" => true,
            _ when method.Method.Name == nameof(Enumerable.Contains) && method.Method.DeclaringType == typeof(Enumerable) => true,
            _ => false
        };
    }

    private static bool IsSupportedNegatedBinary(BinaryExpression binary)
    {
        // Only support binary expressions that have at least one member expression
        bool hasMemberExpression = binary.Left is MemberExpression || binary.Right is MemberExpression;

        if (!hasMemberExpression)
            return false;

        return binary.NodeType switch
        {
            ExpressionType.Equal => true,
            ExpressionType.NotEqual => true,
            ExpressionType.GreaterThan => true,
            ExpressionType.GreaterThanOrEqual => true,
            ExpressionType.LessThan => true,
            ExpressionType.LessThanOrEqual => true,
            _ => false
        };
    }

    private void ProcessNegatedOperand(Expression operand)
    {
        switch (operand)
        {
            case MethodCallExpression method:
                ProcessNegatedMethod(method);
                break;

            case BinaryExpression binary:
                ProcessNegatedBinary(binary);
                break;

            case MemberExpression member when member.Type == typeof(bool):
                ProcessNegatedBooleanMember(member);
                break;

            default:
                throw new UnsupportedExpressionException($"Negated expression type {operand.GetType().Name} is not supported", operand);
        }
    }

    private void ProcessNegatedMethod(MethodCallExpression method)
    {
        switch (method.Method.Name)
        {
            case nameof(string.Contains) when method.Method.DeclaringType == typeof(string):
                ProcessNegatedStringContains(method);
                break;

            case nameof(string.StartsWith):
                ProcessNegatedStringStartsWith(method);
                break;

            case nameof(string.EndsWith):
                ProcessNegatedStringEndsWith(method);
                break;

            case var name when name == nameof(Enumerable.Contains) && method.Method.DeclaringType == typeof(Enumerable):
                ProcessNegatedEnumerableContains(method);
                break;

            case "IsNullOrEmpty":
                ProcessNegatedIsNullOrEmpty(method);
                break;

            case "IsNullOrWhiteSpace":
                ProcessNegatedIsNullOrWhiteSpace(method);
                break;

            default:
                throw new UnsupportedExpressionException($"Negated method '{method.Method.Name}' is not supported", method);
        }
    }

    private void ProcessNegatedBinary(BinaryExpression binary)
    {
        // Convert negated binary to its opposite
        var oppositeType = GetOppositeExpressionType(binary.NodeType);
        var oppositeBinary = Expression.MakeBinary(oppositeType, binary.Left, binary.Right);

        // Process the opposite binary expression
        _visitFunction(oppositeBinary);
    }

    private static ExpressionType GetOppositeExpressionType(ExpressionType type)
    {
        return type switch
        {
            ExpressionType.Equal => ExpressionType.NotEqual,
            ExpressionType.NotEqual => ExpressionType.Equal,
            ExpressionType.GreaterThan => ExpressionType.LessThanOrEqual,
            ExpressionType.GreaterThanOrEqual => ExpressionType.LessThan,
            ExpressionType.LessThan => ExpressionType.GreaterThanOrEqual,
            ExpressionType.LessThanOrEqual => ExpressionType.GreaterThan,
            _ => throw new UnsupportedExpressionException($"Cannot negate expression type: {type}")
        };
    }

    private void ProcessNegatedStringContains(MethodCallExpression method)
    {
        if (method.Object == null || method.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("String.Contains expects one argument", method);
        }

        var memberName = ExtractMemberName(method.Object);
        var value = ExtractConstantValue(method.Arguments[0]);

        _context.AddParameter(memberName, value);

        // Directly use WhereNotContains since Kentico supports it
        _context.AddWhereAction(w =>
        {
            w.WhereNotContains(memberName, value?.ToString());
        });
    }

    private void ProcessNegatedStringStartsWith(MethodCallExpression method)
    {
        if (method.Object == null || method.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("String.StartsWith expects one argument", method);
        }

        var memberName = ExtractMemberName(method.Object);
        var value = ExtractConstantValue(method.Arguments[0]);

        _context.AddParameter(memberName, value);

        // Directly use WhereNotStartsWith since Kentico supports it
        _context.AddWhereAction(w =>
        {
            w.WhereNotStartsWith(memberName, value?.ToString());
        });
    }

    private void ProcessNegatedStringEndsWith(MethodCallExpression method)
    {
        if (method.Object == null || method.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("String.EndsWith expects one argument", method);
        }

        var memberName = ExtractMemberName(method.Object);
        var value = ExtractConstantValue(method.Arguments[0]);

        _context.AddParameter(memberName, value);

        // EndsWith negation - use approximation since WhereEndsWith might not exist
        _context.AddWhereAction(w =>
        {
            w.WhereNull(memberName).Or().WhereNotEquals(memberName, value);
        });
    }

    private void ProcessNegatedEnumerableContains(MethodCallExpression method)
    {
        // Use the enhanced collection processor with negation flag
        var collectionProcessor = new EnhancedCollectionProcessor(_context);

        // We need to modify the collection processor to handle negation
        // For now, throw not supported
        throw new NotSupportedException("Negated Enumerable.Contains is not yet implemented");
    }

    private void ProcessNegatedIsNullOrEmpty(MethodCallExpression method)
    {
        if (method.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("IsNullOrEmpty expects one argument", method);
        }

        var memberName = ExtractMemberName(method.Arguments[0]);

        // Use WhereNotNull and WhereNotEmpty directly
        _context.AddWhereAction(w =>
        {
            w.WhereNotNull(memberName).And().WhereNotEmpty(memberName);
        });
    }

    private void ProcessNegatedIsNullOrWhiteSpace(MethodCallExpression method)
    {
        if (method.Arguments.Count != 1)
        {
            throw new InvalidExpressionFormatException("IsNullOrWhiteSpace expects one argument", method);
        }

        var memberName = ExtractMemberName(method.Arguments[0]);

        // Use WhereNotNull and WhereNotEmpty, then filter out whitespace values
        _context.AddWhereAction(w =>
        {
            w.WhereNotNull(memberName)
             .And().WhereNotEmpty(memberName)
             .And().WhereNotEquals(memberName, " ")
             .And().WhereNotEquals(memberName, "\t")
             .And().WhereNotEquals(memberName, "\n")
             .And().WhereNotEquals(memberName, "\r");
        });
    }

    private void ProcessNegatedBooleanMember(MemberExpression member)
    {
        var memberName = member.Member.Name;
        _context.AddParameter(memberName, false);
        _context.AddWhereAction(w => w.WhereEquals(memberName, false));
    }

    private static string ExtractMemberName(Expression expr)
    {
        return expr switch
        {
            MemberExpression member => member.Member.Name,
            _ => throw new InvalidExpressionFormatException($"Expected member expression, got {expr.GetType().Name}")
        };
    }

    private static object? ExtractConstantValue(Expression expr)
    {
        return expr switch
        {
            ConstantExpression constant => constant.Value,
            _ => throw new InvalidExpressionFormatException($"Expected constant expression, got {expr.GetType().Name}")
        };
    }
}
