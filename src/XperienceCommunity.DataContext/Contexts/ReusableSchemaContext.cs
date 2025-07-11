using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using CMS.ContentEngine;
using CMS.DataEngine;
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
/// Provides context for querying reusable schema content of a specified type.
/// </summary>
/// <typeparam name="T">The type of the reusable schema content.</typeparam>
public sealed class ReusableSchemaContext<T> : BaseDataContext<T, ReusableSchemaExecutor<T>>, IReusableSchemaContext<T>
{
    private bool? _withContentFields;
    private bool? _withPageData;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReusableSchemaContext{T}"/> class.
    /// </summary>
    /// <param name="websiteChannelContext">The website channel context.</param>
    /// <param name="cache">The cache service.</param>
    /// <param name="reusableSchemaExecutor">The reusable schema executor.</param>
    /// <param name="config">The configuration.</param>
    public ReusableSchemaContext(IWebsiteChannelContext websiteChannelContext,
        IProgressiveCache cache, ReusableSchemaExecutor<T> reusableSchemaExecutor, XperienceDataContextConfig config)
        : base(websiteChannelContext, cache, reusableSchemaExecutor, config,
              typeof(T).GetReusableFieldSchemaName() ?? throw new InvalidOperationException("Content type name could not be determined."))
    {
    }

    /// <inheritdoc />
    [return: NotNull]
    public IDataContext<T> WithContentTypeFields()
    {
        _withContentFields = true;
        return this;
    }


    [return: NotNull]
    public IDataContext<T> WithWebPageData()
    {
        _withPageData = true;
        return this;
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
        var queryBuilder = new ContentItemQueryBuilder();

        // Handle reusable schema-specific logic
        if (!string.IsNullOrEmpty(_contentType))
        {
            queryBuilder = queryBuilder.ForContentTypes(subQuery =>
            {
                subQuery.OfReusableSchema(_contentType);

                if (_withContentFields == true)
                {
                    subQuery.WithContentTypeFields();
                }

                if (_withPageData == true)
                {
                    subQuery.WithWebPageData();
                }

                if (_linkedItemsDepth.HasValue)
                {
                    subQuery.WithLinkedItems(_linkedItemsDepth.Value);
                }

            });
        }

        queryBuilder.Parameters(p =>
        {

            if (topN.HasValue)
            {
                p.TopN(topN.Value);
            }

            if (_columnNames?.Count > 0)
            {
                p.Columns(_columnNames.ToArray());
            }

            if (_includeTotalCount.HasValue)
            {
                p.IncludeTotalCount();
            }

            if (_offset is { Item1: not null, Item2: not null })
            {
                p.Offset(_offset.Item1.Value, _offset.Item2.Value);
            }

            var context = new ExpressionContext();

            var visitor = new ContentItemQueryExpressionVisitor(context);

            visitor.Visit(expression);

            foreach (var whereAction in context.WhereActions)
            {
                p.Where(whereAction);
            }

            _parameters = [.. context.Parameters.Select(px => new KeyValuePair<string, object?>(px.Key, px.Value))];
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
    protected override string GetCacheKey(ContentItemQueryBuilder queryBuilder) =>
        $"data|{_contentType}|reusable|{_language}|{queryBuilder.GetHashCode()}|{_parameters?.GetHashCode()}";
}
