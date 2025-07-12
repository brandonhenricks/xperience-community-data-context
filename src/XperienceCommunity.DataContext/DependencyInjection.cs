using Microsoft.Extensions.DependencyInjection;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Contexts;
using XperienceCommunity.DataContext.Executors;

namespace XperienceCommunity.DataContext;

/// <summary>
/// Provides extension methods for configuring dependency injection for XperienceDataContext.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the XperienceDataContext services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="cacheInMinutes">The cache timeout in minutes.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddXperienceDataContext(this IServiceCollection services, int? cacheInMinutes)
    {
        services.AddScoped(typeof(IContentItemContext<>), typeof(ContentItemContext<>));
        services.AddScoped(typeof(IPageContentContext<>), typeof(PageContentContext<>));
        services.AddScoped(typeof(IReusableSchemaContext<>), typeof(ReusableSchemaContext<>));
        services.AddScoped<IXperienceDataContext, XperienceDataContext>();
        services.AddScoped(typeof(ContentQueryExecutor<>));
        services.AddScoped(typeof(PageContentQueryExecutor<>));
        services.AddScoped(typeof(ReusableSchemaExecutor<>));

        var config = new XperienceDataContextConfig();

        if (cacheInMinutes.HasValue)
        {
            config.CacheTimeOut = cacheInMinutes.Value;
        }

        services.AddSingleton(config);

        return services;
    }

    /// <summary>
    /// Adds the XperienceDataContext services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The modified <see cref="XperienceContextBuilder"/>.</returns>
    public static XperienceContextBuilder AddXperienceDataContext(this IServiceCollection services)
    {
        services.AddScoped(typeof(IContentItemContext<>), typeof(ContentItemContext<>));
        services.AddScoped(typeof(IPageContentContext<>), typeof(PageContentContext<>));
        services.AddScoped(typeof(IReusableSchemaContext<>), typeof(ReusableSchemaContext<>));
        services.AddScoped<IXperienceDataContext, XperienceDataContext>();
        services.AddScoped(typeof(ContentQueryExecutor<>));
        services.AddScoped(typeof(PageContentQueryExecutor<>));
        services.AddScoped(typeof(ReusableSchemaExecutor<>));

        return new XperienceContextBuilder(services);
    }
}
