using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Diagnostics;
using System.ComponentModel;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Configurations;

namespace XperienceCommunity.DataContext.Core;

/// <summary>
/// Abstract base class for data contexts that provides common functionality for querying content.
/// </summary>
/// <typeparam name="T">The type of content item.</typeparam>
/// <typeparam name="TExecutor">The type of query executor.</typeparam>
[DebuggerDisplay("ContentType: {_contentType}, Language: {_language}, Parameters: {_parameters.Count}, HasQuery: {_query != null}, CacheTimeout: {_config.CacheTimeOut}min")]
[Description("Base class for content querying with expression support and caching")]
public abstract class BaseDataContext<T, TExecutor> : IDataContext<T>
    where TExecutor : BaseContentQueryExecutor<T>
{
    protected readonly IProgressiveCache _cache;
    protected readonly XperienceDataContextConfig _config;
    protected readonly TExecutor _queryExecutor;
    protected readonly string _contentType;
    protected readonly IWebsiteChannelContext _websiteChannelContext;

    protected readonly ConcurrentDictionary<string, object?> _parameters = new();
    protected bool? _includeTotalCount;
    protected string? _language;
    protected int? _linkedItemsDepth;
    protected (int?, int?) _offset;
    protected IQueryable<T>? _query;
    protected bool? _useFallBack;
    protected HashSet<string>? _columnNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDataContext{T, TExecutor}"/> class.
    /// </summary>
    /// <param name="websiteChannelContext">The website channel context.</param>
    /// <param name="cache">The cache service.</param>
    /// <param name="queryExecutor">The query executor.</param>
    /// <param name="config">The configuration.</param>
    /// <param name="contentType">The content type name.</param>
    protected BaseDataContext(IWebsiteChannelContext websiteChannelContext,
        IProgressiveCache cache, TExecutor queryExecutor, XperienceDataContextConfig config,
        string contentType)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(websiteChannelContext);
        ArgumentNullException.ThrowIfNull(queryExecutor);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(contentType);

        _websiteChannelContext = websiteChannelContext;
        _cache = cache;
        _queryExecutor = queryExecutor;
        _config = config;
        _contentType = contentType;
    }

    /// <inheritdoc />
    public virtual async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var queryBuilder = BuildQuery(predicate, topN: 1);
        var queryOptions = CreateQueryOptions();

        var result = await GetOrCacheAsync(
            () => _queryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
            GetCacheKey(queryBuilder)).ConfigureAwait(false);

        return result is not null ? result.SingleOrDefault() : default;
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var queryBuilder = BuildQuery(predicate, topN: 1);
        var queryOptions = CreateQueryOptions();

        var result = await GetOrCacheAsync(
            () => _queryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
            GetCacheKey(queryBuilder)).ConfigureAwait(false);

        return result is not null ? result.FirstOrDefault() : default;
    }

    /// <inheritdoc />
    public virtual async Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var queryBuilder = BuildQuery(predicate);
        var queryOptions = CreateQueryOptions();

        var result = await GetOrCacheAsync(
            () => _queryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
            GetCacheKey(queryBuilder)).ConfigureAwait(false);

        return result is not null ? result.LastOrDefault() : default;
    }

    /// <inheritdoc />
    public virtual IDataContext<T> IncludeTotalCount(bool includeTotalCount)
    {
        _includeTotalCount = includeTotalCount;
        return this;
    }

    /// <inheritdoc />
    public virtual IDataContext<T> InLanguage(string language, bool useFallBack = true)
    {
        _language = language;
        _useFallBack = useFallBack;
        return this;
    }

    /// <inheritdoc />
    public virtual IDataContext<T> Offset(int start, int count)
    {
        if (start >= 0 && count >= 0)
        {
            _offset = (start, count);
        }
        return this;
    }

    /// <inheritdoc />
    public virtual IDataContext<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        InitializeQuery();
        _query = _query?.OrderBy(keySelector);
        return this;
    }

    /// <inheritdoc />
    public virtual IDataContext<T> Take(int count)
    {
        InitializeQuery();
        _query = _query?.Take(count);
        return this;
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateQuery();

        var queryBuilder = BuildQuery(_query?.Expression!);
        var queryOptions = CreateQueryOptions();

        var result = await GetOrCacheAsync(
            () => _queryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
            GetCacheKey(queryBuilder)).ConfigureAwait(false);

        return result ?? Array.Empty<T>();
    }

    /// <inheritdoc />
    public virtual IDataContext<T> Where(Expression<Func<T, bool>> predicate)
    {
        InitializeQuery();
        _query = _query?.Where(predicate);
        return this;
    }

    /// <inheritdoc />
    public virtual IDataContext<T> WithColumns(params string[] columnNames)
    {
        _columnNames ??= [.. columnNames];
        return this;
    }

    /// <inheritdoc />
    public virtual IDataContext<T> WithLinkedItems(int depth)
    {
        _linkedItemsDepth = depth;
        return this;
    }

    /// <summary>
    /// Builds a content item query based on the specified expression.
    /// </summary>
    /// <param name="expression">The expression to build the query.</param>
    /// <param name="topN">Optional parameter to limit the number of items.</param>
    /// <returns>The constructed content item query builder.</returns>
    [return: NotNull]
    protected abstract ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null);

    /// <summary>
    /// Gets the cache key for the specified query builder.
    /// </summary>
    /// <param name="queryBuilder">The query builder.</param>
    /// <returns>The cache key.</returns>
    [return: NotNull]
    protected abstract string GetCacheKey(ContentItemQueryBuilder queryBuilder);

    /// <summary>
    /// Creates query options based on the current context.
    /// </summary>
    /// <returns>The content query execution options.</returns>
    [return: NotNull]
    protected virtual ContentQueryExecutionOptions CreateQueryOptions()
    {
        var queryOptions = new ContentQueryExecutionOptions { ForPreview = _websiteChannelContext.IsPreview };
        queryOptions.IncludeSecuredItems = queryOptions.IncludeSecuredItems || _websiteChannelContext.IsPreview;
        return queryOptions;
    }

    /// <summary>
    /// Retrieves data from cache or executes the provided function if cache is bypassed or data is not found.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="executeFunc">The function to execute if cache is bypassed or data is not found.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the cached or executed data.</returns>
    protected async Task<TResult?> GetOrCacheAsync<TResult>(Func<Task<TResult>> executeFunc, string cacheKey) where TResult : class
    {
        if (_websiteChannelContext.IsPreview)
        {
            return await executeFunc().ConfigureAwait(false);
        }

        var cacheSettings = new CacheSettings(_config.CacheTimeOut, true, cacheKey);

        return await _cache.LoadAsync(async cs =>
        {
            var result = await executeFunc().ConfigureAwait(false);
            cs.BoolCondition = result != null;

            if (!cs.Cached)
            {
                return result;
            }

            cs.CacheDependency = CacheHelper.GetCacheDependency(GetCacheDependencies(result));
            return result;
        }, cacheSettings).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets cache dependencies for the specified data.
    /// </summary>
    /// <param name="data">The data to get cache dependencies for.</param>
    /// <returns>An array of cache dependency keys.</returns>
    protected virtual string[] GetCacheDependencies<TResult>(TResult data)
    {
        if (data is null)
            return [];

        return data switch
        {
            IEnumerable<T> items => GetCacheDependencies(items),
            IWebPageFieldsSource webPage => [$"contentitem|byid|{webPage.SystemFields.ContentItemID}"],
            IContentItemFieldsSource item => [$"contentitem|byid|{item.SystemFields.ContentItemID}"],
            _ => []
        };
    }

    /// <summary>
    /// Gets cache dependencies for a collection of data.
    /// </summary>
    /// <param name="data">The collection of data to get cache dependencies for.</param>
    /// <returns>An array of cache dependency keys.</returns>
    protected virtual string[] GetCacheDependencies(IEnumerable<T> data)
    {
        var keys = new HashSet<string>();

        foreach (var item in data)
        {
            switch (item)
            {
                case IWebPageFieldsSource webPage:
                    keys.Add($"contentitem|byid|{webPage.SystemFields.ContentItemID}");
                    break;

                case IContentItemFieldsSource contentItem:
                    keys.Add($"contentitem|byid|{contentItem.SystemFields.ContentItemID}");
                    break;
            }
        }

        return [.. keys];
    }

    /// <summary>
    /// Initializes the query if it hasn't been already.
    /// </summary>
    protected virtual void InitializeQuery()
    {
        _query ??= Enumerable.Empty<T>().AsQueryable();
    }

    /// <summary>
    /// Validates the query to ensure it's correctly constructed.
    /// </summary>
    protected virtual void ValidateQuery()
    {
        if (_query == null)
        {
            throw new InvalidOperationException("The query is not properly initialized.");
        }
    }
}
