using System.Linq.Expressions;

namespace XperienceCommunity.DataContext.Interfaces
{
    internal interface IExpressionProcessor<in T> where T : Expression
    {
        void Process(T node);
    }
}
