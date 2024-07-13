using System.Linq.Expressions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal class UnaryExpressionProcessor: IExpressionProcessor<UnaryExpression>
    {
        private readonly QueryParameterManager _parameterManager;

        public UnaryExpressionProcessor(QueryParameterManager parameterManager)
        {
            _parameterManager = parameterManager;
        }

        public void Process(UnaryExpression node)
        {
            throw new NotImplementedException();
        }
    }
}
