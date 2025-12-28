using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Core;
using XperienceCommunity.DataContext.Exceptions;

namespace XperienceCommunity.DataContext.Tests;

public class ProcessorSupportedQueryExecutorTests
{
    // Concrete test implementation of the abstract class
    public class TestQueryExecutor : ProcessorSupportedQueryExecutor<TestItem, IProcessor<TestItem>>
    {
        private readonly Func<Task<IEnumerable<TestItem>>>? _executeFunc;

        public TestQueryExecutor(
            ILogger logger, 
            IContentQueryExecutor queryExecutor,
            IEnumerable<IProcessor<TestItem>>? processors,
            Func<Task<IEnumerable<TestItem>>>? executeFunc = null) 
            : base(logger, queryExecutor, processors)
        {
            _executeFunc = executeFunc;
        }

        protected override async Task<IEnumerable<TestItem>> ExecuteQueryInternalAsync(
            ContentItemQueryBuilder queryBuilder,
            ContentQueryExecutionOptions queryOptions,
            CancellationToken cancellationToken)
        {
            if (_executeFunc != null)
            {
                return await _executeFunc();
            }
            
            return new List<TestItem> { new TestItem() };
        }
    }

    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestProcessor : IProcessor<TestItem>
    {
        public int Order => 0;
        
        public Task ProcessAsync(TestItem item, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Arrange
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var processors = new List<IProcessor<TestItem>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new TestQueryExecutor(null!, queryExecutor, processors));
    }

    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestQueryExecutor>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var processors = new List<IProcessor<TestItem>>();

        // Act
        var executor = new TestQueryExecutor(logger, queryExecutor, processors);

        // Assert
        Assert.NotNull(executor);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldAllowOperationCanceledExceptionToPropagate()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestQueryExecutor>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var cancellationToken = new CancellationToken(canceled: true);
        
        var executor = new TestQueryExecutor(logger, queryExecutor, null);
        
        var queryBuilder = new ContentItemQueryBuilder();
        var queryOptions = new ContentQueryExecutionOptions();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken));
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldWrapOtherExceptionsInQueryExecutionException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestQueryExecutor>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var cancellationToken = CancellationToken.None;
        
        var innerException = new InvalidOperationException("Simulated error");
        var executeFunc = new Func<Task<IEnumerable<TestItem>>>(() => throw innerException);
        
        var executor = new TestQueryExecutor(logger, queryExecutor, null, executeFunc);
        
        var queryBuilder = new ContentItemQueryBuilder();
        var queryOptions = new ContentQueryExecutionOptions();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<QueryExecutionException>(async () =>
            await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken));

        Assert.NotNull(exception);
        Assert.Contains("Failed to execute query for TestItem", exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Equal("TestItem", exception.ContentTypeName);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldLogErrorBeforeThrowingQueryExecutionException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestQueryExecutor>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var cancellationToken = CancellationToken.None;
        
        var innerException = new InvalidOperationException("Simulated error");
        var executeFunc = new Func<Task<IEnumerable<TestItem>>>(() => throw innerException);
        
        var executor = new TestQueryExecutor(logger, queryExecutor, null, executeFunc);
        
        var queryBuilder = new ContentItemQueryBuilder();
        var queryOptions = new ContentQueryExecutionOptions();

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
            Arg.Is<object>(o => o.ToString()!.Contains("Query execution failed for TestItem")),
            innerException,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldExecuteProcessorsInOrder()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestQueryExecutor>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var cancellationToken = CancellationToken.None;
        
        var executionOrder = new List<int>();
        
        var processor1 = new OrderedTestProcessor(1, executionOrder);
        var processor2 = new OrderedTestProcessor(2, executionOrder);
        
        var processors = new List<IProcessor<TestItem>> { processor2, processor1 }; // Out of order
        
        var executor = new TestQueryExecutor(logger, queryExecutor, processors);
        
        var queryBuilder = new ContentItemQueryBuilder();
        var queryOptions = new ContentQueryExecutionOptions();

        // Act
        await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken);

        // Assert - Processors should be called in order (1 then 2)
        Assert.Equal(2, executionOrder.Count);
        Assert.Equal(1, executionOrder[0]);
        Assert.Equal(2, executionOrder[1]);
    }

    public class OrderedTestProcessor : IProcessor<TestItem>
    {
        private readonly int _order;
        private readonly List<int> _executionOrder;

        public OrderedTestProcessor(int order, List<int> executionOrder)
        {
            _order = order;
            _executionOrder = executionOrder;
        }

        public int Order => _order;
        
        public Task ProcessAsync(TestItem item, CancellationToken cancellationToken = default)
        {
            _executionOrder.Add(_order);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldAllowCancellationDuringProcessing()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestQueryExecutor>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        var cts = new CancellationTokenSource();
        
        var processor = new CancellationTestProcessor(cts);
        
        var processors = new List<IProcessor<TestItem>> { processor };
        var executor = new TestQueryExecutor(logger, queryExecutor, processors);
        
        var queryBuilder = new ContentItemQueryBuilder();
        var queryOptions = new ContentQueryExecutionOptions();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await executor.ExecuteQueryAsync(queryBuilder, queryOptions, cts.Token));
    }

    public class CancellationTestProcessor : IProcessor<TestItem>
    {
        private readonly CancellationTokenSource _cts;

        public CancellationTestProcessor(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        public int Order => 1;
        
        public Task ProcessAsync(TestItem item, CancellationToken cancellationToken = default)
        {
            // Cancel during processing
            _cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
