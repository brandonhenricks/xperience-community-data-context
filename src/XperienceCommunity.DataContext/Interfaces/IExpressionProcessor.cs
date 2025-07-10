using System.Linq.Expressions;

namespace XperienceCommunity.DataContext.Interfaces
{
    /// <summary>
    /// Represents a marker interface for processing expression nodes.
    /// </summary>
    internal interface IExpressionProcessor
    {
        bool CanProcess(Expression node);
    }

    internal interface IExpressionProcessor<in T> : IExpressionProcessor where T : Expression
    {
        /// <summary>
        /// Processes the specified expression node.
        /// </summary>
        /// <param name="node">The expression node to process.</param>
        void Process(T node);
    }
}
