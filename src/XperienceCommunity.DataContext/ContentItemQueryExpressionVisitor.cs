using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Interfaces;
using XperienceCommunity.DataContext.Processors;

namespace XperienceCommunity.DataContext
{
    internal sealed class ContentItemQueryExpressionVisitor : ExpressionVisitor
    {
        private readonly QueryParameterManager _parameterManager;
        private readonly Dictionary<Type, IExpressionProcessor> _expressionProcessors;
        private string? _currentMemberName;
        private object? _currentValue;

        public ContentItemQueryExpressionVisitor(QueryParameterManager parameterManager)
        {
            _parameterManager = parameterManager ?? throw new ArgumentNullException(nameof(parameterManager));

            _expressionProcessors = new Dictionary<Type, IExpressionProcessor>
            {
                { typeof(BinaryExpression), new BinaryExpressionProcessor(_parameterManager) },
                { typeof(MethodCallExpression), new MethodCallExpressionProcessor(_parameterManager) },
                { typeof(UnaryExpression), new UnaryExpressionProcessor(_parameterManager) }
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
            if (_expressionProcessors.TryGetValue(node.GetType(), out var processor))
            {
                ((IExpressionProcessor<BinaryExpression>)processor).Process(node);
                return node;
            }

            throw new NotSupportedException($"The binary expression type '{node.NodeType}' is not supported.");
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_expressionProcessors.TryGetValue(node.GetType(), out var processor))
            {
                ((IExpressionProcessor<MethodCallExpression>)processor).Process(node);
                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (_expressionProcessors.TryGetValue(node.GetType(), out var processor))
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
}
