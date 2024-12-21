using System.Linq.Expressions;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Builders
{
    public class ReusableSchemaQueryBuilder : IExpressionQueryBuilder
    {
        private readonly HashSet<string>? _columnNames;
        private readonly string? _contentType;
        private readonly bool? _includeTotalCount;
        private readonly string? _language;
        private readonly int? _linkedItemsDepth;
        private readonly (int?, int?) _offset;
        private readonly HashSet<string>? _schemaNames;
        private readonly bool? _useFallBack;
        private readonly bool? _withContentFields;

        public ReusableSchemaQueryBuilder(ReusableSchemaConfig config)
        {
            _contentType = config.ContentType;
            _columnNames = config.ColumnNames;
            _linkedItemsDepth = config.LinkedItemsDepth;
            _includeTotalCount = config.IncludeTotalCount;
            _offset = config.Offset;
            _language = config.Language;
            _useFallBack = config.UseFallBack;
            _schemaNames = config.SchemaNames;
            _withContentFields = config.WithContentFields;
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
