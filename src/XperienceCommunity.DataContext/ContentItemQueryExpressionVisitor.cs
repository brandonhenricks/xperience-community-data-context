using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;
using XperienceCommunity.DataContext.Processors;

namespace XperienceCommunity.DataContext;

internal sealed class ContentItemQueryExpressionVisitor : ExpressionVisitor
{
    private readonly ExpressionContext _context;

    private readonly Dictionary<ExpressionType, IExpressionProcessor<BinaryExpression>> _binaryExpressionProcessors;
    private readonly Dictionary<Type, IExpressionProcessor> _expressionProcessors;
    private string? _currentMemberName;
    private object? _currentValue;

    public ContentItemQueryExpressionVisitor(ExpressionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _binaryExpressionProcessors = new Dictionary<ExpressionType, IExpressionProcessor<BinaryExpression>>
        {
            { ExpressionType.Equal, new EqualityExpressionProcessor(_context) },
            { ExpressionType.NotEqual, new EqualityExpressionProcessor(_context, isEqual: false) },
            { ExpressionType.GreaterThan, new ComparisonExpressionProcessor(_context, isGreaterThan: true) },
            { ExpressionType.GreaterThanOrEqual, new ComparisonExpressionProcessor(_context, isGreaterThan: true, isEqual: true) },
            { ExpressionType.LessThan, new ComparisonExpressionProcessor(_context, isGreaterThan: false) },
            { ExpressionType.LessThanOrEqual, new ComparisonExpressionProcessor(_context, isGreaterThan: false, isEqual: true) },
            { ExpressionType.AndAlso, new LogicalExpressionProcessor(_context, isAnd: true) },
            { ExpressionType.OrElse, new LogicalExpressionProcessor(_context, isAnd: false) }
        };

        _expressionProcessors = new Dictionary<Type, IExpressionProcessor>
        {
            { typeof(MethodCallExpression), new MethodCallExpressionProcessor(_context) },
            { typeof(UnaryExpression), new UnaryExpressionProcessor(_context) }
        };
    }

    public IExpressionProcessor GetProcessor(Type expressionType)
    {
        if (_expressionProcessors.TryGetValue(expressionType, out var processor))
        {
            return processor;
        }
        throw new NotSupportedException($"The expression type '{expressionType}' is not supported.");
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (_binaryExpressionProcessors.TryGetValue(node.NodeType, out var processor))
        {
            processor.Process(node);
            return node;
        }

        throw new NotSupportedException($"The binary expression type '{node.NodeType}' is not supported.");
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (_expressionProcessors.TryGetValue(typeof(MethodCallExpression), out var processor))
        {
            ((IExpressionProcessor<MethodCallExpression>)processor).Process(node);
            return node;
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (_expressionProcessors.TryGetValue(typeof(UnaryExpression), out var processor))
        {
            ((IExpressionProcessor<UnaryExpression>)processor).Process(node);
            return node;
        }

        return base.VisitUnary(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        _currentMemberName = node.Member.Name;
        return base.VisitMember(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        _currentValue = node.Value;
        return base.VisitConstant(node);
    }
}
