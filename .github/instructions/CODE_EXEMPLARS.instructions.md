# Code Exemplars

This file highlights a few classes and patterns in the repository that are good examples of clean design, testability, and SOLID principles.

- `BaseDataContext<T, TExecutor>`
  - Why exemplary: centralizes shared behavior (caching, options, query lifecycle) and reduces duplication across context types.
  - Excerpt (simplified):

```csharp
// Initializes query and applies fluent filters
protected virtual void InitializeQuery() => _query ??= Enumerable.Empty<T>().AsQueryable();

public virtual IDataContext<T> Where(Expression<Func<T,bool>> predicate)
{
    InitializeQuery();
    _query = _query?.Where(predicate);
    return this;
}
```

- `ContentItemQueryExpressionVisitor`
  - Why exemplary: clean visitor + processor separation; processors are small, focused translation units.
  - Best practice: add a new processor under `Expressions/Processors/` and register it in the visitor.

- `ContentQueryExecutor<T>`
  - Why exemplary: executor pattern isolates external Kentico calls behind an interface so tests can mock `IContentQueryExecutor`.

- `XperienceContextBuilder`
  - Why exemplary: builder pattern for configuring DI and processors; keeps DI registrations fluent and discoverable.

How to follow these exemplars when contributing
- Prefer small, single-responsibility processors for expression handling.
- Keep DI registrations centralized in `DependencyInjection` and `XperienceContextBuilder`.
- Write unit tests for processors and executors that mock Kentico interfaces.
# Code Exemplars

This document highlights exemplary code patterns, classes, and methods that demonstrate best practices in the XperienceCommunity.DataContext library. Use these as reference implementations when contributing or extending the library.

---

## 1. Clean Architecture: Base Class Design

### Exemplar: `BaseDataContext<T, TExecutor>`
**Location**: [src/XperienceCommunity.DataContext/Core/BaseDataContext.cs](../../src/XperienceCommunity.DataContext/Core/BaseDataContext.cs)

**Why it''s exemplary**:
- **Template Method Pattern**: Defines common workflow, delegates specifics to subclasses
- **70%+ Code Reduction**: Eliminated duplication across three context types
- **SOLID Principles**: Open for extension (abstract methods), closed for modification
- **Comprehensive Documentation**: XML doc comments on all public members

```csharp
public abstract class BaseDataContext<T, TExecutor> : IDataContext<T>
    where TExecutor : BaseContentQueryExecutor<T>
{
    // Dependencies injected via constructor - DI best practice
    protected readonly IProgressiveCache _cache;
    protected readonly XperienceDataContextConfig _config;
    protected readonly TExecutor _queryExecutor;
    
    // Template method - defines workflow, subclasses customize behavior
    public virtual async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateQuery();
        
        var queryBuilder = BuildQuery(_query?.Expression!); // Abstract - subclass implements
        var queryOptions = CreateQueryOptions();
        
        return await GetOrCacheAsync(
            () => _queryExecutor.ExecuteQueryAsync(queryBuilder, queryOptions, cancellationToken),
            GetCacheKey(queryBuilder)); // Abstract - subclass implements
    }
    
    // Protected abstract methods - extension points for subclasses
    protected abstract ContentItemQueryBuilder BuildQuery(Expression expression, int? topN = null);
    protected abstract string GetCacheKey(ContentItemQueryBuilder queryBuilder);
}
```

**Key Takeaways**:
- Use abstract base classes to share common behavior
- Template methods delegate customization to subclasses
- Always provide `CancellationToken` parameters for async methods
- Validate inputs before proceeding with operations

---

## 2. Visitor Pattern: Expression Processing

### Exemplar: `ContentItemQueryExpressionVisitor`
**Location**: [src/XperienceCommunity.DataContext/Expressions/Visitors/ContentItemQueryExpressionVisitor.cs](../../src/XperienceCommunity.DataContext/Expressions/Visitors/ContentItemQueryExpressionVisitor.cs)

**Why it''s exemplary**:
- **Visitor Pattern**: Separates expression structure from processing logic
- **Strategy Pattern Integration**: Delegates to processor strategies by expression type
- **Extensibility**: New expression types require only new processors, no visitor changes
- **Defensive Programming**: Checks processor availability before invocation

