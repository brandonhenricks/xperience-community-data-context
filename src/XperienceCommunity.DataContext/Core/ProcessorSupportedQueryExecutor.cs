using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Diagnostics;

namespace XperienceCommunity.DataContext.Core;

/// <summary>
/// Abstract base class for content query executors that includes processor support.
/// Performance metrics are tracked in DEBUG builds via <see cref="QueryExecutorPerformanceTracker"/>.
/// </summary>
/// <typeparam name="T">The type of content item.</typeparam>
/// <typeparam name="TProcessor">The type of processor.</typeparam>
[DebuggerDisplay("ContentType: {typeof(T).Name}, Processors: {_processors?.Count ?? 0}")]
[Description("Query executor with processor support")]
public abstract class ProcessorSupportedQueryExecutor<T, TProcessor> : BaseContentQueryExecutor<T>
    where T : class, new()
    where TProcessor : IProcessor<T>
{
    private readonly ILogger _logger;
    private readonly ImmutableList<TProcessor>? _processors;
    private static readonly ActivitySource ActivitySource = new("XperienceCommunity.Data.Context.QueryExecution");

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
        using var activity = ActivitySource.StartActivity("ExecuteQuery");
        activity?.SetTag("contentType", typeof(T).Name);
        activity?.SetTag("processorCount", _processors?.Count ?? 0);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var results = await ExecuteQueryInternalAsync(queryBuilder, queryOptions, cancellationToken).ConfigureAwait(false);

            activity?.SetTag("executionTimeMs", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("resultCount", results?.Count() ?? 0);

            if (_processors == null)
            {
                return results ?? Array.Empty<T>();
            }

            using var processingActivity = ActivitySource.StartActivity("ProcessResults");
            processingActivity?.SetTag("processorCount", _processors.Count);

            var processedCount = 0;
            foreach (var result in results ?? Array.Empty<T>())
            {
                foreach (var processor in _processors.OrderBy(x => x.Order))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await processor.ProcessAsync(result, cancellationToken).ConfigureAwait(false);
                }
                processedCount++;
            }

            processingActivity?.SetTag("itemsProcessed", processedCount);
            return results ?? Array.Empty<T>();
        }
        catch (Exception ex)
        {
            activity?.SetTag("error", true);
            activity?.SetTag("errorMessage", ex.Message);
            _logger.LogError(ex, ex.Message);
            return Array.Empty<T>();
        }
        finally
        {
            stopwatch.Stop();
            // Track performance in DEBUG builds only
            QueryExecutorPerformanceTracker.RecordExecution(
                $"{GetType().Name}<{typeof(T).Name}>",
                stopwatch.ElapsedMilliseconds);
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
