using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    /// <summary>
    /// Provides context for querying content items of a specified type.
    /// </summary>
    /// <typeparam name="T">The type of the content item.</typeparam>
    public sealed class ContentItemContext<T> : BaseContentContext<T>, IContentItemContext<T> where T : class, IContentItemFieldsSource, new()
    {
        private readonly ContentQueryExecutor<T> _contentQueryExecutor;

        public ContentItemContext(IWebsiteChannelContext websiteChannelContext, IProgressiveCache cache, ContentQueryExecutor<T> contentQueryExecutor, XperienceDataContextConfig config)
            : base(websiteChannelContext, cache, config)
        {
            ArgumentNullException.ThrowIfNull(contentQueryExecutor);
            _contentQueryExecutor = contentQueryExecutor;
        }

        protected override async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder, ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            return await _contentQueryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);
        }
    }
}
