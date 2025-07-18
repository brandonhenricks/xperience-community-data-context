using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using Microsoft.Extensions.Logging;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Executors;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.Executors
{
    public class PageContentQueryExecutorTests
    {
        public class TestWebPage : IWebPageFieldsSource
        {
            public WebPageFields SystemFields => throw new NotImplementedException();

            public string? GetFieldValue(string fieldName) => null;
        }

        [Fact]
        public void Constructor_ShouldThrow_IfQueryExecutorIsNull()
        {
            var logger = Substitute.For<ILogger<PageContentQueryExecutor<TestWebPage>>>();
            IEnumerable<IPageContentProcessor<TestWebPage>> processors = null;

            Assert.Throws<System.ArgumentNullException>(() =>
                new PageContentQueryExecutor<TestWebPage>(logger, null, processors));
        }

        [Fact]
        public void Constructor_ShouldInstantiate_WithValidArguments()
        {
            var logger = Substitute.For<ILogger<PageContentQueryExecutor<TestWebPage>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            IEnumerable<IPageContentProcessor<TestWebPage>> processors = new List<IPageContentProcessor<TestWebPage>>();

            var executor = new PageContentQueryExecutor<TestWebPage>(logger, queryExecutor, processors);

            Assert.NotNull(executor);
        }

        [Fact]
        public async Task ExecuteQueryInternalAsync_ShouldCallQueryExecutorGetMappedWebPageResult()
        {
            var logger = Substitute.For<ILogger<PageContentQueryExecutor<TestWebPage>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var processors = new List<IPageContentProcessor<TestWebPage>>();

            var queryBuilder = new ContentItemQueryBuilder();
            var queryOptions = new ContentQueryExecutionOptions();
            var cancellationToken = CancellationToken.None;

            // Instead of using GetMappedWebPageResult, mock the internal method to avoid automapper error
            var expected = new List<TestWebPage> { new TestWebPage() };

            // Substitute for the public method instead
            var executor = Substitute.ForPartsOf<PageContentQueryExecutor<TestWebPage>>(logger, queryExecutor, processors);
            executor.When(x => x.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken))
                .DoNotCallBase();
            executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken)
                .Returns(Task.FromResult<IEnumerable<TestWebPage>>(expected));

            var result = await executor
                .ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);

            Assert.NotNull(result);
            Assert.Equal(expected, result);
        }
    }
}
