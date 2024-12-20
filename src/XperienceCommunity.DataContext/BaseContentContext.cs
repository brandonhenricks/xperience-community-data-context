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
    public abstract class BaseContentContext<T> : IDataContext<T> where T : class, new()
    {
        protected readonly IProgressiveCache Cache;
        protected readonly XperienceDataContextConfig Config;
        protected readonly string ContentType;
        protected readonly IWebsiteChannelContext WebsiteChannelContext;
        protected IList<KeyValuePair<string, object?>> Parameters;
        protected bool? TotalCount;
        protected string? Language;
        protected int? LinkedItemsDepth;
        protected (int?, int?) CurrentOffset;
        protected IQueryable<T>? Query;
        protected HashSet<string>? ColumnNames;

        protected PathMatch? PathMatch;
        protected string? ChannelName;

        protected BaseContentContext(IWebsiteChannelContext websiteChannelContext, IProgressiveCache cache,
            XperienceDataContextConfig config)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(websiteChannelContext);
            ArgumentNullException.ThrowIfNull(config);

            WebsiteChannelContext = websiteChannelContext;
            Cache = cache;
            Config = config;
            ContentType = typeof(T).GetContentTypeName() ??
                          throw new InvalidOperationException("Content type name could not be determined.");
            Parameters = new List<KeyValuePair<string, object?>>();
        }

        public async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var queryBuilder = BuildQuery(predicate, topN: 1);
            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(
                () => ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            Parameters.Clear();

            return result?.SingleOrDefault();
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var queryBuilder = BuildQuery(predicate, topN: 1);
            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(
                () => ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            Parameters.Clear();

            return result?.FirstOrDefault();
        }

        public async Task<T?> LastOrDefaultAsync(Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var queryBuilder = BuildQuery(predicate);
            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(
                () => ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            Parameters.Clear();

            return result?.LastOrDefault();
        }

        public IDataContext<T> IncludeTotalCount(bool includeTotalCount)
        {
            TotalCount = includeTotalCount;
            return this;
        }

        public IDataContext<T> InLanguage(string language, bool useFallBack = true)
        {
            Language = language;
            return this;
        }

        public IDataContext<T> Offset(int start, int count)
        {
            if (start >= 0 && count >= 0)
            {
                CurrentOffset = (start, count);
            }

            return this;
        }

        public IDataContext<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            InitializeQuery();

            Query = Query?.OrderBy(keySelector);

            return this;
        }

        public IDataContext<T> Take(int count)
        {
            InitializeQuery();

            Query = Query?.Take(count);

            return this;
        }

        public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateQuery();

            var queryBuilder = BuildQuery(Query?.Expression!);
            var queryOptions = CreateQueryOptions();

            var result = await GetOrCacheAsync(
                () => ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
                GetCacheKey(queryBuilder));

            Parameters.Clear();

            return result ?? Array.Empty<T>();
        }

        public IDataContext<T> Where(Expression<Func<T, bool>> predicate)
        {
            InitializeQuery();

            Query = Query?.Where(predicate);

            return this;
        }

        public IDataContext<T> WithColumns(params string[] columnNames)
        {
            ColumnNames ??= new HashSet<string>(columnNames);

            return this;
        }

        public IDataContext<T> WithLinkedItems(int depth)
        {
            LinkedItemsDepth = depth;

            return this;
        }

        protected abstract Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken);

        private ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null)
        {
            var queryBuilder = new ContentItemQueryBuilder().ForContentType(ContentType, subQuery =>
            {
                if (LinkedItemsDepth.HasValue)
                {
                    subQuery.WithLinkedItems(LinkedItemsDepth.Value);
                }

                if (ColumnNames?.Count > 0)
                {
                    subQuery.Columns(ColumnNames.ToArray());
                }

                if (topN.HasValue)
                {
                    subQuery.TopN(topN.Value);
                }

                if (TotalCount.HasValue)
                {
                    subQuery.IncludeTotalCount();
                }

                if (CurrentOffset is { Item1: not null, Item2: not null })
                {
                    subQuery.Offset(CurrentOffset.Item1.Value, CurrentOffset.Item2.Value);
                }

                var manager = new QueryParameterManager(subQuery);
                var visitor = new ContentItemQueryExpressionVisitor(manager);

                visitor.Visit(expression);

                Parameters = manager.GetQueryParameters().ToList();
                manager.ApplyConditions();
            });

            if (!string.IsNullOrEmpty(Language))
            {
                queryBuilder.InLanguage(Language);
            }

            return queryBuilder;
        }

        private ContentQueryExecutionOptions CreateQueryOptions()
        {
            var queryOptions = new ContentQueryExecutionOptions { ForPreview = WebsiteChannelContext.IsPreview };
            queryOptions.IncludeSecuredItems = queryOptions.IncludeSecuredItems || WebsiteChannelContext.IsPreview;

            return queryOptions;
        }

        private string GetCacheKey(ContentItemQueryBuilder queryBuilder) =>
            $"data|{ContentType}|{WebsiteChannelContext.WebsiteChannelID}|{Language}|{queryBuilder.GetHashCode()}|{Parameters.GetHashCode()}";

        private async Task<T?> GetOrCacheAsync<T>(Func<Task<T>> executeFunc, string cacheKey) where T : class
        {
            if (WebsiteChannelContext.IsPreview)
            {
                return await executeFunc();
            }

            var cacheSettings = new CacheSettings(Config.CacheTimeOut, true, cacheKey);

            return await Cache.LoadAsync(async cs =>
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
                return new[] { $"contentitem|byid|{item.SystemFields.ContentItemID}" };
            }

            return Array.Empty<string>();
        }

        private void InitializeQuery()
        {
            Query ??= Enumerable.Empty<T>().AsQueryable();
        }

        private void ValidateQuery()
        {
            if (Query == null)
            {
                throw new InvalidOperationException("The query is not properly initialized.");
            }
        }
    }
}
