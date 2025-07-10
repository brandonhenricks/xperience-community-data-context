using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Contexts;
using XperienceCommunity.DataContext.Executors;

namespace XperienceCommunity.DataContext.Tests
{
    public class XperienceDataContextTests
    {
        private readonly IProgressiveCache _cache = Substitute.For<IProgressiveCache>();
        private readonly IWebsiteChannelContext _websiteChannelContext = Substitute.For<IWebsiteChannelContext>();
        private readonly IEnumerable<IProcessor> _processors = new List<IProcessor>();
        private readonly XperienceDataContextConfig _config = new XperienceDataContextConfig();
        private readonly IContentQueryExecutor _executor = Substitute.For<IContentQueryExecutor>();
        private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();

        [Fact]
        public void Constructor_ShouldInstantiate()
        {
            var context = new XperienceDataContext(
                _cache,
                _websiteChannelContext,
                _processors,
                _config,
                _executor,
                _loggerFactory
            );

            Assert.NotNull(context);
        }

        [Fact]
        public void ForContentType_ShouldReturn_ContentItemContext()
        {
            var logger = Substitute.For<ILogger<ContentQueryExecutor<FakeContentItem>>>();
            _loggerFactory.CreateLogger<ContentQueryExecutor<FakeContentItem>>().Returns(logger);

            var context = new XperienceDataContext(
                _cache,
                _websiteChannelContext,
                _processors,
                _config,
                _executor,
                _loggerFactory
            );

            var result = context.ForContentType<FakeContentItem>();

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IContentItemContext<FakeContentItem>>(result);
        }

        [Fact]
        public void ForPageContentType_ShouldReturn_PageContentContext()
        {
            var logger = Substitute.For<ILogger<PageContentQueryExecutor<FakeWebPage>>>();
            _loggerFactory.CreateLogger<PageContentQueryExecutor<FakeWebPage>>().Returns(logger);

            var context = new XperienceDataContext(
                _cache,
                _websiteChannelContext,
                _processors,
                _config,
                _executor,
                _loggerFactory
            );

            var result = context.ForPageContentType<FakeWebPage>();

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IPageContentContext<FakeWebPage>>(result);
        }

        [Fact]
        public void ForReusableSchema_ShouldReturn_ReusableSchemaContext()
        {
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<IFakeSchema>>>();
            _loggerFactory.CreateLogger<ReusableSchemaExecutor<IFakeSchema>>().Returns(logger);

            var context = new XperienceDataContext(
                _cache,
                _websiteChannelContext,
                _processors,
                _config,
                _executor,
                _loggerFactory
            );

            var result = context.ForReusableSchema<IFakeSchema>();

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReusableSchemaContext<IFakeSchema>>(result);
        }

        // Fake types for testing
        public class FakeContentItem : IContentItemFieldsSource
        {
            public const string CONTENT_TYPE_NAME = "FakeContentItem";

            public ContentItemFields SystemFields => throw new NotImplementedException();
        }

        public class FakeWebPage : IWebPageFieldsSource
        {
            public const string CONTENT_TYPE_NAME = "FakePage";
            public WebPageFields SystemFields => throw new NotImplementedException();
        }

        public class FakeSchema
        {
        }

        public interface IFakeSchema
        {
            public const string REUSABLE_FIELD_SCHEMA_NAME = "FakeSchema";
        }
    }
}
