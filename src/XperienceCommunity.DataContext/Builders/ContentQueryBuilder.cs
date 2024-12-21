using System.Linq.Expressions;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Builders
{
    public sealed class ContentQueryBuilder<T> : IExpressionQueryBuilder<T> where T : class, new()
    {
        private readonly string _contentType;
        private readonly HashSet<string>? _columnNames;
        private readonly int? _linkedItemsDepth;
        private readonly bool? _includeTotalCount;
        private readonly (int?, int?) _offset;
        private readonly string? _language;
        private readonly bool? _useFallBack;

        public ContentQueryBuilder(string contentType, HashSet<string>? columnNames, int? linkedItemsDepth,
            bool? includeTotalCount, (int?, int?) offset, string? language, bool? useFallBack)
        {
            _contentType = contentType;
            _columnNames = columnNames;
            _linkedItemsDepth = linkedItemsDepth;
            _includeTotalCount = includeTotalCount;
            _offset = offset;
            _language = language;
            _useFallBack = useFallBack;
        }

        public string Type => "Content";

        public ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null)
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

                var manager = new QueryParameterManager(subQuery);
                var visitor = new ContentItemQueryExpressionVisitor(manager);

                visitor.Visit(expression);

                manager.ApplyConditions();
            });

            if (!string.IsNullOrEmpty(_language))
            {
                queryBuilder.InLanguage(_language, useLanguageFallbacks: _useFallBack.HasValue && _useFallBack.Value);
            }

            return queryBuilder;
        }
    }
}
