using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Core;
using XperienceCommunity.DataContext.Executors;
using XperienceCommunity.DataContext.Expressions.Visitors;
using XperienceCommunity.DataContext.Extensions;

namespace XperienceCommunity.DataContext.Contexts;

/// <summary>
/// Provides context for querying content items of a specified type.
/// </summary>
/// <typeparam name="T">The type of the content item.</typeparam>
public sealed class ContentItemContext<T> : BaseDataContext<T, ContentQueryExecutor<T>>, IContentItemContext<T> 
    where T : class, IContentItemFieldsSource, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemContext{T}"/> class.
    /// </summary>
    /// <param name="websiteChannelContext">The website channel context.</param>
    /// <param name="cache">The cache service.</param>
    /// <param name="contentQueryExecutor">The content query executor.</param>
    /// <param name="config">The configuration.</param>
    public ContentItemContext(IWebsiteChannelContext websiteChannelContext,
        IProgressiveCache cache, ContentQueryExecutor<T> contentQueryExecutor, XperienceDataContextConfig config)
        : base(websiteChannelContext, cache, contentQueryExecutor, config, 
              typeof(T).GetContentTypeName() ?? throw new InvalidOperationException("Content type name could not be determined."))
    {
    }

    /// <summary>
    /// Builds a content item query based on the specified expression.
    /// </summary>
    /// <param name="expression">The expression to build the query.</param>
    /// <param name="topN">Optional parameter to limit the number of items.</param>
    /// <returns>The constructed content item query builder.</returns>
    [return: NotNull]
    protected override ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null)
    {
        var queryBuilder = new ContentItemQueryBuilder().ForContentType(_contentType, subQuery =>
        {
            if (_linkedItemsDepth.HasValue)
            {
                subQuery.WithLinkedItems(_linkedItemsDepth.Value);
            }

            if (_columnNames?.Count > 0)
            {
                subQuery.Columns(_columnNames.ToArray());
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

            var context = new ExpressionContext();
            var visitor = new ContentItemQueryExpressionVisitor(context);
            visitor.Visit(expression);

            // Wire up: apply query fragments as WHERE conditions and parameters
            foreach (var whereAction in context.WhereActions)
            {
                subQuery.Where(whereAction);
            }
            
            // Update thread-safe parameter collection
            _parameters.Clear();
            foreach (var param in context.Parameters)
            {
                _parameters.TryAdd(param.Key, param.Value);
            }
        });

        if (!string.IsNullOrEmpty(_language))
        {
            queryBuilder.InLanguage(_language, useLanguageFallbacks: _useFallBack.HasValue && _useFallBack.Value);
        }

        return queryBuilder;
    }

    /// <summary>
    /// Generates a cache key based on the query builder.
    /// </summary>
    /// <param name="queryBuilder">The query builder.</param>
    /// <returns>The generated cache key.</returns>
    [return: NotNull]
    protected override string GetCacheKey(ContentItemQueryBuilder queryBuilder)
    {
        return CacheKeyGenerator.GenerateCacheKey(
            _contentType,
            _websiteChannelContext.WebsiteChannelID.ToString(),
            _language,
            queryBuilder,
            _parameters);
    }
}
