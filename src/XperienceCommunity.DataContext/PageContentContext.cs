using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using XperienceCommunity.DataContext.Configurations;
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
        private readonly XperienceDataContextConfig _config;
        private string? _channelName;
        private bool? _includeTotalCount;
        private string? _language;
        private int? _linkedItemsDepth;
        private (int?, int?) _offset;
        private PathMatch? _pathMatch;
        private HashSet<string>? _columnNames;
        private IQueryable<T>? _query;

        public PageContentContext(IProgressiveCache cache, PageContentQueryExecutor<T> pageContentQueryExecutor,
            IWebsiteChannelContext websiteChannelContext, XperienceDataContextConfig config)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(websiteChannelContext);
            ArgumentNullException.ThrowIfNull(pageContentQueryExecutor);

            _cache = cache;
            _contentType = typeof(T).GetContentTypeName() ??
                           throw new InvalidOperationException("Content type name could not be determined.");
            _pageContentQueryExecutor = pageContentQueryExecutor;
            _websiteChannelContext = websiteChannelContext;
            _config = config;
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var queryBuilder = BuildQuery(predicate, topN: 1);

            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(
                () => _pageContentQueryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            return result?.FirstOrDefault();
        }

        [return: NotNull]
        public IPageContentContext<T> InChannel(string channelName)
        {
            _channelName = channelName;
            return this;
        }

        [return: NotNull]
        public IDataContext<T> IncludeTotalCount(bool includeTotalCount)
        {
            _includeTotalCount = includeTotalCount;
            return this;
        }

        [return: NotNull]
        public IDataContext<T> InLanguage(string language, bool useFallBack = true)
        {
            _language = language;
            return this;
        }

        [return: NotNull]
        public IDataContext<T> Offset(int start, int count)
        {
            if (start >= 0 && count >= 0)
            {
                _offset = (start, count);
            }

            return this;
        }

        [return: NotNull]
        public IPageContentContext<T> OnPath(PathMatch pathMatch)
        {
            _pathMatch = pathMatch;
            return this;
        }

        [return: NotNull]
        public IDataContext<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            InitializeQuery();

            _query = _query?.OrderBy(keySelector);

            return this;
        }

        [return: NotNull]
        public IDataContext<T> Take(int count)
        {
            InitializeQuery();

            _query = _query?.Take(count);

            return this;
        }

        [return: NotNull]
        public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            ValidateQuery();

            var queryBuilder = BuildQuery(_query?.Expression!);

            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(
                () => _pageContentQueryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            return result ?? [];
        }

        [return: NotNull]
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
        [return: NotNull]
        public IDataContext<T> WithLinkedItems(int depth)
        {
            _linkedItemsDepth = depth;

            return this;
        }

        [return: NotNull]
        public IDataContext<T> WithColumns(params string[] columnNames)
        {
            _columnNames ??= [.. columnNames];

            return this;
        }

        [return: NotNull]
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

            return [.. keys];
        }

        [return: NotNull]
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

        [return: NotNull]
        private string GetChannelName() => !string.IsNullOrWhiteSpace(_channelName)
            ? _channelName
            : _websiteChannelContext.WebsiteChannelName;

        /// <summary>
        /// Builds a content item query based on the specified expression.
        /// </summary>
        /// <param name="expression">The expression to build the query.</param>
        /// <param name="topN">Optional parameter to limit the number of items.</param>
        /// <returns>The constructed content item query builder.</returns>
        [return: NotNull]
        private ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null)
        {
            var channelName = GetChannelName();

            var queryBuilder = new ContentItemQueryBuilder().ForContentType(_contentType, subQuery =>
            {
                if (_pathMatch is null)
                {

                    subQuery.ForWebsite(channelName);
                }
                else
                {
                    subQuery.ForWebsite(channelName, _pathMatch);
                }
                
                if (_columnNames?.Count > 0)
                {
                    subQuery.Columns(_columnNames.ToArray());
                }

                if (_linkedItemsDepth.HasValue)
                {
                    subQuery.WithLinkedItems(_linkedItemsDepth.Value);
                }

                if (topN.HasValue)
                {
                    subQuery.TopN(topN.Value);
                }

                if (_includeTotalCount.HasValue)
                {
                    subQuery.IncludeTotalCount();
                }

                if (_offset is { Item1: not null, Item2: not null })
                {
                    subQuery.Offset(_offset.Item1.Value, _offset.Item2.Value);
                }

                var manager = new QueryParameterManager(subQuery);

                var visitor = new ContentItemQueryExpressionVisitor(manager);

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
        [return: NotNull]
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
        [return: NotNull]
        private string GetCacheKey(ContentItemQueryBuilder queryBuilder) => $"data|{_contentType}|{GetChannelName()}|{_language}|{queryBuilder.GetHashCode()}";


        /// <summary>
        /// Retrieves data from cache or executes the provided function if cache is bypassed or data is not found.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="executeFunc">The function to execute if cache is bypassed or data is not found.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the cached or executed data.</returns>
        private async Task<T?> GetOrCacheAsync<T>(Func<Task<T>> executeFunc, string cacheKey) where T : class
        {
            if (_websiteChannelContext.IsPreview)
            {
                return await executeFunc();
            }

            var cacheSettings = new CacheSettings(_config.CacheTimeOut, true, cacheKey);

            return await _cache.LoadAsync(async cs =>
            {
                var result = await executeFunc();

                cs.BoolCondition = result != null;

                if (!cs.Cached)
                {
                    return result;
                }

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
