using System.Linq.Expressions;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Builders
{
    public class ReusableSchemaQueryBuilder<T> : IExpressionQueryBuilder<T> where T : class, new()
    {
        private readonly string? _contentType;
        private readonly HashSet<string>? _columnNames;
        private readonly int? _linkedItemsDepth;
        private readonly bool? _includeTotalCount;
        private readonly (int?, int?) _offset;
        private readonly string? _language;
        private readonly bool? _useFallBack;
        private readonly HashSet<string>? _schemaNames;
        private readonly bool? _withContentFields;

        public ReusableSchemaQueryBuilder(string? contentType, HashSet<string>? columnNames, int? linkedItemsDepth, bool? includeTotalCount, (int?, int?) offset, string? language, bool? useFallBack, HashSet<string>? schemaNames, bool? withContentFields)
        {
            _contentType = contentType;
            _columnNames = columnNames;
            _linkedItemsDepth = linkedItemsDepth;
            _includeTotalCount = includeTotalCount;
            _offset = offset;
            _language = language;
            _useFallBack = useFallBack;
            _schemaNames = schemaNames;
            _withContentFields = withContentFields;
        }

        public string Type => "ReusableSchema";

        public ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null)
        {
            var queryBuilder = new ContentItemQueryBuilder().ForContentTypes(subQuery =>
            {
                if (_linkedItemsDepth.HasValue)
                {
                    subQuery.WithLinkedItems(_linkedItemsDepth.Value);
                }

                if (_schemaNames?.Count > 0)
                {
                    subQuery.OfReusableSchema(_schemaNames.ToArray());
                }
                else if (!string.IsNullOrWhiteSpace(_contentType))
                {
                    subQuery.OfContentType(_contentType);
                }

                if (_withContentFields.HasValue && _withContentFields.Value)
                {
                    subQuery.WithContentTypeFields();
                }
            }).Parameters(paramConfig =>
            {
                if (_columnNames?.Count > 0)
                {
                    paramConfig.Columns(_columnNames.ToArray());
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
