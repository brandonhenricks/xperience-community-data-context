﻿using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Extensions;

namespace XperienceCommunity.DataContext
{
    internal sealed class QueryParameterManager
    {
        private readonly ContentQueryParameters? _contentQueryParameters;
        private readonly ContentTypeQueryParameters? _queryParameters;
        private readonly List<Action<WhereParameters>> _whereActions;
        private readonly IList<KeyValuePair<string, object?>> _params;

        public QueryParameterManager(ContentTypeQueryParameters queryParameters)
        {
            _queryParameters = queryParameters;
            _whereActions = new List<Action<WhereParameters>>();
            _params = new List<KeyValuePair<string, object?>>();
        }

        public QueryParameterManager(ContentQueryParameters contentQueryParameters)
        {
            _contentQueryParameters = contentQueryParameters;
            _whereActions = new List<Action<WhereParameters>>();
            _params = new List<KeyValuePair<string, object?>>();
        }

        [return: NotNull]
        public IEnumerable<KeyValuePair<string, object>> GetQueryParameters() => _params!;

        private void AddParam(string key, object value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            _params.Add(new KeyValuePair<string, object>(key, value)!);
        }

        internal void AddComparisonCondition(string key, string comparisonOperator, object? value)
        {
            if (value == null)
            {
                return;
            }

            switch (comparisonOperator)
            {
                case ">":
                    _whereActions.Add(where => where.WhereGreater(key, value));
                    AddParam(key, value);
                    break;

                case ">=":
                    _whereActions.Add(where => where.WhereGreaterOrEquals(key, value));
                    AddParam(key, value);
                    break;

                case "<":
                    _whereActions.Add(where => where.WhereLess(key, value));
                    AddParam(key, value);
                    break;

                case "<=":
                    _whereActions.Add(where => where.WhereLessOrEquals(key, value));
                    AddParam(key, value);
                    break;

                default:
                    throw new NotSupportedException($"Comparison operator '{comparisonOperator}' is not supported.");
            }
        }

        internal void AddEqualsCondition(string key, object? value)
        {
            if (value == null)
            {
                return;
            }

            _whereActions.Add(where => where.WhereEquals(key, value));
            AddParam(key, value);
        }

        internal void AddLogicalCondition(string logicalOperator)
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

        internal void AddMethodCall(string methodName, params object[] parameters)
        {
            switch (methodName)
            {
                case "Contains":
                    AddContainsMethod(parameters);
                    break;
                // Add cases for other supported methods here
                default:
                    throw new NotSupportedException($"Method '{methodName}' is not supported.");
            }
        }

        internal void AddContainsMethod(object[] parameters)
        {
            if (parameters.Length != 2)
            {
                throw new InvalidOperationException("Invalid parameters for Contains method.");
            }

            var key = parameters[0].ToString();

            var collection = parameters[1];

            switch (collection)
            {
                case IEnumerable<int> intCollection:
                    _whereActions.Add(where => where.WhereIn(key, intCollection.ToList()));
                    AddParam(key, intCollection);
                    break;
                case IEnumerable<string> stringCollection:
                    _whereActions.Add(where => where.WhereIn(key, stringCollection.ToList()));
                    AddParam(key, stringCollection);
                    break;
                case IEnumerable<Guid> guidCollection:
                    _whereActions.Add(where => where.WhereIn(key, guidCollection.ToList()));
                    AddParam(key, guidCollection);
                    break;
                default:
                    throw new NotSupportedException($"Collection of type '{collection.GetType()}' is not supported.");
            }
        }

        internal void AddNotEqualsCondition(string key, object? value)
        {
            if (value == null)
            {
                return;
            }

            _whereActions.Add(where => where.WhereNotEquals(key, value));

            AddParam(key, value);
        }

        internal void ApplyConditions()
        {
            _queryParameters?.Where(whereParameters =>
            {
                foreach (var action in _whereActions)
                {
                    action(whereParameters);
                }
            });

            _contentQueryParameters?.Where(whereParameters =>
            {
                foreach (var action in _whereActions)
                {
                    action(whereParameters);
                }
            });
            // Clear the conditions after applying them
            _params.Clear();
            _whereActions.Clear();
        }

