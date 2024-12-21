using Microsoft.Extensions.DependencyInjection;
using XperienceCommunity.DataContext.Builders;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext
{
    /// <summary>
    /// Provides extension methods for configuring dependency injection for XperienceDataContext.
    /// </summary>
    public static class DependencyInjection
    {
        private static void AddContext(this IServiceCollection services)
        {
            services.AddScoped(typeof(IContentItemContext<>), typeof(ContentItemContext<>));
            services.AddScoped(typeof(IPageContentContext<>), typeof(PageContentContext<>));
            services.AddScoped(typeof(IReusableSchemaContext<>), typeof(ReusableSchemaContext<>));
            services.AddScoped<IXperienceDataContext, XperienceDataContext>();
        }

        private static void AddQueryExecutors(this IServiceCollection services)
        {
            services.AddScoped(typeof(ContentQueryExecutor<>));
            services.AddScoped(typeof(PageContentQueryExecutor<>));
        }

        private static void AddQueryBuilders(this IServiceCollection services)
        {
            services.AddScoped<IExpressionQueryBuilder, ContentQueryBuilder>();
            services.AddScoped<IExpressionQueryBuilder, PageQueryBuilder>();
            services.AddScoped<IExpressionQueryBuilder, ReusableSchemaQueryBuilder>();
        }

        /// <summary>
        /// Adds the XperienceDataContext services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="cacheInMinutes"></param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddXperienceDataContext(this IServiceCollection services, int? cacheInMinutes)
        {
            services.AddContext();
            services.AddQueryExecutors();
            services.AddQueryBuilders();

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
            services.AddContext();
            services.AddQueryExecutors();
            services.AddQueryBuilders();

            return new XperienceContextBuilder(services);
        }
    }
}
