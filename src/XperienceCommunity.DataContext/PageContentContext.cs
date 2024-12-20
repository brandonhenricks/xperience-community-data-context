using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    public sealed class PageContentContext<T> : BaseContentContext<T>, IPageContentContext<T> where T : class, IWebPageFieldsSource, new()
    {
        private readonly PageContentQueryExecutor<T> _pageContentQueryExecutor;

        public PageContentContext(IProgressiveCache cache, PageContentQueryExecutor<T> pageContentQueryExecutor, IWebsiteChannelContext websiteChannelContext, XperienceDataContextConfig config)
            : base(websiteChannelContext, cache, config)
        {
            ArgumentNullException.ThrowIfNull(pageContentQueryExecutor);
            _pageContentQueryExecutor = pageContentQueryExecutor;
        }

        protected override async Task<IEnumerable<T>> ExecuteQueryAsync(ContentItemQueryBuilder queryBuilder, ContentQueryExecutionOptions queryOptions, CancellationToken cancellationToken)
        {
            return await _pageContentQueryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);
        }

        public IPageContentContext<T> InChannel(string channelName)
        {
            // Implement InChannel logic here
            return this;
        }

        public IPageContentContext<T> OnPath(PathMatch pathMatch)
        {
            // Implement OnPath logic here
            return this;
        }
    }
}