```csharp
internal sealed class ContentItemQueryExpressionVisitor : ExpressionVisitor
{
    private readonly IExpressionContext _context;
    private readonly Dictionary<ExpressionType, IExpressionProcessor<BinaryExpression>> _binaryExpressionProcessors;
    
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Strategy pattern: select processor based on expression type
        if (_binaryExpressionProcessors.TryGetValue(node.NodeType, out var processor))
        {
            processor.Process(node);
            return node;
        }
        
        throw new UnsupportedExpressionException(node.NodeType, node);
    }
    
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Enhanced processors checked first for optimization
        if (_collectionProcessor.CanProcess(node))
        {
            _collectionProcessor.Process(node);
            return node;
        }
        
        // Fall back to general processor
        if (_expressionProcessors.TryGetValue(typeof(MethodCallExpression), out var processor))
        {
            ((IExpressionProcessor<MethodCallExpression>)processor).Process(node);
            return node;
        }
        
        return base.VisitMethodCall(node);
    }
}
```

**Key Takeaways**:
- Use the Visitor pattern for tree traversal with type-specific operations
- Maintain processor dictionaries for O(1) lookup
- Provide clear error messages for unsupported operations
- Layer specialized processors over general ones for optimization

---

## 3. Defensive Programming: Expression Processor

### Exemplar: `BinaryExpressionProcessor`
**Location**: [src/XperienceCommunity.DataContext/Expressions/Processors/BinaryExpressionProcessor.cs](../../src/XperienceCommunity.DataContext/Expressions/Processors/BinaryExpressionProcessor.cs)

**Why it''s exemplary**:
- **Conditional Compilation**: Debug-only validation with `[Conditional("DEBUG")]`
- **Performance Optimization**: No runtime cost in Release builds
- **Comprehensive Validation**: Early detection of malformed expressions
- **Debugging Support**: `DebuggerDisplay` and `DebuggerStepThrough` attributes

```csharp
[DebuggerDisplay("Processor: Binary, Context: {_context.Parameters.Count} params, {_context.WhereActions.Count} actions")]
internal sealed class BinaryExpressionProcessor : IExpressionProcessor<BinaryExpression>
{
    private readonly IExpressionContext _context;
    
    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerStepThrough]
    private void ValidateExpression(BinaryExpression node, [CallerMemberName] string? callerName = null)
    {
        if (node.Left == null || node.Right == null)
        {
            throw new ExpressionProcessingException(
                "Binary expression must have both left and right operands",
                node,
                $"NodeType: {node.NodeType}, Caller: {callerName}");
        }
    }
    
    public void Process(BinaryExpression node)
    {
        ValidateExpression(node); // No-op in Release builds
        
        // Diagnostic logging
        DataContextDiagnostics.LogDiagnostic(
            "ExpressionProcessing", 
            $"Processing binary expression: {node.NodeType}",
            LogLevel.Debug);
        
        // Process based on node type
        switch (node.NodeType)
        {
            case ExpressionType.Equal:
                ProcessEquality(node, isEqual: true);
                break;
            // ... other cases
        }
    }
}
```

**Key Takeaways**:
- Use `[Conditional("DEBUG")]` for debug-only validation (zero cost in Release)
- Add `DebuggerDisplay` attributes for better debugging experience
- Log diagnostic information for troubleshooting
- Provide detailed exception messages with context

---

## 4. Builder Pattern: Fluent Configuration

### Exemplar: `XperienceContextBuilder`
**Location**: [src/XperienceCommunity.DataContext/Configurations/XperienceContextBuilder.cs](../../src/XperienceCommunity.DataContext/Configurations/XperienceContextBuilder.cs)

**Why it''s exemplary**:
- **Fluent Interface**: Returns `this` for method chaining
- **Type Safety**: Compile-time checks via generic constraints
- **Clear Intent**: Method names describe actions clearly
- **Consistent API**: All configuration methods follow same pattern

```csharp
public sealed class XperienceContextBuilder
{
    private readonly IServiceCollection _services;
    private readonly XperienceDataContextConfig _config;
    
    // Fluent method: returns this for chaining
    public XperienceContextBuilder AddContentItemProcessor<TContent, TProcessor>()
        where TContent : class, IContentItemFieldsSource, new()
        where TProcessor : class, IContentItemProcessor<TContent>
    {
        _services.AddScoped<IContentItemProcessor<TContent>, TProcessor>();
        return this; // Enable chaining
    }
    
    public XperienceContextBuilder SetCacheTimeout(int timeoutInMinutes)
    {
        _config.CacheTimeOut = timeoutInMinutes;
        
        // Remove existing registration, add updated config
        var existingDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(XperienceDataContextConfig));
        if (existingDescriptor != null)
        {
            _services.Remove(existingDescriptor);
        }
        
        _services.AddSingleton(_config);
        return this;
    }
}
```

