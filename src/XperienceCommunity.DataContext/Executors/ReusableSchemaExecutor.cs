using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Core;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Executors;

public class ReusableSchemaExecutor<T> : BaseContentQueryExecutor<T>
{
    private readonly ILogger<ReusableSchemaExecutor<T>> _logger;

    public ReusableSchemaExecutor(ILogger<ReusableSchemaExecutor<T>> logger, IContentQueryExecutor queryExecutor) : base(queryExecutor)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    [return: NotNull]
    public override async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
        ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var results = await QueryExecutor.GetMappedResult<T>(queryBuilder, queryOptions,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return results ?? Array.Empty<T>();
        }
        catch (OperationCanceledException)
        {
            // Allow cancellation to bubble up
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed for {ContentType}", typeof(T).Name);
            throw new QueryExecutionException($"Failed to execute query for {typeof(T).Name}", typeof(T).Name, ex);
        }
    }
}
