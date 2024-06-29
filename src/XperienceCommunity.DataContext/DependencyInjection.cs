using Microsoft.Extensions.DependencyInjection;
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
        /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddXperienceDataContext(this IServiceCollection services)
        {
            services.AddScoped(typeof(IContentItemContext<>), typeof(ContentItemContext<>));

            services.AddScoped(typeof(IPageContentContext<>), typeof(PageContentContext<>));

            return services;
        }
    }
}
