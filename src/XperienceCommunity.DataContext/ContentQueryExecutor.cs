using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    public sealed class ContentQueryExecutor<T> where T : class, IContentItemFieldsSource, new()
    {
        private readonly ILogger<ContentQueryExecutor<T>> _logger;
        private readonly ImmutableList<IContentItemProcessor<T>>? _processors;
        private readonly IContentQueryExecutor _queryExecutor;

        public ContentQueryExecutor(ILogger<ContentQueryExecutor<T>> logger, IContentQueryExecutor queryExecutor,
            IEnumerable<IContentItemProcessor<T>>? processors)
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
                var results = await _queryExecutor.GetMappedResult<T>(queryBuilder, queryOptions,
                    cancellationToken: cancellationToken);

                if (_processors == null)
                {
                    return results ?? [];
                }

                foreach (var result in results)
                {
                    foreach (var processor in _processors.OrderBy(x=> x.Order))
                    {
                        await processor.ProcessAsync(result);
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