**Usage Example**:
```csharp
builder.Services.AddXperienceDataContext()
    .AddContentItemProcessor<BlogPost, BlogPostEnricher>()
    .AddPageContentProcessor<WebPage, WebPageProcessor>()
    .SetCacheTimeout(30);
```

**Key Takeaways**:
- Return `this` from configuration methods for fluent chaining
- Use generic constraints to enforce type safety at compile time
- Maintain immutability where possible, or clearly document mutation
- Provide clear, action-oriented method names

---

## 5. Dependency Injection: Registration Pattern

### Exemplar: `DependencyInjection.AddXperienceDataContext`
**Location**: [src/XperienceCommunity.DataContext/DependencyInjection.cs](../../src/XperienceCommunity.DataContext/DependencyInjection.cs)

**Why it''s exemplary**:
- **Extension Method Pattern**: Natural integration with `IServiceCollection`
- **Service Lifetime Awareness**: Scoped for contexts, singleton for config
- **Generic Registration**: Type parameters for flexible service registration
- **Configuration Options**: Multiple overloads for different use cases

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddXperienceDataContext(
        this IServiceCollection services, 
        int? cacheInMinutes)
    {
        // Register contexts as scoped - per-request lifetime
        services.AddScoped(typeof(IContentItemContext<>), typeof(ContentItemContext<>));
        services.AddScoped(typeof(IPageContentContext<>), typeof(PageContentContext<>));
        services.AddScoped(typeof(IReusableSchemaContext<>), typeof(ReusableSchemaContext<>));
        
        // Factory for creating contexts
        services.AddScoped<IXperienceDataContext, XperienceDataContext>();
        
        // Executors - scoped lifetime
        services.AddScoped(typeof(ContentQueryExecutor<>));
        services.AddScoped(typeof(PageContentQueryExecutor<>));
        
        // Configuration - singleton, shared across all requests
        var config = new XperienceDataContextConfig
        {
            CacheTimeOut = cacheInMinutes ?? 60
        };
        services.AddSingleton(config);
        
        return services;
    }
    
    // Builder pattern overload for advanced configuration
    public static XperienceContextBuilder AddXperienceDataContext(this IServiceCollection services)
    {
        // Register core services
        // ... (same as above)
        
        // Return builder for fluent configuration
        return new XperienceContextBuilder(services);
    }
}
```

**Key Takeaways**:
- Use extension methods for natural API integration
- Choose appropriate service lifetimes (scoped, singleton, transient)
- Provide multiple overloads for simple and advanced scenarios
- Register open generic types for flexible dependency resolution

---

## 6. Async/Await Best Practices

### Exemplar: `ProcessorSupportedQueryExecutor.ExecuteQueryAsync`
**Location**: [src/XperienceCommunity.DataContext/Core/ProcessorSupportedQueryExecutor.cs](../../src/XperienceCommunity.DataContext/Core/ProcessorSupportedQueryExecutor.cs)

**Why it''s exemplary**:
- **CancellationToken Support**: All async methods accept cancellation
- **Proper Async Flow**: Uses `await` correctly, no blocking calls
- **Error Handling**: Try-catch with proper async exception handling
- **Telemetry Integration**: OpenTelemetry Activity for observability

```csharp
public override async Task<IEnumerable<T>?> ExecuteQueryAsync(
    ContentItemQueryBuilder queryBuilder,
    ContentQueryExecutionOptions options,
    CancellationToken cancellationToken = default)
{
    using var activity = ActivitySource.StartActivity("ExecuteQuery");
    activity?.SetTag("contentType", typeof(T).Name);
    
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // Execute query
        var result = await base.ExecuteQueryAsync(queryBuilder, options, cancellationToken);
        
        // Process results if processors are registered
        if (_processors != null && result != null)
        {
            // Sequential processing with order enforcement
            var orderedProcessors = _processors.OrderBy(p => p.Order);
            foreach (var processor in orderedProcessors)
            {
                foreach (var item in result)
                {
                    await processor.ProcessAsync(item, cancellationToken);
                }
            }
        }
        
        stopwatch.Stop();
        
        // Update performance counters
        Interlocked.Increment(ref _totalExecutions);
        Interlocked.Add(ref _totalProcessingTime, stopwatch.ElapsedMilliseconds);
        
        // Telemetry tags
        activity?.SetTag("executionTimeMs", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("resultCount", result?.Count() ?? 0);
        
        return result;
    }
    catch (Exception ex)
    {
        activity?.SetTag("error", true);
        activity?.SetTag("errorMessage", ex.Message);
        throw;
    }
}
```

**Key Takeaways**:
- Always accept `CancellationToken` in async methods
- Use `using` statements for IDisposable resources (Activity)
- Track performance metrics with thread-safe operations (`Interlocked`)
- Integrate telemetry for production observability
- Properly propagate exceptions with context

---

## 7. Testing Best Practices

### Exemplar: `ContentItemContextTests`
**Location**: [tests/XperienceCommunity.DataContext.Tests/ContentItemContextTests.cs](../../tests/XperienceCommunity.DataContext.Tests/ContentItemContextTests.cs)

**Why it''s exemplary**:
- **AAA Pattern**: Arrange, Act, Assert clearly separated
- **Mocking with NSubstitute**: Clean, readable mocking syntax
- **Test Isolation**: Each test is independent
- **Descriptive Test Names**: Method names describe what is being tested

```csharp
public class ContentItemContextTests
{
    // Test-specific content type
    public class TestContentItem : IContentItemFieldsSource
    {
        public const string CONTENT_TYPE_NAME = "TestContentItem";
        public ContentItemFields SystemFields => new() { ContentItemID = 1 };
    }
    
