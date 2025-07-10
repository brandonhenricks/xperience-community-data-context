using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    /// <summary>
    /// Abstract base class for content query executors that includes processor support.
    /// </summary>
    /// <typeparam name="T">The type of content item.</typeparam>
    /// <typeparam name="TProcessor">The type of processor.</typeparam>
    public abstract class ProcessorSupportedQueryExecutor<T, TProcessor> : BaseContentQueryExecutor<T>
        where T : class, new()
        where TProcessor : IProcessor<T>
    {
        private readonly ILogger _logger;
        private readonly ImmutableList<TProcessor>? _processors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessorSupportedQueryExecutor{T, TProcessor}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="queryExecutor">The query executor.</param>
        /// <param name="processors">The processors.</param>
        protected ProcessorSupportedQueryExecutor(ILogger logger, IContentQueryExecutor queryExecutor,
            IEnumerable<TProcessor>? processors) : base(queryExecutor)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
            _processors = processors?.ToImmutableList();
        }

        /// <inheritdoc />
        [return: NotNull]
        public override async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var results = await ExecuteQueryInternalAsync(queryBuilder, queryOptions, cancellationToken);

                if (_processors == null)
                {
                    return results ?? [];
                }

                foreach (var result in results)
                {
                    foreach (var processor in _processors.OrderBy(x => x.Order))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await processor.ProcessAsync(result, cancellationToken);
                    }
                }

                return results ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return [];
            }
        }

        /// <summary>
        /// Executes the query internally and returns the results.
        /// </summary>
        /// <param name="queryBuilder">The query builder.</param>
        /// <param name="queryOptions">The query options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query results.</returns>
        protected abstract Task<IEnumerable<T>> ExecuteQueryInternalAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken);
    }
}
