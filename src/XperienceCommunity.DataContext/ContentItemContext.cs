using System.Linq.Expressions;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Extensions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    /// <summary>
    /// Provides context for querying content items of a specified type.
    /// </summary>
    /// <typeparam name="T">The type of the content item.</typeparam>
    public class ContentItemContext<T> : IContentItemContext<T> where T : class, IContentItemFieldsSource, new()
    {
        private readonly IProgressiveCache _cache;
        private readonly string _contentType;
        private readonly ILogger<ContentItemContext<T>> _logger;
        private readonly IContentQueryExecutor _queryExecutor;
        private readonly IWebsiteChannelContext _websiteChannelContext;
        private int? _linkedItemsDepth;
        private IQueryable<T>? _query;
        private string? _language;
        private bool? _useFallBack;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItemContext{T}"/> class.
        /// </summary>
        /// <param name="queryExecutor">The query executor for executing content queries.</param>
        /// <param name="websiteChannelContext">The website channel context.</param>
        /// <param name="cache">The cache service.</param>
        /// <param name="logger"></param>
        public ContentItemContext(IContentQueryExecutor queryExecutor, IWebsiteChannelContext websiteChannelContext,
            IProgressiveCache cache, ILogger<ContentItemContext<T>> logger)
        {
            _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));

            _websiteChannelContext =
                websiteChannelContext ?? throw new ArgumentNullException(nameof(websiteChannelContext));

            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _contentType = typeof(T).GetContentTypeName() ??
                           throw new InvalidOperationException("Content type name could not be determined.");
        }

        /// <summary>
        /// Retrieves the first content item asynchronously based on the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter content items.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first content item or null.</returns>
        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var queryBuilder = BuildQuery(predicate, topN: 1);

            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(() => ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Orders the content items based on the specified key selector.
        /// </summary>
        /// <param name="keySelector">The key selector to order content items.</param>
        /// <returns>The current context for chaining.</returns>
        public IDataContext<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            InitializeQuery();

            _query = _query?.OrderBy(keySelector);

            return this;
        }

        /// <summary>
        /// Limits the number of content items.
        /// </summary>
        /// <param name="count">The maximum number of content items to return.</param>
        /// <returns>The current context for chaining.</returns>
        public IDataContext<T> Take(int count)
        {
            InitializeQuery();

            _query = _query?.Take(count);

            return this;
        }

        /// <summary>
        /// Retrieves the content items asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the content items.</returns>
        public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            ValidateQuery();

            var queryBuilder = BuildQuery(_query?.Expression!);

            var queryOptions = CreateQueryOptions();

            return await GetOrCacheAsync(() => ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));
        }

        /// <summary>
        /// Filters the content items based on the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter content items.</param>
        /// <returns>The current context for chaining.</returns>
        public IDataContext<T> Where(Expression<Func<T, bool>> predicate)
        {
            InitializeQuery();

            _query = _query?.Where(predicate);

            return this;
        }

        /// <summary>
        /// Includes linked items in the query.
        /// </summary>
        /// <param name="depth">The depth of linked items to include.</param>
        /// <returns>The current context for chaining.</returns>
        public IDataContext<T> WithLinkedItems(int depth)
        {
            _linkedItemsDepth = depth;

            return this;
        }
         
        public IDataContext<T> InLanguage(string language, bool useFallBack = true)
        {
            _language = language;
            _useFallBack = useFallBack;
            return this;
        }

        private static string[] GetCacheDependencies<T>(IEnumerable<T> data)
        {
            var keys = new HashSet<string>();

            foreach (var item in data)
            {
                if (item is IContentItemFieldsSource contentItem)
                {
                    keys.Add($"contentitem|byid|{contentItem.SystemFields.ContentItemID}");
                }
            }

            return keys.ToArray();
        }

        private static string[] GetCacheDependencies<T>(T data)
        {
            if (data is IEnumerable<T> items)
            {
                return GetCacheDependencies(items);
            }

            if (data is IContentItemFieldsSource item)
            {
                return [$"contentitem|byid|{item.SystemFields.ContentItemID}"];
            }

            return [];
        }

        /// <summary>
        /// Builds a content item query based on the specified expression.
        /// </summary>
        /// <param name="expression">The expression to build the query.</param>
        /// <param name="topN">Optional parameter to limit the number of items.</param>
        /// <returns>The constructed content item query builder.</returns>
        private ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null)
        {
            var queryBuilder = new ContentItemQueryBuilder().ForContentType(_contentType, subQuery =>
            {
                if (_linkedItemsDepth.HasValue)
                {
                    subQuery.WithLinkedItems(_linkedItemsDepth.Value);
                }

                if (topN.HasValue)
                {
                    subQuery.TopN(topN.Value);
                }

                var visitor = new ContentItemQueryExpressionVisitor(subQuery);

                visitor.Visit(expression);
            });

            if (!string.IsNullOrEmpty(_language))
            {
                queryBuilder.InLanguage(_language, useLanguageFallbacks: _useFallBack.HasValue && _useFallBack.Value);
            }

            return queryBuilder;
        }

        /// <summary>
        /// Creates query options based on the current context.
        /// </summary>
        /// <returns>The content query execution options.</returns>
        private ContentQueryExecutionOptions CreateQueryOptions()
        {
            var queryOptions = new ContentQueryExecutionOptions { ForPreview = _websiteChannelContext.IsPreview };

            queryOptions.IncludeSecuredItems = queryOptions.IncludeSecuredItems || _websiteChannelContext.IsPreview;

            return queryOptions;
        }

        /// <summary>
        /// Executes the query and returns the result.
        /// </summary>
        /// <param name="queryBuilder">The query builder.</param>
        /// <param name="queryOptions">The query options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The task result containing the query result.</returns>
        private async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            try
            {
                return await _queryExecutor.GetMappedResult<T>(queryBuilder, queryOptions,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return [];
            }
        }

        /// <summary>
        /// Generates a cache key based on the query builder.
        /// </summary>
        /// <param name="queryBuilder">The query builder.</param>
        /// <returns>The generated cache key.</returns>
        private string GetCacheKey(ContentItemQueryBuilder queryBuilder)
        {
            return
                $"data|{_contentType}|{_websiteChannelContext.WebsiteChannelName}|{_language}|{queryBuilder.GetHashCode()}";
        }

        /// <summary>
        /// Retrieves data from cache or executes the provided function if cache is bypassed or data is not found.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="executeFunc">The function to execute if cache is bypassed or data is not found.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the cached or executed data.</returns>
        private async Task<T> GetOrCacheAsync<T>(Func<Task<T>> executeFunc, string cacheKey) where T : class
        {
            if (_websiteChannelContext.IsPreview)
            {
                return await executeFunc();
            }

            var cacheSettings = new CacheSettings(10, true, cacheKey);

            return await _cache.LoadAsync(async cs =>
            {
                var result = await executeFunc();

                cs.CacheDependency = CacheHelper.GetCacheDependency(GetCacheDependencies(result));

                return result;
            }, cacheSettings);
        }

        /// <summary>
        /// Initializes the query if it hasn't been already.
        /// </summary>
        private void InitializeQuery()
        {
            if (_query is null)
            {
                _query = Enumerable.Empty<T>().AsQueryable();
            }
        }

        /// <summary>
        /// Validates the query to ensure it's correctly constructed.
        /// </summary>
        private void ValidateQuery()
        {
            if (_query == null)
            {
                throw new InvalidOperationException("The query is not properly initialized.");
            }
        }
    }
}
