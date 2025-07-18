using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Core;

namespace XperienceCommunity.DataContext.Executors;

/// <summary>
/// Executor for content item queries.
/// </summary>
/// <typeparam name="T">The type of content item.</typeparam>
public class ContentQueryExecutor<T> : ProcessorSupportedQueryExecutor<T, IContentItemProcessor<T>> 
    where T : class, IContentItemFieldsSource, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentQueryExecutor{T}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="queryExecutor">The query executor.</param>
    /// <param name="processors">The processors.</param>
    public ContentQueryExecutor(ILogger<ContentQueryExecutor<T>> logger, IContentQueryExecutor queryExecutor,
        IEnumerable<IContentItemProcessor<T>>? processors) : base(logger, queryExecutor, processors)
    {
    }

    /// <inheritdoc />
    [return: NotNull]
    protected override async Task<IEnumerable<T>> ExecuteQueryInternalAsync(ContentItemQueryBuilder queryBuilder,
        ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
    {
        return await QueryExecutor.GetMappedResult<T>(queryBuilder, queryOptions, cancellationToken: cancellationToken);
    }
}
