using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using CMS.Websites;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    public sealed class PageContentQueryExecutor<T> where T : class, IWebPageFieldsSource, new()
    {
        private readonly ILogger<PageContentQueryExecutor<T>> _logger;
        private readonly ImmutableList<IPageContentProcessor<T>>? _processors;
        private readonly IContentQueryExecutor _queryExecutor;

        public PageContentQueryExecutor(ILogger<PageContentQueryExecutor<T>> logger,
            IContentQueryExecutor queryExecutor, IEnumerable<IPageContentProcessor<T>>? processors)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(queryExecutor);
            _logger = logger;
            _queryExecutor = queryExecutor;
            _processors = processors?.ToImmutableList();
        }

        [return: NotNull]
        public async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            try
            {
                var results = await _queryExecutor.GetMappedWebPageResult<T>(queryBuilder, queryOptions,
                    cancellationToken: cancellationToken);

                if (_processors == null)
                {
                    return results;
                }

                foreach (var result in results)
                {
                    foreach (var processor in _processors.OrderBy(x => x.Order))
                    {
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
