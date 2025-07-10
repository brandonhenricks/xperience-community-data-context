using System.Linq.Expressions;

namespace XperienceCommunity.DataContext.Interfaces;

/// <summary>
/// Defines the base contract for a data context that provides common query functionalities.
/// </summary>
/// <typeparam name="T">The type of the content item.</typeparam>
public interface IDataContext<T>
{
    /// <summary>
    /// Retrieves a single item that matches the specified predicate asynchronously or returns the default value if no such item is found.
    /// </summary>
    /// <param name="predicate">The predicate used to filter the items.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the single item that matches the predicate or the default value if no such item is found.</returns>
    Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the first or default item that matches the specified predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The predicate used to filter the items.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the first or default item that matches the predicate.</returns>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the last or default item that matches the specified predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The predicate used to filter the items.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the last or default item that matches the predicate.</returns>
    Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes the total count of items in the query result.
    /// </summary>
    /// <param name="includeTotalCount">Indicates whether to include the total count of items in the query result.</param>
    /// <returns>The current context for chaining.</returns>
    IDataContext<T> IncludeTotalCount(bool includeTotalCount);

    /// <summary>
    /// Filters the items by language.
    /// </summary>
    /// <param name="language">The language code to filter items.</param>
    /// <param name="useFallBack">Indicates whether to use fallback language if the specified language is not available.</param>
    /// <returns>The current context for chaining.</returns>
    IDataContext<T> InLanguage(string language, bool useFallBack = true);

    /// <summary>
    /// Offsets the items by the specified start index and count.
    /// </summary>
    /// <param name="start">The start index of the items to offset.</param>
    /// <param name="count">The number of items to offset.</param>
    /// <returns>The current context for chaining.</returns>
    IDataContext<T> Offset(int start, int count);

    /// <summary>
    /// Orders the items based on the specified key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">The key selector to order items.</param>
    /// <returns>The current context for chaining.</returns>
    IDataContext<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Limits the number of items.
    /// </summary>
    /// <param name="count">The maximum number of items to return.</param>
    /// <returns>The current context for chaining.</returns>
    IDataContext<T> Take(int count);

    /// <summary>
    /// Retrieves the content items asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the content items.</returns>
    Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Filters the items based on the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter items.</param>
    /// <returns>The current context for chaining.</returns>
    IDataContext<T> Where(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Retrieves the content items with specified columns asynchronously.
    /// </summary>
    /// <param name="columnNames">The names of the columns to retrieve.</param>
    /// <returns>The current context for chaining.</returns>
    IDataContext<T> WithColumns(params string[] columnNames);

    /// <summary>
    /// Includes linked items in the query.
    /// </summary>
    /// <param name="depth">The depth of linked items to include.</param>
    /// <returns>The current context for chaining.</returns>
    IDataContext<T> WithLinkedItems(int depth);
}
