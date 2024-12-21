﻿using System.Linq.Expressions;
using CMS.ContentEngine;

namespace XperienceCommunity.DataContext.Interfaces
{
    /// <summary>
    /// Defines a method to build a query from an expression.
    /// </summary>
    public interface IExpressionQueryBuilder
    {
        /// <summary>
        /// Gets the type of the builder.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Builds a query based on the provided expression.
        /// </summary>
        /// <param name="expression">The expression to build the query from.</param>
        /// <param name="topN">The optional number of top records to return.</param>
        /// <returns>A <see cref="ContentItemQueryBuilder"/> representing the built query.</returns>
        ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null);
    }
}