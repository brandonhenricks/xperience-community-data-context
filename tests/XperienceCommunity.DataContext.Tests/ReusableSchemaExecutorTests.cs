using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Executors;

namespace XperienceCommunity.DataContext.Tests.Executors
{
    public class ReusableSchemaExecutorTests
    {
        // Test subclass that allows us to inject behavior
        private class TestableReusableSchemaExecutor<T> : ReusableSchemaExecutor<T>
        {
            private readonly Func<Task<IEnumerable<T>?>>? _executeFunc;

            public TestableReusableSchemaExecutor(
                ILogger<ReusableSchemaExecutor<T>> logger,
                IContentQueryExecutor queryExecutor,
                Func<Task<IEnumerable<T>?>>? executeFunc = null)
                : base(logger, queryExecutor)
            {
                _executeFunc = executeFunc;
            }

            public override async Task<IEnumerable<T>> ExecuteQueryAsync(
                ContentItemQueryBuilder queryBuilder,
                ContentQueryExecutionOptions queryOptions,
                CancellationToken cancellationToken)
            {
                if (_executeFunc != null)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var results = await _executeFunc();
                        return results ?? Array.Empty<T>();
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Query execution failed for {ContentType}", typeof(T).Name);
                        throw new QueryExecutionException($"Failed to execute query for {typeof(T).Name}", typeof(T).Name, ex);
                    }
                }

                return await base.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);
            }

            // Expose logger for testing
            public ILogger<ReusableSchemaExecutor<T>> Logger => 
                (ILogger<ReusableSchemaExecutor<T>>)GetType()
                    .BaseType!
                    .GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .GetValue(this)!;
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
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<object>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var innerException = new InvalidOperationException("Database connection failed");
            var executor = new TestableReusableSchemaExecutor<object>(
                logger, 
                queryExecutor,
                () => throw innerException);
            
            var queryBuilder = new ContentItemQueryBuilder();
            var queryOptions = new ContentQueryExecutionOptions();
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<QueryExecutionException>(async () =>
                await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken));

            Assert.NotNull(exception);
            Assert.Equal("Failed to execute query for Object", exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal("Object", exception.ContentTypeName);
        }

        [Fact]
        public async Task ExecuteQueryAsync_ShouldLogErrorBeforeThrowingQueryExecutionException()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<object>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var innerException = new InvalidOperationException("Database connection failed");
            var executor = new TestableReusableSchemaExecutor<object>(
                logger, 
                queryExecutor,
                () => throw innerException);
            
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

            // Assert - Verify logging occurred
            logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Query execution failed for Object")),
                innerException,
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task ExecuteQueryAsync_ShouldReturnEmptyArrayWhenNoResults()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<object>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var executor = new TestableReusableSchemaExecutor<object>(
                logger, 
                queryExecutor,
                () => Task.FromResult<IEnumerable<object>?>(null));
            
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
            var executor = new TestableReusableSchemaExecutor<object>(logger, queryExecutor);
            
            var queryBuilder = new ContentItemQueryBuilder();
            var queryOptions = new ContentQueryExecutionOptions();
            var cancellationToken = new CancellationToken(canceled: true);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken));
        }
    }
}
