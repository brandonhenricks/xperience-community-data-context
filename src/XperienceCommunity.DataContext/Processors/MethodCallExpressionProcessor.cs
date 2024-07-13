using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal class MethodCallExpressionProcessor : IExpressionProcessor<MethodCallExpression>
    {
        private readonly QueryParameterManager _parameterManager;

        public MethodCallExpressionProcessor(QueryParameterManager parameterManager)
        {
            _parameterManager = parameterManager;
        }

        public void Process(MethodCallExpression node)
        {
            var methodName = node.Method.Name;
            var arguments = node.Arguments.Select(arg => Expression.Lambda(arg).Compile().DynamicInvoke()).ToArray();

            _parameterManager.AddMethodCall(methodName, arguments);
        }
    }
}
