using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;

namespace XperienceCommunity.DataContext
{
    /// <summary>
    /// Abstract base class for executing content queries.
    /// </summary>
    /// <typeparam name="T">The type of content item to be returned by the query.</typeparam>
    public abstract class BaseContentQueryExecutor<T>
    {
        /// <summary>
        /// Gets the content query executor.
        /// </summary>
        protected IContentQueryExecutor QueryExecutor { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseContentQueryExecutor{T}"/> class.
        /// </summary>
        /// <param name="queryExecutor">The content query executor.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="queryExecutor"/> is null.</exception>
        protected BaseContentQueryExecutor(IContentQueryExecutor queryExecutor)
        {
            QueryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
        }

        /// <summary>
        /// Executes the content query asynchronously.
        /// </summary>
        /// <param name="queryBuilder">The content item query builder.</param>
        /// <param name="queryOptions">The content query execution options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable of content items.</returns>
        [return: NotNull]
        public abstract Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken);
    }
}
