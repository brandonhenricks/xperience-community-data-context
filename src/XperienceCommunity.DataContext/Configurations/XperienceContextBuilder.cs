using CMS.ContentEngine;
using CMS.Websites;
using Microsoft.Extensions.DependencyInjection;
using XperienceCommunity.DataContext.Abstractions.Processors;

namespace XperienceCommunity.DataContext.Configurations;

/// <summary>
/// Builder class for configuring XperienceDataContext.
/// </summary>
public sealed class XperienceContextBuilder
{
    private readonly IServiceCollection _services;
    private readonly XperienceDataContextConfig _config;

    public XperienceContextBuilder(IServiceCollection services)
    {
        _services = services;
        _config = new XperienceDataContextConfig();

        // Register the default config (can be overridden by SetCacheTimeout)
        _services.AddSingleton(_config);
    }

    /// <summary>
    /// Registers a ContentItemProcessor for the specified content type.
    /// </summary>
    /// <typeparam name="TContent">The type of the content item.</typeparam>
    /// <typeparam name="TProcessor">The type of the content item processor.</typeparam>
    /// <returns>The current <see cref="XperienceContextBuilder"/> instance.</returns>
    public XperienceContextBuilder AddContentItemProcessor<TContent, TProcessor>()
        where TContent : class, IContentItemFieldsSource, new()
        where TProcessor : class, IContentItemProcessor<TContent>
    {
        _services.AddScoped<IContentItemProcessor<TContent>, TProcessor>();
        return this;
    }

    /// <summary>
    /// Registers a PageContentProcessor for the specified page type.
    /// </summary>
    /// <typeparam name="TPage">The type of the page.</typeparam>
    /// <typeparam name="TProcessor">The type of the page content processor.</typeparam>
    /// <returns>The current <see cref="XperienceContextBuilder"/> instance.</returns>
    public XperienceContextBuilder AddPageContentProcessor<TPage, TProcessor>()
        where TPage : class, IWebPageFieldsSource, new()
        where TProcessor : class, IPageContentProcessor<TPage>
    {
        _services.AddScoped<IPageContentProcessor<TPage>, TProcessor>();
        return this;
    }

    /// <summary>
    /// Sets the cache timeout in minutes for XperienceDataContext.
    /// </summary>
    /// <param name="timeoutInMinutes">The cache timeout in minutes.</param>
    /// <returns>The current <see cref="XperienceContextBuilder"/> instance.</returns>
    public XperienceContextBuilder SetCacheTimeout(int timeoutInMinutes)
    {
        _config.CacheTimeOut = timeoutInMinutes;

        // Remove any existing config registration and add the updated one
        var existingDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(XperienceDataContextConfig));
        if (existingDescriptor != null)
        {
            _services.Remove(existingDescriptor);
        }

        _services.AddSingleton(_config);

        return this;
    }
}
