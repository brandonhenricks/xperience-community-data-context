using System.Linq.Expressions;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Diagnostics;

namespace XperienceCommunity.DataContext.Expressions.Processors;

[DebuggerDisplay("Processor: Binary, Context: {_context.Parameters.Count} params, {_context.WhereActions.Count} actions")]
[Description("Processes binary expressions (equality, comparison, arithmetic) for query translation")]
internal sealed class BinaryExpressionProcessor : IExpressionProcessor<BinaryExpression>
{
    private readonly IExpressionContext _context;

    public BinaryExpressionProcessor(IExpressionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerStepThrough]
    private void ValidateExpression(BinaryExpression node, [CallerMemberName] string? callerName = null)
    {
        if (node.Left == null || node.Right == null)
        {
            throw new ExpressionProcessingException(
                "Binary expression must have both left and right operands",
                node,
                $"NodeType: {node.NodeType}, Left: {node.Left?.NodeType}, Right: {node.Right?.NodeType}, Caller: {callerName}");
        }
    }

    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    private void LogProcessingStart(BinaryExpression node, [CallerMemberName] string? operation = null)
    {
        Debug.WriteLine($"[BinaryExpressionProcessor] Starting {operation}: {node.NodeType} | Left: {node.Left?.NodeType} | Right: {node.Right?.NodeType}");
    }

    public void Process(BinaryExpression node)
    {
        ValidateExpression(node);
        LogProcessingStart(node);

        // Log diagnostic information
        DataContextDiagnostics.LogDiagnostic(
            "ExpressionProcessing", 
            $"Processing binary expression: {node.NodeType}",
            LogLevel.Debug);

        switch (node.NodeType)
        {
            case ExpressionType.Equal:
                ProcessEquality(node, isEqual: true);
                break;

            case ExpressionType.NotEqual:
                ProcessEquality(node, isEqual: false);
                break;

            case ExpressionType.GreaterThan:
                ProcessComparison(node, isGreaterThan: true);
                break;

            case ExpressionType.GreaterThanOrEqual:
                ProcessComparison(node, isGreaterThan: true, isEqual: true);
                break;

            case ExpressionType.LessThan:
                ProcessComparison(node, isGreaterThan: false);
                break;

            case ExpressionType.LessThanOrEqual:
                ProcessComparison(node, isGreaterThan: false, isEqual: true);
                break;

            case ExpressionType.AndAlso:
                ProcessLogical(node, isAnd: true);
                break;

            case ExpressionType.OrElse:
                ProcessLogical(node, isAnd: false);
                break;

            case ExpressionType.And:
                ProcessLogical(node, true);
                break;

            case ExpressionType.Or:
                ProcessLogical(node, false);
                break;

            default:
                throw new UnsupportedExpressionException(node.NodeType, node);
        }
    }

    private void ProcessEquality(BinaryExpression node, bool isEqual)
    {
        MemberExpression? member = null;
        ConstantExpression? constant = null;

        // Handle both (Member == Constant) and (Constant == Member)
        if (node.Left is MemberExpression leftMember && node.Right is ConstantExpression rightConstant)
        {
            member = leftMember;
            constant = rightConstant;
        }
        else if (node.Left is ConstantExpression leftConstant && node.Right is MemberExpression rightMember)
        {
            member = rightMember;
            constant = leftConstant;
        }
        else if (node.Left is ConstantExpression leftConst && node.Right is ConstantExpression rightConst)
        {
            var result = isEqual
                ? Equals(leftConst.Value, rightConst.Value)
                : !Equals(leftConst.Value, rightConst.Value);
            // Optionally: store or use 'result' as needed
            // For most data contexts, you might ignore or throw
            throw new InvalidExpressionFormatException("Cannot process constant-to-constant equality in a data context.");
        }
        if (member != null && constant != null)
        {
            var paramName = member.Member.Name;
            _context.AddParameter(paramName, constant.Value);

            // For equality, order doesn't matter. For not-equals, order doesn't matter.
            if (isEqual)
            {
                _context.AddWhereAction(w => w.WhereEquals(paramName, constant.Value));
            }
            else
            {
                _context.AddWhereAction(w => w.WhereNotEquals(paramName, constant.Value));
            }
        }
        else
        {
            throw new InvalidExpressionFormatException("Invalid expression format for equality comparison.");
        }
    }

    private void ProcessComparison(BinaryExpression node, bool isGreaterThan, bool isEqual = false)
    {
        if (node is { Left: MemberExpression member, Right: ConstantExpression constant })
        {
            var paramName = member.Member.Name;
            _context.AddParameter(paramName, constant.Value);
            if (isGreaterThan && isEqual)
            {
                _context.AddWhereAction(w => w.WhereGreaterOrEquals(paramName, constant.Value));
            }
            else if (isGreaterThan)
            {
                _context.AddWhereAction(w => w.WhereGreater(paramName, constant.Value));
            }
            else if (isEqual)
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
            throw new InvalidExpressionFormatException("Invalid expression format for comparison.");
        }
    }

    private void ProcessLogical(BinaryExpression node, bool isAnd)
    {
        var logicalOperator = isAnd ? "AND" : "OR";
        _context.PushLogicalGrouping(logicalOperator);
        _context.AddWhereAction(w =>
        {
            if (isAnd)
                w.And();
            else
                w.Or();
        });
    }

    public bool CanProcess(Expression node)
    {
        return node is BinaryExpression;
    }
}
