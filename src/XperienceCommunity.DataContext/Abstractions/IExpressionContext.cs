using CMS.ContentEngine;

namespace XperienceCommunity.DataContext.Abstractions;

/// <summary>
/// Represents a context for building and evaluating expressions, including parameters, member access chains,
/// logical groupings, and where clause actions.
/// </summary>
public interface IExpressionContext
{
    /// <summary>
    /// Gets the collection of parameters used in the expression context.
    /// </summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets the chain of member accesses representing the current navigation path in the expression.
    /// </summary>
    IReadOnlyCollection<string> MemberAccessChain { get; }

    /// <summary>
    /// Gets the collection of logical groupings (such as parentheses) used in the expression.
    /// </summary>
    IReadOnlyCollection<string> LogicalGroupings { get; }

    /// <summary>
    /// Gets the list of actions to be applied to <see cref="WhereParameters"/> for building where clauses.
    /// </summary>
    IReadOnlyList<Action<WhereParameters>> WhereActions { get; }

    /// <summary>
    /// Adds a parameter to the context.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    void AddParameter(string name, object? value);

    /// <summary>
    /// Pushes a member name onto the member access chain.
    /// </summary>
    /// <param name="memberName">The member name to push.</param>
    void PushMember(string memberName);

    /// <summary>
    /// Pops the most recently pushed member name from the member access chain.
    /// </summary>
    /// <returns>The member name that was removed.</returns>
    string PopMember();

    /// <summary>
    /// Pushes a logical grouping (such as a parenthesis) onto the logical groupings stack.
    /// </summary>
    /// <param name="grouping">The logical grouping to push.</param>
    void PushLogicalGrouping(string grouping);

    /// <summary>
    /// Pops the most recently pushed logical grouping from the logical groupings stack.
    /// </summary>
    /// <returns>The logical grouping that was removed.</returns>
    string PopLogicalGrouping();

    /// <summary>
    /// Adds an action to be applied to <see cref="WhereParameters"/> for building where clauses.
    /// </summary>
    /// <param name="action">The action to add.</param>
    void AddWhereAction(Action<WhereParameters> action);

    /// <summary>
    /// Clears all parameters, member access chains, logical groupings, and where actions from the context.
    /// </summary>
    void Clear();
}
