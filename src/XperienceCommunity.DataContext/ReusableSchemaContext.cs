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
    public sealed class ReusableSchemaContext<T> : IReusableSchemaContext<T>
    {
        private readonly IProgressiveCache _cache;
        private readonly XperienceDataContextConfig _config;
        private readonly ReusableSchemaExecutor<T> _contentQueryExecutor;
        private readonly string? _contentType;
        private readonly IWebsiteChannelContext _websiteChannelContext;
        private HashSet<string>? _columnNames;
        private bool? _includeTotalCount;
        private string? _language;
        private int? _linkedItemsDepth;
        private (int?, int?) _offset;
        private IQueryable<T>? _query;
        private HashSet<string>? _schemaNames;
        private bool? _useFallBack;
        private bool? _withContentFields;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItemContext{T}"/> class.
        /// </summary>
        /// <param name="websiteChannelContext">The website channel context.</param>
        /// <param name="cache">The cache service.</param>
        /// <param name="contentQueryExecutor"></param>
        /// <param name="config"></param>
        public ReusableSchemaContext(IWebsiteChannelContext websiteChannelContext,
            IProgressiveCache cache, ReusableSchemaExecutor<T> contentQueryExecutor, XperienceDataContextConfig config)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(websiteChannelContext);
            ArgumentNullException.ThrowIfNull(contentQueryExecutor);
            _websiteChannelContext =
                websiteChannelContext;
            _cache = cache;
            _contentQueryExecutor = contentQueryExecutor;
            _config = config;
            _contentType = typeof(T)?.GetContentTypeName() ?? null;
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
            var queryBuilder = BuildQuery(predicate, 1);

            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(
                () => _contentQueryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            return (result ?? []).FirstOrDefault();
        }

        public IDataContext<T> IncludeTotalCount(bool includeTotalCount)
        {
            _includeTotalCount = includeTotalCount;
            return this;
        }

        public IDataContext<T> InLanguage(string language, bool useFallBack = true)
        {
            _language = language;
            _useFallBack = useFallBack;
            return this;
        }

        public IDataContext<T> Offset(int start, int count)
        {
            if (start >= 0 && count >= 0)
            {
                _offset = (start, count);
            }

            return this;
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

            var results = await GetOrCacheAsync(
                () => _contentQueryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            return results ?? [];
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

        public IDataContext<T> WithColumns(params string[] columnNames)
        {
            _columnNames ??= [.. columnNames];

            return this;
        }

        public IDataContext<T> WithContentTypeFields()
        {
            _withContentFields = true;

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

        public IDataContext<T> WithReusableSchemas(params string[] schemaNames)
        {
            _schemaNames ??= [.. schemaNames];

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

                if (item is IWebPageFieldsSource webPage)
                {
                    keys.Add($"webpageitem|byid|{webPage.SystemFields.WebPageItemID}");
                }
            }

            return [.. keys];
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

            if (data is IWebPageFieldsSource webPage)
            {
                return [$"webpageitem|byid|{webPage.SystemFields.WebPageItemID}"];
            }

            return [];
        }

        /// <summary>
        /// Builds a content item query based on the specified expression.
        /// </summary>
        /// <param name="expression">The expression to build the query.</param>
        /// <param name="topN">Optional parameter to limit the number of items.</param>
        /// <returns>The constructed content item query builder.</returns>
        [return: NotNull]
        private ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null)
        {
            var queryBuilder = new ContentItemQueryBuilder().ForContentTypes(subQuery =>
            {
                if (_linkedItemsDepth.HasValue)
                {
                    subQuery.WithLinkedItems(_linkedItemsDepth.Value);
                }

                if (_schemaNames?.Count > 0)
                {
                    subQuery.OfReusableSchema([.. _schemaNames]);
                }
                else if (!string.IsNullOrWhiteSpace(_contentType))
                {
                    subQuery.OfContentType(_contentType);
                }

                if (_withContentFields.HasValue)
                {
                    if (_withContentFields.Value)
                    {
                        subQuery.WithContentTypeFields();
                    }
                }
            }).Parameters(paramConfig =>
            {
                if (_columnNames?.Count > 0)
                {
                    paramConfig.Columns([.. _columnNames]);
                }

                if (topN.HasValue)
                {
                    paramConfig.TopN(topN.Value);
                }

                if (_includeTotalCount.HasValue)
                {
                    paramConfig.IncludeTotalCount();
                }

                if (_offset is { Item1: not null, Item2: not null })
                {
                    paramConfig.Offset(_offset.Item1.Value, _offset.Item2.Value);
                }

                var manager = new QueryParameterManager(paramConfig);

                var visitor = new ContentItemQueryExpressionVisitor(manager);

                visitor.Visit(expression);

                // Apply conditions before returning the query parameters
                manager.ApplyConditions();
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
        private string GetCacheKey(ContentItemQueryBuilder queryBuilder)
        {
            return
                $"data|{string.Join(',', _schemaNames)}|{_websiteChannelContext.WebsiteChannelID}|{_language}|{queryBuilder.GetHashCode()}";
        }

        /// <summary>
        /// Retrieves data from cache or executes the provided function if cache is bypassed or data is not found.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="executeFunc">The function to execute if cache is bypassed or data is not found.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the cached or executed data.</returns>
        private async Task<T?> GetOrCacheAsync<T>(Func<Task<T>> executeFunc, string cacheKey)
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