    private readonly IWebsiteChannelContext _websiteChannelContext;
    private readonly IProgressiveCache _cache;
    private readonly ContentQueryExecutor<TestContentItem> _contentQueryExecutor;
    
    public ContentItemContextTests()
    {
        // Arrange: Set up mocks in constructor (shared across tests)
        _websiteChannelContext = Substitute.For<IWebsiteChannelContext>();
        _websiteChannelContext.WebsiteChannelID.Returns(123);
        
        _cache = Substitute.For<IProgressiveCache>();
        
        var logger = Substitute.For<ILogger<ContentQueryExecutor<TestContentItem>>>();
        var queryExecutor = Substitute.For<IContentQueryExecutor>();
        _contentQueryExecutor = new ContentQueryExecutor<TestContentItem>(logger, queryExecutor, null);
    }
    
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        // Act
        var context = new ContentItemContext<TestContentItem>(
            _websiteChannelContext,
            _cache,
            _contentQueryExecutor,
            new XperienceDataContextConfig()
        );
        
        // Assert
        Assert.NotNull(context);
    }
    
    [Fact]
    public void GetCacheKey_ShouldReturnExpectedFormat()
    {
        // Arrange
        var context = new ContentItemContext<TestContentItem>(/*...*/);
        var builder = new ContentItemQueryBuilder();
        
        // Use reflection to access private method for testing
        var method = typeof(ContentItemContext<TestContentItem>)
            .GetMethod("GetCacheKey", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var cacheKey = method?.Invoke(context, new object[] { builder }) as string;
        
        // Assert
        Assert.NotNull(cacheKey);
        Assert.Contains("data|", cacheKey);
        Assert.Contains("en-US", cacheKey);
    }
}
```

**Key Takeaways**:
- Follow AAA (Arrange, Act, Assert) pattern consistently
- Use descriptive test method names (e.g., `Method_Scenario_ExpectedOutcome`)
- Mock external dependencies with NSubstitute
- Test one behavior per test method
- Use reflection sparingly for testing internal/private methods

---

## 8. Exception Handling: Custom Exceptions

### Exemplar: `ExpressionProcessingException`
**Location**: [src/XperienceCommunity.DataContext/Exceptions/ExpressionProcessingException.cs](../../src/XperienceCommunity.DataContext/Exceptions/)

**Why it''s exemplary**:
- **Domain-Specific Exceptions**: Clear, specific exception types
- **Rich Context**: Includes expression details and diagnostic information
- **Serializable**: Proper serialization support
- **Inheritance**: Extends appropriate base exception type

```csharp
[Serializable]
public class ExpressionProcessingException : Exception
{
    public Expression? Expression { get; }
    public string? DiagnosticInfo { get; }
    
    public ExpressionProcessingException(
        string message, 
        Expression? expression = null,
        string? diagnosticInfo = null) 
        : base(message)
    {
        Expression = expression;
        DiagnosticInfo = diagnosticInfo;
    }
    
