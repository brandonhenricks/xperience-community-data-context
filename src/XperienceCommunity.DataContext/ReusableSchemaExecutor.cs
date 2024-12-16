﻿using System.Diagnostics.CodeAnalysis;
using CMS.ContentEngine;
using Microsoft.Extensions.Logging;

namespace XperienceCommunity.DataContext
{
    public sealed class ReusableSchemaExecutor<T> : BaseContentQueryExecutor<T>
    {
        private readonly ILogger<ReusableSchemaExecutor<T>> _logger;

        public ReusableSchemaExecutor(ILogger<ReusableSchemaExecutor<T>> logger, IContentQueryExecutor queryExecutor) : base(queryExecutor)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }

        [return: NotNull]
        public override async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var results = await QueryExecutor.GetMappedResult<T>(queryBuilder, queryOptions,
                    cancellationToken: cancellationToken);

                return results ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return [];
            }
        }
    }
}
