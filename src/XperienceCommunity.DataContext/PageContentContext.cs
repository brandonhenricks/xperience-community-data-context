using System.Linq.Expressions;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using XperienceCommunity.DataContext.Extensions;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    public sealed class PageContentContext<T> : IPageContentContext<T> where T : class, IWebPageFieldsSource, new()
    {
        private readonly IProgressiveCache _cache;
        private readonly string _contentType;
        private readonly PageContentQueryExecutor<T> _pageContentQueryExecutor;
        private readonly IWebsiteChannelContext _websiteChannelContext;
        private string? _channelName;
        private string? _language;
        private int? _linkedItemsDepth;
        private PathMatch? _pathMatch;
        private IQueryable<T>? _query;

        public PageContentContext(IProgressiveCache cache, PageContentQueryExecutor<T> pageContentQueryExecutor,
            IWebsiteChannelContext websiteChannelContext)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(websiteChannelContext);
            ArgumentNullException.ThrowIfNull(pageContentQueryExecutor);

            _cache = cache;
            _contentType = typeof(T).GetContentTypeName() ??
                           throw new InvalidOperationException("Content type name could not be determined.");
            _pageContentQueryExecutor = pageContentQueryExecutor;
            _websiteChannelContext = websiteChannelContext;
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var queryBuilder = BuildQuery(predicate, topN: 1);

            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(
                () => _pageContentQueryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            return result.FirstOrDefault();
        }

        public IPageContentContext<T> InChannel(string channelName)
        {
            _channelName = channelName;
            return this;
        }

        public IDataContext<T> InLanguage(string language, bool useFallBack = true)
        {
            _language = language;
            return this;
        }

        public IPageContentContext<T> OnPath(PathMatch pathMatch)
        {
            _pathMatch = pathMatch;
            return this;
        }

        public IDataContext<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            InitializeQuery();

            _query = _query?.OrderBy(keySelector);

            return this;
        }

        public IDataContext<T> Take(int count)
        {
            InitializeQuery();

            _query = _query?.Take(count);

            return this;
        }

        public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            ValidateQuery();

            var queryBuilder = BuildQuery(_query?.Expression!);

            var queryOptions = CreateQueryOptions();

            return await GetOrCacheAsync(
                () => _pageContentQueryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));
        }

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

        private static string[] GetCacheDependencies(IEnumerable<T> data)
        {
            var keys = new HashSet<string>();

            foreach (var item in data)
            {
                if (item is IWebPageFieldsSource contentItem)
                {
                    keys.Add($"webpageitem|byid|{contentItem.SystemFields.ContentItemID}");
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

            if (data is IWebPageFieldsSource item)
            {
                return [$"webpageitem|byid|{item.SystemFields.ContentItemID}"];
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
                if (_pathMatch is null)
                {
                    subQuery.ForWebsite(_channelName);
                }
                else
                {
                    subQuery.ForWebsite(_channelName, _pathMatch);
                }

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
                queryBuilder.InLanguage(_language);
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
            _query ??= Enumerable.Empty<T>().AsQueryable();
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
