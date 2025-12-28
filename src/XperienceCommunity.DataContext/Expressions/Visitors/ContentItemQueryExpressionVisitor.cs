using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Contexts;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Expressions.Visitors;

internal sealed class ContentItemQueryExpressionVisitor : ExpressionVisitor
{
    private readonly IExpressionContext _context;

    private readonly Dictionary<ExpressionType, IExpressionProcessor<BinaryExpression>> _binaryExpressionProcessors;
    private readonly Dictionary<Type, IExpressionProcessor> _expressionProcessors;

    // Enhanced processors for specialized operations
    private readonly EnhancedCollectionProcessor _collectionProcessor;

    private readonly EnhancedStringProcessor _stringProcessor;
    private readonly NegatedExpressionProcessor _negatedProcessor;
    private readonly RangeOptimizationProcessor _rangeProcessor;

    public ContentItemQueryExpressionVisitor(ExpressionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Initialize enhanced processors
        _collectionProcessor = new EnhancedCollectionProcessor(_context);
        _stringProcessor = new EnhancedStringProcessor(_context);
        _negatedProcessor = new NegatedExpressionProcessor(_context, Visit);
        _rangeProcessor = new RangeOptimizationProcessor(_context, Visit);

        _binaryExpressionProcessors = new Dictionary<ExpressionType, IExpressionProcessor<BinaryExpression>>
        {
            { ExpressionType.Equal, new EqualityExpressionProcessor(_context) },
            { ExpressionType.NotEqual, new EqualityExpressionProcessor(_context, isEqual: false) },
            { ExpressionType.GreaterThan, new ComparisonExpressionProcessor(_context, isGreaterThan: true) },
            { ExpressionType.GreaterThanOrEqual, new ComparisonExpressionProcessor(_context, isGreaterThan: true, isEqual: true) },
            { ExpressionType.LessThan, new ComparisonExpressionProcessor(_context, isGreaterThan: false) },
            { ExpressionType.LessThanOrEqual, new ComparisonExpressionProcessor(_context, isGreaterThan: false, isEqual: true) },
            { ExpressionType.AndAlso, new LogicalExpressionProcessor(_context, isAnd: true, Visit) },
            { ExpressionType.OrElse, new LogicalExpressionProcessor(_context, isAnd: false, Visit) },
            { ExpressionType.Coalesce, new NullCoalescingExpressionProcessor(_context) }
        };

        _expressionProcessors = new Dictionary<Type, IExpressionProcessor>
        {
            { typeof(MethodCallExpression), new MethodCallExpressionProcessor(_context) },
            { typeof(UnaryExpression), new UnaryExpressionProcessor(_context) },
            { typeof(ConditionalExpression), new ConditionalExpressionProcessor(_context) }
        };
    }

    public IExpressionProcessor GetProcessor(Type expressionType)
    {
        if (_expressionProcessors.TryGetValue(expressionType, out var processor))
        {
            return processor;
        }

        throw new UnsupportedExpressionException(expressionType);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // First, check if this is a range expression that can be optimized
        if (node.NodeType == ExpressionType.AndAlso && _rangeProcessor.CanProcess(node))
        {
            _rangeProcessor.Process(node);
            return node;
        }

        // Use existing binary expression processors
        if (_binaryExpressionProcessors.TryGetValue(node.NodeType, out var processor))
        {
            processor.Process(node);
            return node;
        }

        throw new UnsupportedExpressionException(node.NodeType, node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Try enhanced processors first for better optimization

        // Enhanced collection processing
        if (_collectionProcessor.CanProcess(node))
        {
            _collectionProcessor.Process(node);
            return node;
        }

        // Enhanced string processing
        if (_stringProcessor.CanProcess(node))
        {
            _stringProcessor.Process(node);
            return node;
        }

        // Fall back to existing method call processor
        if (_expressionProcessors.TryGetValue(typeof(MethodCallExpression), out var processor))
        {
            ((IExpressionProcessor<MethodCallExpression>)processor).Process(node);
            return node;
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        // Handle negated expressions with enhanced processor
        if (node.NodeType == ExpressionType.Not && _negatedProcessor.CanProcess(node))
        {
            _negatedProcessor.Process(node);
            return node;
        }

        // Fall back to existing unary processor
        if (_expressionProcessors.TryGetValue(typeof(UnaryExpression), out var processor))
        {
            ((IExpressionProcessor<UnaryExpression>)processor).Process(node);
            return node;
        }

        return base.VisitUnary(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return base.VisitMember(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        return base.VisitConstant(node);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        if (_expressionProcessors.TryGetValue(typeof(ConditionalExpression), out var processor))
        {
            ((IExpressionProcessor<ConditionalExpression>)processor).Process(node);
            return node;
        }

        return base.VisitConditional(node);
    }
}
