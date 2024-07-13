using CMS.ContentEngine;

namespace XperienceCommunity.DataContext
{
    internal class QueryParameterManager
    {
        private readonly ContentTypeQueryParameters _queryParameters;

        public QueryParameterManager(ContentTypeQueryParameters queryParameters)
        {
            _queryParameters = queryParameters;
        }
        public void AddEqualsCondition(string key, object? value)
        {
            if (value is null)
            {
                return;
            }

            _queryParameters.Where(where => where.WhereEquals(key, value));
        }

        public void AddNotEqualsCondition(string key, object? value)
        {
            if (value is null)
            {
                return;
            }

            _queryParameters.Where(where => where.WhereNotEquals(key, value));
        }

        public void AddLogicalCondition(string logicalOperator)
        {
            // Assuming that Kentico's API automatically handles AND/OR in chained conditions
            // Therefore, this might be a placeholder or be handled differently
        }

        public void AddComparisonCondition(string key, string comparisonOperator, object? value)
        {
            if (value is null)
            {
                return;
            }
            switch (comparisonOperator)
            {
                case ">":
                    _queryParameters.Where(where => where.WhereGreater(key, value));
                    break;
                case ">=":
                    _queryParameters.Where(where => where.WhereGreaterOrEquals(key, value));
                    break;
                case "<":
                    _queryParameters.Where(where => where.WhereLess(key, value));
                    break;
                case "<=":
                    _queryParameters.Where(where => where.WhereLessOrEquals(key, value));
                    break;
                default:
                    throw new NotSupportedException($"Comparison operator '{comparisonOperator}' is not supported.");
            }
        }
    }
}
