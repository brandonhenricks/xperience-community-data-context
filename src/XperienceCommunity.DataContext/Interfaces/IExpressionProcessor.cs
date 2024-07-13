using System.Linq.Expressions;

namespace XperienceCommunity.DataContext.Interfaces
{
    internal interface IExpressionProcessor
    {
    }

    internal interface IExpressionProcessor<in T>: IExpressionProcessor where T : Expression
    {
        void Process(T node);
    }
}
