using CMS.ContentEngine;
using CMS.Websites;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    public sealed class PageContentQueryExecutor<T> where T : class, IWebPageFieldsSource, new()
    {
        private readonly ILogger<PageContentQueryExecutor<T>> _logger;
        private readonly IEnumerable<IPageContentProcessor<T>> _processors;
        private readonly IContentQueryExecutor _queryExecutor;

        public PageContentQueryExecutor(ILogger<PageContentQueryExecutor<T>> logger,
            IContentQueryExecutor queryExecutor, IEnumerable<IPageContentProcessor<T>>? processors)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(queryExecutor);
            _logger = logger;
            _queryExecutor = queryExecutor;
            _processors = processors ?? [];
        }

        public async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            try
            {
                var results = await _queryExecutor.GetMappedWebPageResult<T>(queryBuilder, queryOptions,
                    cancellationToken: cancellationToken);

                if (!_processors.Any())
                {
                    return results;
                }

                foreach (var result in results)
                {
                    foreach (var processor in _processors)
                    {
                        await processor.ProcessAsync(result);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return [];
            }
        }
    }
}
