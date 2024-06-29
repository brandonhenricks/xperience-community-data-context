using System.Linq.Expressions;
using CMS.Websites;

namespace XperienceCommunity.DataContext.Interfaces
{
    /// <summary>
    /// Represents a context for querying page content of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the page content.</typeparam>
    public interface IPageContentContext<T> where T : class, IWebPageFieldsSource, new()
    {
        /// <summary>
        /// Filters the page content based on the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate used to filter the page content.</param>
        /// <returns>A new instance of <see cref="IPageContentContext{T}"/> with the applied filter.</returns>
        IPageContentContext<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Orders the page content by the specified key selector.
        /// </summary>
        /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
        /// <param name="keySelector">The key selector used for ordering the page content.</param>
        /// <returns>A new instance of <see cref="IPageContentContext{T}"/> with the applied ordering.</returns>
        IPageContentContext<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

        /// <summary>
        /// Includes linked items in the page content up to the specified depth.
        /// </summary>
        /// <param name="depth">The depth of linked items to include.</param>
        /// <returns>A new instance of <see cref="IPageContentContext{T}"/> with the included linked items.</returns>
        IPageContentContext<T> WithLinkedItems(int depth);

        /// <summary>
        /// Filters the page content based on the specified path match.
        /// </summary>
        /// <param name="pathMatch">The path match used to filter the page content.</param>
        /// <returns>A new instance of <see cref="IPageContentContext{T}"/> with the applied path match filter.</returns>
        IPageContentContext<T> OnPath(PathMatch pathMatch);

        /// <summary>
        /// Filters the page content based on the specified channel name.
        /// </summary>
        /// <param name="channelName">The channel name used to filter the page content.</param>
        /// <returns>A new instance of <see cref="IPageContentContext{T}"/> with the applied channel filter.</returns>
        IPageContentContext<T> InChannel(string channelName);

        /// <summary>
        /// Filters the page content based on the specified language.
        /// </summary>
        /// <param name="language">The language used to filter the page content.</param>
        /// <returns>A new instance of <see cref="IPageContentContext{T}"/> with the applied language filter.</returns>
        IPageContentContext<T> InLanguage(string language);

        /// <summary>
        /// Retrieves the page content as a list asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the page content as a list.</returns>
        Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the first or default page content that matches the specified predicate asynchronously.
        /// </summary>
        /// <param name="predicate">The predicate used to filter the page content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first or default page content that matches the predicate.</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
