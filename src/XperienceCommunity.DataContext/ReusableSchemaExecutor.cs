using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using Microsoft.Extensions.Logging;

namespace XperienceCommunity.DataContext
{
    internal class ReusableSchemaExecutor<T>
    {
        private readonly ILogger<ReusableSchemaExecutor<T>> _logger;
        private readonly IContentQueryExecutor _queryExecutor;

        public ReusableSchemaExecutor(ILogger<ReusableSchemaExecutor<T>> logger, IContentQueryExecutor queryExecutor)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(queryExecutor);
            _logger = logger;
            _queryExecutor = queryExecutor;
        }

        [return: NotNull]
        public async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            try
            {
                var results = await _queryExecutor.GetMappedResult<T>(queryBuilder, queryOptions,
                    cancellationToken: cancellationToken);

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
