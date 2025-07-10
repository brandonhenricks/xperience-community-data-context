using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Executors;

namespace XperienceCommunity.DataContext.Tests;

public class ContentQueryExecutorTests
{
    public class TestContentItem : IContentItemFieldsSource
    {
        public IDictionary<string, object?> Fields { get; } = new Dictionary<string, object?>();

        public ContentItemFields SystemFields => throw new NotImplementedException();
    }

    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var logger = Substitute.For<ILogger<ContentQueryExecutor<TestContentItem>>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var processors = Substitute.For<IEnumerable<IContentItemProcessor<TestContentItem>>>();

        var executor = new ContentQueryExecutor<TestContentItem>(logger, queryExecutor, processors);

        Assert.NotNull(executor);
    }

}