    public ExpressionProcessingException(
        string message, 
        Exception innerException,
        Expression? expression = null) 
        : base(message, innerException)
    {
        Expression = expression;
    }
    
    // Serialization constructor
    protected ExpressionProcessingException(
        SerializationInfo info, 
        StreamingContext context) 
        : base(info, context)
    {
        DiagnosticInfo = info.GetString(nameof(DiagnosticInfo));
    }
    
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(DiagnosticInfo), DiagnosticInfo);
    }
}
```

**Key Takeaways**:
- Create domain-specific exception types for better error handling
- Include relevant context in exception properties
- Provide multiple constructors for different scenarios
- Implement serialization for distributed scenarios

---

## 9. Diagnostics: DebuggerDisplay Attributes

### Exemplar: Multiple Classes
**Locations**: Throughout the codebase

**Why it''s exemplary**:
- **Enhanced Debugging**: Shows useful information in debugger without expanding
- **Performance**: No runtime cost (debug-only)
- **Consistency**: Applied uniformly across all major classes

```csharp
// BaseDataContext
[DebuggerDisplay("ContentType: {_contentType}, Language: {_language}, Parameters: {_parameters.Count}, HasQuery: {_query != null}, CacheTimeout: {_config.CacheTimeOut}min")]
public abstract class BaseDataContext<T, TExecutor> : IDataContext<T>

// ProcessorSupportedQueryExecutor
[DebuggerDisplay("ContentType: {typeof(T).Name}, Processors: {_processors?.Count ?? 0}, ActivitySource: {ActivitySource.Name}")]
public abstract class ProcessorSupportedQueryExecutor<T, TProcessor>

// BinaryExpressionProcessor
[DebuggerDisplay("Processor: Binary, Context: {_context.Parameters.Count} params, {_context.WhereActions.Count} actions")]
internal sealed class BinaryExpressionProcessor

// ExpressionContext
[DebuggerDisplay("Parameters: {Parameters.Count}, Members: {string.Join(\".\", _memberStack.Reverse())}, WhereActions: {WhereActions.Count}, Groupings: {_logicalGroupings.Count}")]
public sealed class ExpressionContext : IExpressionContext
```

**Key Takeaways**:
- Add `[DebuggerDisplay]` to all public classes and major internal classes
- Show key state information that helps with debugging
- Use interpolated strings for readability
- Include counts, flags, and identifiers

---

## 10. Performance: Concurrent Collections

### Exemplar: `ExpressionContext.Parameters`
**Location**: [src/XperienceCommunity.DataContext/Contexts/ExpressionContext.cs](../../src/XperienceCommunity.DataContext/Contexts/ExpressionContext.cs)

**Why it''s exemplary**:
- **Thread Safety**: Uses `ConcurrentDictionary` for parameter storage
- **Lock-Free Operations**: High-performance concurrent access
- **Appropriate Data Structure**: Right tool for the job

```csharp
public sealed class ExpressionContext : IExpressionContext
{
    private readonly ConcurrentDictionary<string, object?> _parameters = new();
    private readonly Stack<string> _memberStack = new();
    private readonly List<Action<ContentItemQueryBuilder>> _whereActions = new();
    
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;
    
    public void AddParameter(string name, object? value)
    {
        // Thread-safe add operation
        _parameters.TryAdd(name, value);
    }
}
```

**Key Takeaways**:
- Use `ConcurrentDictionary` for shared mutable state
- Expose read-only interfaces to external consumers
- Choose appropriate collection types (Stack for LIFO, List for ordered)
- Avoid unnecessary locks with concurrent collections

---

## Summary of Best Practices Demonstrated

1. **SOLID Principles**: Single responsibility, open/closed, dependency inversion
2. **Design Patterns**: Template Method, Visitor, Strategy, Factory, Builder, Decorator
3. **Async/Await**: Proper cancellation token usage, no blocking calls
4. **Dependency Injection**: Appropriate service lifetimes, extension methods
5. **Testing**: AAA pattern, mocking, test isolation
6. **Error Handling**: Domain-specific exceptions with context
7. **Performance**: Conditional compilation, concurrent collections, lock-free algorithms
8. **Diagnostics**: DebuggerDisplay, telemetry, performance counters
9. **Documentation**: XML doc comments on all public APIs
10. **Defensive Programming**: Validation, null checks, early returns

---

**Last Updated**: December 28, 2025
**Review Frequency**: Quarterly (update as patterns evolve)
