using System.Linq.Expressions;
using System.ComponentModel;
using System.Diagnostics;

namespace XperienceCommunity.DataContext.Abstractions.Processors;

/// <summary>
/// Represents a marker interface for processing expression nodes.
/// </summary>
[Description("Base interface for expression processors in the data context pipeline")]
internal interface IExpressionProcessor
{
    /// <summary>
    /// Determines whether the processor can handle the specified expression node.
    /// </summary>
    /// <param name="node">The expression node to evaluate.</param>
    /// <returns>True if the processor can handle the node; otherwise, false.</returns>
    [DebuggerStepThrough]
    bool CanProcess(Expression node);
}

/// <summary>
/// Represents a typed interface for processing specific expression node types.
/// </summary>
/// <typeparam name="T">The type of expression node this processor handles.</typeparam>
[Description("Generic interface for typed expression processors")]
internal interface IExpressionProcessor<in T> : IExpressionProcessor where T : Expression
{
    /// <summary>
    /// Processes the specified expression node.
    /// </summary>
    /// <param name="node">The expression node to process.</param>
    void Process(T node);
}
