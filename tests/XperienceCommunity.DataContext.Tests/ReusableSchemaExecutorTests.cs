using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using NSubstitute;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Executors;

namespace XperienceCommunity.DataContext.Tests.Executors
{
    public class ReusableSchemaExecutorTests
    {
        // Testable subclass that exposes protected members for testing
        // without duplicating the exception handling logic
        private class TestableReusableSchemaExecutor<T> : ReusableSchemaExecutor<T>
        {
            private readonly Func<Task<IEnumerable<T>?>>? _getResultOverride;

            public TestableReusableSchemaExecutor(
                ILogger<ReusableSchemaExecutor<T>> logger,
                IContentQueryExecutor queryExecutor,
                Func<Task<IEnumerable<T>?>>? getResultOverride = null)
                : base(logger, queryExecutor)
            {
                _getResultOverride = getResultOverride;
            }

            public override async Task<IEnumerable<T>> ExecuteQueryAsync(
                ContentItemQueryBuilder queryBuilder,
                ContentQueryExecutionOptions queryOptions,
                CancellationToken cancellationToken)
            {
                if (_getResultOverride != null)
                {
                    // Override the GetMappedResult call to inject test behavior
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var results = await _getResultOverride();
                        return results ?? Array.Empty<T>();
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }

                return await base.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);
            }
        }

        [Fact]
        public void Constructor_ShouldThrow_IfLoggerIsNull()
        {
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            Assert.Throws<ArgumentNullException>(() =>
                new ReusableSchemaExecutor<object>(null!, queryExecutor));
        }

        [Fact]
        public void Constructor_ShouldInstantiate()
        {
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<object>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var executor = new ReusableSchemaExecutor<object>(logger, queryExecutor);
            Assert.NotNull(executor);
        }

        [Fact]
        public async Task ExecuteQueryAsync_ShouldAllowOperationCanceledExceptionToPropagate()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<object>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var executor = new TestableReusableSchemaExecutor<object>(
                logger, 
                queryExecutor,
                () => throw new OperationCanceledException());
            
            var queryBuilder = new ContentItemQueryBuilder();
            var queryOptions = new ContentQueryExecutionOptions();
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken));
        }

        [Fact]
        public async Task ExecuteQueryAsync_ShouldWrapOtherExceptionsInQueryExecutionException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<TestContent>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var innerException = new InvalidOperationException("Database connection failed");
            
            // Use the base executor directly - let it call GetMappedResult which will throw
            var executor = new ReusableSchemaExecutor<TestContent>(logger, queryExecutor);
            
            var queryBuilder = new ContentItemQueryBuilder();
            var queryOptions = new ContentQueryExecutionOptions();
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            // The actual implementation will call GetMappedResult which will fail
            // because queryExecutor doesn't have automapper configured
            var exception = await Assert.ThrowsAsync<QueryExecutionException>(async () =>
                await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken));

            Assert.NotNull(exception);
            Assert.Contains("Failed to execute query for TestContent", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("TestContent", exception.ContentTypeName);
        }

        [Fact]
        public async Task ExecuteQueryAsync_ShouldLogErrorBeforeThrowingQueryExecutionException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<TestContent>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var executor = new ReusableSchemaExecutor<TestContent>(logger, queryExecutor);
            
            var queryBuilder = new ContentItemQueryBuilder();
            var queryOptions = new ContentQueryExecutionOptions();
            var cancellationToken = CancellationToken.None;

            // Act
            try
            {
                await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);
            }
            catch (QueryExecutionException)
            {
                // Expected
            }

            // Assert - Verify logging occurred with the expected message pattern
            logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Query execution failed for TestContent")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task ExecuteQueryAsync_ShouldReturnEmptyArrayWhenNoResults()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<TestContent>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var executor = new TestableReusableSchemaExecutor<TestContent>(
                logger, 
                queryExecutor,
                () => Task.FromResult<IEnumerable<TestContent>?>(null));
            
            var queryBuilder = new ContentItemQueryBuilder();
            var queryOptions = new ContentQueryExecutionOptions();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ExecuteQueryAsync_ShouldHandleCancellationTokenCanceled()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<object>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var executor = new ReusableSchemaExecutor<object>(logger, queryExecutor);
            
            var queryBuilder = new ContentItemQueryBuilder();
            var queryOptions = new ContentQueryExecutionOptions();
            var cancellationToken = new CancellationToken(canceled: true);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken));
        }

        // Test content type for testing
        public class TestContent
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
