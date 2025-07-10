using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Executors;

namespace XperienceCommunity.DataContext.Contexts;

public sealed class XperienceDataContext : IXperienceDataContext
{
    private readonly IProgressiveCache _cache;
    private readonly XperienceDataContextConfig _config;
    private readonly IContentQueryExecutor _executor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEnumerable<IProcessor> _processors;
    private readonly IWebsiteChannelContext _websiteChannelContext;

    public XperienceDataContext(IProgressiveCache cache, IWebsiteChannelContext websiteChannelContext,
        IEnumerable<IProcessor> processors, XperienceDataContextConfig config, IContentQueryExecutor executor,
        ILoggerFactory loggerFactory)
    {
        _cache = cache;
        _websiteChannelContext = websiteChannelContext;
        _processors = processors;
        _config = config;
        _executor = executor;
        _loggerFactory = loggerFactory;
    }

    public IContentItemContext<T> ForContentType<T>() where T : class, IContentItemFieldsSource, new()
    {
        var logger = _loggerFactory.CreateLogger<ContentQueryExecutor<T>>();

        var executor = new ContentQueryExecutor<T>(logger, _executor, _processors.OfType<IContentItemProcessor<T>>());

        return new ContentItemContext<T>(_websiteChannelContext, _cache, executor, _config);
    }

    public IPageContentContext<T> ForPageContentType<T>() where T : class, IWebPageFieldsSource, new()
    {
        var logger = _loggerFactory.CreateLogger<PageContentQueryExecutor<T>>();
        var executor =
            new PageContentQueryExecutor<T>(logger, _executor, _processors.OfType<IPageContentProcessor<T>>());
        return new PageContentContext<T>(_cache, executor, _websiteChannelContext, _config);
    }

    public IReusableSchemaContext<T> ForReusableSchema<T>()
    {
        var logger = _loggerFactory.CreateLogger<ReusableSchemaExecutor<T>>();

        var executor = new ReusableSchemaExecutor<T>(logger, _executor);

        return new ReusableSchemaContext<T>(_websiteChannelContext, _cache, executor, _config);
    }
}
