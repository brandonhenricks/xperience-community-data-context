using Microsoft.Extensions.DependencyInjection;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    /// <summary>
    /// Provides extension methods for configuring dependency injection for XperienceDataContext.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds the XperienceDataContext services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="cacheInMinutes"></param>
        /// <returns>The modified <see cref="XperienceContextBuilder"/>.</returns>
        public static IServiceCollection AddXperienceDataContext(this IServiceCollection services, int? cacheInMinutes)
        {
            services.AddScoped(typeof(IContentItemContext<>), typeof(ContentItemContext<>));
            services.AddScoped(typeof(IPageContentContext<>), typeof(PageContentContext<>));

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

            return new XperienceContextBuilder(services);
        }
    }
}
