using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using CMS.Websites;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Core;

namespace XperienceCommunity.DataContext.Executors;

/// <summary>
/// Executor for page content queries.
/// </summary>
/// <typeparam name="T">The type of page content.</typeparam>
public class PageContentQueryExecutor<T> : ProcessorSupportedQueryExecutor<T, IPageContentProcessor<T>>
    where T : class, IWebPageFieldsSource, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageContentQueryExecutor{T}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="queryExecutor">The query executor.</param>
    /// <param name="processors">The processors.</param>
    public PageContentQueryExecutor(ILogger<PageContentQueryExecutor<T>> logger,
        IContentQueryExecutor queryExecutor, IEnumerable<IPageContentProcessor<T>>? processors)
        : base(logger, queryExecutor, processors)
    {
        ArgumentNullException.ThrowIfNull(queryExecutor);
    }

    /// <inheritdoc />
    [return: NotNull]
    protected override async Task<IEnumerable<T>> ExecuteQueryInternalAsync(ContentItemQueryBuilder queryBuilder,
        ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
    {
        return await QueryExecutor.GetMappedWebPageResult<T>(queryBuilder, queryOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
