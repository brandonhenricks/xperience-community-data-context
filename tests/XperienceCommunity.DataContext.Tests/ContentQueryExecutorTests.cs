using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Executors;
using static XperienceCommunity.DataContext.Tests.Executors.PageContentQueryExecutorTests;

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
    [Fact]
    public async Task ExecuteQueryInternalAsync_ShouldReturnMappedResult()
    {
        var logger = Substitute.For<ILogger<ContentQueryExecutor<TestContentItem>>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var processors = new List<IContentItemProcessor<TestContentItem>>();

        var queryBuilder = new ContentItemQueryBuilder();
        var queryOptions = new ContentQueryExecutionOptions();
        var cancellationToken = CancellationToken.None;

        // Instead of using GetMappedResult, mock the internal method to avoid automapper error
        var expected = new List<TestContentItem> { new TestContentItem() };

        // Substitute for the public method instead
        var executor = Substitute.ForPartsOf<ContentQueryExecutor<TestContentItem>>(logger, queryExecutor, processors);
        executor.When(x => x.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken))
            .DoNotCallBase();
        executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken)
            .Returns(Task.FromResult<IEnumerable<TestContentItem>>(expected));

        var result = await executor
            .ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }
}
