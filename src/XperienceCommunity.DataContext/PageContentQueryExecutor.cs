using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using CMS.Websites;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    public sealed class PageContentQueryExecutor<T> : BaseContentQueryExecutor<T> where T : class, IWebPageFieldsSource, new()
    {
        private readonly ILogger<PageContentQueryExecutor<T>> _logger;
        private readonly ImmutableList<IPageContentProcessor<T>>? _processors;

        public PageContentQueryExecutor(ILogger<PageContentQueryExecutor<T>> logger,
            IContentQueryExecutor queryExecutor, IEnumerable<IPageContentProcessor<T>>? processors) : base(queryExecutor)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(queryExecutor);
            _logger = logger;
            _processors = processors?.ToImmutableList();
        }

        [return: NotNull]
        public override async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var results = await QueryExecutor.GetMappedWebPageResult<T>(queryBuilder, queryOptions,
                    cancellationToken: cancellationToken);

                if (_processors == null)
                {
                    return results;
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
    }
}
