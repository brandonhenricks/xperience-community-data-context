using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors;

internal sealed class LogicalExpressionProcessor : IExpressionProcessor<BinaryExpression>
{
    private readonly IExpressionContext _context;
    private readonly bool _isAnd;

    public LogicalExpressionProcessor(IExpressionContext context, bool isAnd)
    {
        _context = context;
        _isAnd = isAnd;
    }

    public bool CanProcess(Expression node)
    {
        return node is BinaryExpression binaryExpression &&
               (binaryExpression.NodeType == ExpressionType.AndAlso || binaryExpression.NodeType == ExpressionType.OrElse) &&
               (binaryExpression.Left is MemberExpression || binaryExpression.Right is MemberExpression);
    }

    public void Process(BinaryExpression node)
    {
        var logicalOperator = _isAnd ? "AND" : "OR";
        _context.PushLogicalGrouping(logicalOperator);
        _context.AddWhereAction(w =>
        {
            if (_isAnd)
                w.And();
            else
                w.Or();
        });
    }
}
