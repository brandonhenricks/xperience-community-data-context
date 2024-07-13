using CMS.ContentEngine;

namespace XperienceCommunity.DataContext
{
    internal class QueryParameterManager
    {
        private readonly ContentTypeQueryParameters _queryParameters;
        private readonly List<Action<WhereParameters>> _whereActions;

        public QueryParameterManager(ContentTypeQueryParameters queryParameters)
        {
            _queryParameters = queryParameters;
            _whereActions = new List<Action<WhereParameters>>();
        }

        public void AddComparisonCondition(string key, string comparisonOperator, object? value)
        {
            if (value == null)
            {
                return;
            }

            switch (comparisonOperator)
            {
                case ">":
                    _whereActions.Add(where => where.WhereGreater(key, value));
                    break;

                case ">=":
                    _whereActions.Add(where => where.WhereGreaterOrEquals(key, value));
                    break;

                case "<":
                    _whereActions.Add(where => where.WhereLess(key, value));
                    break;

                case "<=":
                    _whereActions.Add(where => where.WhereLessOrEquals(key, value));
                    break;

                default:
                    throw new NotSupportedException($"Comparison operator '{comparisonOperator}' is not supported.");
            }
        }

        public void AddEqualsCondition(string key, object? value)
        {
            if (value == null)
            {
                return;
            }

            _whereActions.Add(where => where.WhereEquals(key, value));
        }

        public void AddLogicalCondition(string logicalOperator)
        {
            switch (logicalOperator.ToUpper())
            {
                case "AND":
                    _whereActions.Add(where => where.And());
                    break;

                case "OR":
                    _whereActions.Add(where => where.Or());
                    break;

                default:
                    throw new NotSupportedException($"Logical operator '{logicalOperator}' is not supported.");
            }
        }

        public void AddMethodCall(string methodName, object?[] parameters)
        {
            if (parameters is null || parameters.Length == 0)
            {
                return;
            }

            switch (methodName)
            {
                case "Contains":
                    var columnName = parameters?[0]?.ToString();

                    var values = parameters?.Skip(1).ToArray();

                    if (string.IsNullOrWhiteSpace(columnName))
                    {
                        return;
                    }

                    if (values is null)
                    {
                        return;
                    }

                    AddWhereInCondition(columnName, values);
                    break;
                // Handle other method calls as necessary
                default:
                    throw new NotSupportedException($"Method '{methodName}' is not supported.");
            }
        }

        public void AddNotEqualsCondition(string key, object? value)
        {
            if (value == null)
            {
                return;
            }

            _whereActions.Add(where => where.WhereNotEquals(key, value));
        }

        public void ApplyConditions()
        {
            _queryParameters.Where(whereParameters =>
            {
                foreach (var action in _whereActions)
                {
                    action(whereParameters);
                }
            });
        }

        private void AddWhereInCondition(string key, object?[] collection)
        {
            if (collection == null || collection.Length == 0)
            {
                return;
            }

            var elementType = collection[0]?.GetType();

            if (elementType == typeof(int))
            {
                _whereActions.Add(where => where.WhereIn(key, collection.Cast<int>().ToList()));
            }
            else if (elementType == typeof(string))
            {
                _whereActions.Add(where => where.WhereIn(key, collection.Cast<string>().ToList()));
            }
            else if (elementType == typeof(Guid))
            {
                _whereActions.Add(where => where.WhereIn(key, collection.Cast<Guid>().ToList()));
            }
            else
            {
                throw new NotSupportedException($"Collection of type '{elementType}' is not supported.");
            }
        }
    }
}
