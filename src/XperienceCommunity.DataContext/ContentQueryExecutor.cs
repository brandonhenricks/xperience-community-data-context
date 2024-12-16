using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    public sealed class ContentQueryExecutor<T> : BaseContentQueryExecutor<T> where T : class, IContentItemFieldsSource, new()
    {
        private readonly ILogger<ContentQueryExecutor<T>> _logger;
        private readonly ImmutableList<IContentItemProcessor<T>>? _processors;

        public ContentQueryExecutor(ILogger<ContentQueryExecutor<T>> logger, IContentQueryExecutor queryExecutor,
            IEnumerable<IContentItemProcessor<T>>? processors) : base(queryExecutor)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
            _processors = processors?.ToImmutableList();
        }

        [return: NotNull]
        public override async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var results = await QueryExecutor.GetMappedResult<T>(queryBuilder, queryOptions,
                    cancellationToken: cancellationToken);

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
    }
}
