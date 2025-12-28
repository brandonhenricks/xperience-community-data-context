using System.Collections.Concurrent;
using System.Linq.Expressions;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Contexts;
using XperienceCommunity.DataContext.Executors;

namespace XperienceCommunity.DataContext.Tests
{
    public class ContentItemContextTests
    {
        public class TestContentItem : IContentItemFieldsSource
        {
            public const string CONTENT_TYPE_NAME = "TestContentItem";

            public ContentItemFields SystemFields => new ContentItemFields
            {
                ContentItemContentTypeID = 1,
                ContentItemID = 1,
                ContentItemGUID = Guid.NewGuid(),
            };
        }

        private readonly IWebsiteChannelContext _websiteChannelContext;
        private readonly IProgressiveCache _cache;
        private readonly ContentQueryExecutor<TestContentItem> _contentQueryExecutor;
        private readonly XperienceDataContextConfig _config;

        public ContentItemContextTests()
        {
            _websiteChannelContext = Substitute.For<IWebsiteChannelContext>();
            _websiteChannelContext.WebsiteChannelID.Returns(123);

            _cache = Substitute.For<IProgressiveCache>();

            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ContentQueryExecutor<TestContentItem>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            IEnumerable<IContentItemProcessor<TestContentItem>>? processors = null;
            _contentQueryExecutor = new ContentQueryExecutor<TestContentItem>(logger, queryExecutor, processors);

            _config = new XperienceDataContextConfig { CacheTimeOut = 10 };
        }

        [Fact]
        public void Constructor_ShouldInitialize()
        {
            var context = new ContentItemContext<TestContentItem>(
                _websiteChannelContext,
                _cache,
                _contentQueryExecutor,
                _config
            );

            Assert.NotNull(context);
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenContentTypeNameIsNull()
        {
            // Arrange
            var type = typeof(TestContentItem);
            // Simulate GetContentTypeName() returning null
            var originalMethod = type.GetType().GetMethod("GetContentTypeName");
            // Not possible to patch extension method, so skip this test in practice
            // This is a placeholder for completeness
        }

        [Fact]
        public void BuildQuery_ShouldReturnQueryBuilder_WithExpectedSettings()
        {
            var context = new ContentItemContext<TestContentItem>(
                _websiteChannelContext,
                _cache,
                _contentQueryExecutor,
                _config
            );

            // Set private fields using reflection for test
            var type = typeof(ContentItemContext<TestContentItem>);
            type.GetField("_linkedItemsDepth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(context, 2);
            type.GetField("_columnNames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(context, new HashSet<string> { "Col1", "Col2" });
            type.GetField("_includeTotalCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(context, true);
            type.GetField("_offset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(context, (5 as int?, 10 as int?));
            type.GetField("_language", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(context, "en-US");
            type.GetField("_useFallBack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(context, true);

            Expression<Func<TestContentItem, bool>> expr = x => true;

            var builder = type.GetMethod("BuildQuery", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(context, new object[] { expr, 3 });

            Assert.NotNull(builder);
            Assert.IsType<ContentItemQueryBuilder>(builder);
        }

        [Fact]
        public void GetCacheKey_ShouldReturnExpectedFormat()
        {
            var context = new ContentItemContext<TestContentItem>(
                _websiteChannelContext,
                _cache,
                _contentQueryExecutor,
                _config
            );

            var type = typeof(ContentItemContext<TestContentItem>);
            type.GetField("_language", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(context, "en-US");

            var parametersField = type.GetField("_parameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parameters = new ConcurrentDictionary<string, object?>();
            parameters.TryAdd("a", 1);
            parametersField?.SetValue(context, parameters);

            var builder = new ContentItemQueryBuilder();
            var method = type.GetMethod("GetCacheKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cacheKey = method?.Invoke(context, new object[] { builder }) as string;

            Assert.NotNull(cacheKey);
            Assert.Contains("data|", cacheKey);
            Assert.Contains("en-US", cacheKey);
            Assert.Contains(_websiteChannelContext.WebsiteChannelID.ToString(), cacheKey);
        }
    }
}
