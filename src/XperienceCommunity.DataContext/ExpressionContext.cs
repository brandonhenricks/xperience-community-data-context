using CMS.ContentEngine;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext;

/// <summary>
/// Encapsulates the state and context during expression tree traversal and translation to Kentico API queries.
/// Tracks parameter names/values, member/property access chains, logical groupings, and intermediate query fragments.
/// Extensible for future LINQ features and Kentico API changes.
/// </summary>
public sealed class ExpressionContext: IExpressionContext
{
    private readonly Dictionary<string, object?> _parameters = new();
    private readonly Stack<string> _memberAccessChain = new();
    private readonly Stack<string> _logicalGroupings = new();
    private readonly List<Action<WhereParameters>> _whereActions = new();

    /// <summary>
    /// Gets the parameter dictionary for query binding.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    /// <summary>
    /// Gets the current member/property access chain.
    /// </summary>
    public IReadOnlyCollection<string> MemberAccessChain => _memberAccessChain.ToArray();

    /// <summary>
    /// Gets the current logical groupings (AND/OR nesting).
    /// </summary>
    public IReadOnlyCollection<string> LogicalGroupings => _logicalGroupings.ToArray();

    /// <summary>
    /// Gets the intermediate query fragments or builder state.
    /// </summary>
    public IReadOnlyList<Action<WhereParameters>> WhereActions => _whereActions;

    /// <summary>
    /// Adds a parameter for query binding.
    /// </summary>
    public void AddParameter(string name, object? value)
    {
        if (!_parameters.ContainsKey(name))
        {
            _parameters.Add(name, value);
        }
    }

    /// <summary>
    /// Pushes a member/property name onto the access chain.
    /// </summary>
    public void PushMember(string memberName)
    {
        _memberAccessChain.Push(memberName);
    }

    /// <summary>
    /// Pops the most recent member/property name from the access chain.
    /// </summary>
    public string PopMember()
    {
        return _memberAccessChain.Pop();
    }

    /// <summary>
    /// Pushes a logical grouping (e.g., AND/OR) onto the stack.
    /// </summary>
    public void PushLogicalGrouping(string grouping)
    {
        _logicalGroupings.Push(grouping);
    }

    /// <summary>
    /// Pops the most recent logical grouping from the stack.
    /// </summary>
    public string PopLogicalGrouping()
    {
        return _logicalGroupings.Pop();
    }

    /// <summary>
    /// Adds an intermediate query fragment or builder state.
    /// </summary>
    public void AddWhereAction(Action<WhereParameters> action)
    {
        _whereActions.Add(action);
    }

    /// <summary>
    /// Clears all context state.
    /// </summary>
    public void Clear()
    {
        _parameters.Clear();
        _memberAccessChain.Clear();
        _logicalGroupings.Clear();
        _whereActions.Clear();
    }
}
