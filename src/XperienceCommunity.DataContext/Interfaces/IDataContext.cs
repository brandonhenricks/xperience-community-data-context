using System.Linq.Expressions;

namespace XperienceCommunity.DataContext.Interfaces
{
    /// <summary>
    /// Defines the base contract for a data context that provides common query functionalities.
    /// </summary>
    /// <typeparam name="T">The type of the content item.</typeparam>
    public interface IDataContext<T> where T : class
    {
        /// <summary>
        /// Retrieves the first or default item that matches the specified predicate asynchronously.
        /// </summary>
        /// <param name="predicate">The predicate used to filter the items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first or default item that matches the predicate.</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Filters the items by language.
        /// </summary>
        /// <param name="language">The language code to filter items.</param>
        /// <param name="useFallBack"></param>
        /// <returns>The current context for chaining.</returns>
        IDataContext<T> InLanguage(string language, bool useFallBack = true);

        /// <summary>
        /// Orders the items based on the specified key selector.
        /// </summary>
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
        /// Includes linked items in the query.
        /// </summary>
        /// <param name="depth">The depth of linked items to include.</param>
        /// <returns>The current context for chaining.</returns>
        IDataContext<T> WithLinkedItems(int depth);
    }
}
