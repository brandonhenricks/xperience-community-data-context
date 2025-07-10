using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Core;
using XperienceCommunity.DataContext.Executors;
using XperienceCommunity.DataContext.Expressions.Visitors;
using XperienceCommunity.DataContext.Extensions;

namespace XperienceCommunity.DataContext.Contexts;

/// <summary>
/// Provides context for querying page content of a specified type.
/// </summary>
/// <typeparam name="T">The type of the page content.</typeparam>
public sealed class PageContentContext<T> : BaseDataContext<T, PageContentQueryExecutor<T>>, IPageContentContext<T> 
    where T : class, IWebPageFieldsSource, new()
{
    private string? _channelName;
    private PathMatch? _pathMatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageContentContext{T}"/> class.
    /// </summary>
    /// <param name="cache">The cache service.</param>
    /// <param name="pageContentQueryExecutor">The page content query executor.</param>
    /// <param name="websiteChannelContext">The website channel context.</param>
    /// <param name="config">The configuration.</param>
    public PageContentContext(IProgressiveCache cache, PageContentQueryExecutor<T> pageContentQueryExecutor,
        IWebsiteChannelContext websiteChannelContext, XperienceDataContextConfig config)
        : base(websiteChannelContext, cache, pageContentQueryExecutor, config,
              typeof(T).GetContentTypeName() ?? throw new InvalidOperationException("Content type name could not be determined."))
    {
    }

    /// <inheritdoc />
    [return: NotNull]
    public IPageContentContext<T> InChannel(string channelName)
    {
        _channelName = channelName;
        return this;
    }

    /// <inheritdoc />
    [return: NotNull]
    public IPageContentContext<T> OnPath(PathMatch pathMatch)
    {
        _pathMatch = pathMatch;
        return this;
    }

    /// <summary>
    /// Gets the channel name to use for the query.
    /// </summary>
    /// <returns>The channel name.</returns>
    [return: NotNull]
    private string GetChannelName() => !string.IsNullOrEmpty(_channelName) ? _channelName 
        : _websiteChannelContext.WebsiteChannelName;

    /// <summary>
    /// Builds a content item query based on the specified expression.
    /// </summary>
    /// <param name="expression">The expression to build the query.</param>
    /// <param name="topN">Optional parameter to limit the number of items.</param>
    /// <returns>The constructed content item query builder.</returns>
    [return: NotNull]
    protected override ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null)
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

            var context = new ExpressionContext();
            var visitor = new ContentItemQueryExpressionVisitor(context);
            visitor.Visit(expression);
            foreach (var whereAction in context.WhereActions)
            {
                subQuery.Where(whereAction);
            }
            _parameters = context.Parameters.Select(p => new KeyValuePair<string, object?>(p.Key, p.Value)).ToList();
        });

        if (!string.IsNullOrEmpty(_language))
        {
            queryBuilder.InLanguage(_language);
        }

        return queryBuilder;
    }

    /// <summary>
    /// Generates a cache key based on the query builder.
    /// </summary>
    /// <param name="queryBuilder">The query builder.</param>
    /// <returns>The generated cache key.</returns>
    [return: NotNull]
    protected override string GetCacheKey(ContentItemQueryBuilder queryBuilder) =>
        $"data|{_contentType}|{GetChannelName()}|{_language}|{queryBuilder.GetHashCode()}|{_parameters?.GetHashCode()}";
}
