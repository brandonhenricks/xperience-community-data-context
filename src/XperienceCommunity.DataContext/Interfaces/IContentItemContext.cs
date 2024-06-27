using System.Linq.Expressions;
using CMS.ContentEngine;

namespace XperienceCommunity.DataContext.Interfaces
{
    /// <summary>
    /// Defines the contract for a context that provides access to content items of a specified type.
    /// </summary>
    /// <typeparam name="T">The type of the content item.</typeparam>
    /// <summary>
    /// Defines the context for querying content items of a specified type.
    /// </summary>
    public interface IContentItemContext<T> where T : class, IContentItemFieldsSource, new()
    {
        /// <summary>
        /// Filters the content items based on the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter content items.</param>
        /// <returns>The current context for chaining.</returns>
        IContentItemContext<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Orders the content items based on the specified key selector.
        /// </summary>
        /// <param name="keySelector">The key selector to order content items.</param>
        /// <returns>The current context for chaining.</returns>
        IContentItemContext<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Limits the number of content items.
        /// </summary>
        /// <param name="count">The maximum number of content items to return.</param>
        /// <returns>The current context for chaining.</returns>
        IContentItemContext<T> Take(int count);

        /// <summary>
        /// Includes linked items in the query.
        /// </summary>
        /// <param name="depth">The depth of linked items to include.</param>
        /// <returns>The current context for chaining.</returns>
        IContentItemContext<T> WithLinkedItems(int depth);


        /// <summary>
        /// Filters the content items by language.
        /// </summary>
        /// <param name="language">The language code to filter content items.</param>
        /// <returns>The current context for chaining.</returns>
        IContentItemContext<T> InLanguage(string language);

        /// <summary>
        /// Retrieves the content items asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the content items.</returns>
        Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the first content item asynchronously based on the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter content items.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first content item or null.</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