        internal void AddStringContains(MethodCallExpression node)
        {
            if (node.Object is MemberExpression member && node.Arguments[0] is ConstantExpression constant)
            {
                _queryParameters?.Where(where => where.WhereContains(member.Member.Name, constant?.Value?.ToString()));
                _contentQueryParameters?.Where(where => where.WhereContains(member.Member.Name, constant?.Value?.ToString()));
            }
        }

        internal void AddStringStartsWith(MethodCallExpression node)
        {
            if (node.Object is MemberExpression member && node.Arguments[0] is ConstantExpression constant)
            {
                _queryParameters?.Where(where => where.WhereStartsWith(member.Member.Name, constant?.Value?.ToString()));
                _contentQueryParameters?.Where(where => where.WhereStartsWith(member.Member.Name, constant?.Value?.ToString()));
            }
        }

        internal void AddEnumerableContains(MethodCallExpression node)
        {
            if (node.Arguments.Count == 2)
            {
                var collectionExpression = node.Arguments[0];
                var itemExpression = node.Arguments[1];

                if (collectionExpression is MemberExpression collectionMember && itemExpression is ConstantExpression constantExpression)
                {
                    var columnName = collectionMember.Member.Name;
                    var values = constantExpression.Value.ExtractValues().ToArray();

                    AddWhereInCondition(columnName, values);
                }
                else if (collectionExpression is MemberExpression collectionFieldExpression && itemExpression is MemberExpression itemFieldExpression)
                {
                    var collection = collectionFieldExpression.ExtractFieldValues().ToArray();
                    var columnName = itemFieldExpression.Member.Name;

                    AddWhereInCondition(columnName, collection);
                }
                else
                {
                    throw new NotSupportedException($"The expression types '{collectionExpression.GetType().Name}' and '{itemExpression.GetType().Name}' are not supported.");
                }
            }
            else if (node.Arguments.Count == 1 && node.Object != null)
            {
                var collectionExpression = node.Object;
                var itemExpression = node.Arguments[0];

                if (collectionExpression is MemberExpression collectionMember && itemExpression is MemberExpression itemMember)
                {
                    var collection = collectionMember.ExtractFieldValues().ToArray();
                    var columnName = itemMember.Member.Name;

                    AddWhereInCondition(columnName, collection);
                }
                else
                {
                    throw new NotSupportedException($"The expression types '{collectionExpression.GetType().Name}' and '{itemExpression.GetType().Name}' are not supported.");
                }
            }
            else
            {
                throw new NotSupportedException($"The method '{node.Method.Name}' requires either 1 or 2 arguments.");
            }
        }


        internal Expression AddQueryableSelect(MethodCallExpression node)
        {
            if (node.Arguments[1] is UnaryExpression unaryExpression &&
                unaryExpression.Operand is LambdaExpression lambdaExpression)
            {
                var visitor = new ContentItemQueryExpressionVisitor(this);
                visitor.Visit(lambdaExpression.Body);
            }
            else
            {
                throw new NotSupportedException(
                    $"The expression type '{node.Arguments[1].GetType().Name}' is not supported.");
            }

            return node;
        }

        internal Expression AddQueryableWhere(MethodCallExpression node)
        {
            if (node.Arguments[1] is UnaryExpression unaryExpression &&
                unaryExpression.Operand is LambdaExpression lambdaExpression)
            {
                var visitor = new ContentItemQueryExpressionVisitor(this);
                visitor.Visit(lambdaExpression.Body);
            }
            else
            {
                throw new NotSupportedException(
                    $"The expression type '{node.Arguments[1].GetType().Name}' is not supported.");
            }

            return node;
        }

        internal void AddWhereInCondition(string key, object?[] collection)
        {
            if (collection?.Length == 0)
            {
                return;
            }

            var elementType = collection![0]!.GetType();

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
