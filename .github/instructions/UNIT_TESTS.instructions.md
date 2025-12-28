# Unit Tests

This document describes the unit testing strategy, patterns, tools, and coverage goals for the XperienceCommunity.DataContext library.

---

## Testing Framework

### Primary Tools
- **Test Framework**: xUnit 2.8.1
- **Mocking**: NSubstitute 5.3.0
- **Code Coverage**: coverlet.collector 6.0.2
- **Test SDK**: Microsoft.NET.Test.Sdk 17.10.0

### Target Framework
- **Test Projects**: .NET 9.0 only (single target for simplicity)
- **Source Projects**: Multi-targeted (.NET 8.0 + .NET 9.0)

---

## Test Project Structure

```
tests/
└── XperienceCommunity.DataContext.Tests/
    ├── ProcessorTests/              # Expression processor tests
    │   ├── BinaryExpressionProcessorTests.cs
    │   ├── ComparisonExpressionProcessorTests.cs
    │   ├── EnhancedCollectionProcessorTests.cs
    │   ├── EnhancedMethodCallExpressionTests.cs
    │   ├── EnhancedStringProcessorTests.cs
    │   ├── EqualityExpressionProcessorTests.cs
    │   ├── LogicalExpressionProcessorIntegrationTests.cs
    │   ├── LogicalExpressionProcessorTests.cs
    │   ├── MethodCallExpressionProcessorEdgeCasesTests.cs
    │   ├── MethodCallExpressionProcessorTests.cs
    │   ├── NegatedExpressionProcessorTests.cs
    │   ├── RangeOptimizationProcessorTests.cs
    │   └── UnaryExpressionProcessorTests.cs
    │
    ├── ContentItemContextTests.cs           # Content context tests
    ├── ContentItemQueryExpressionVisitorTests.cs  # Visitor tests
    ├── ContentQueryExecutorTests.cs         # Executor tests
    ├── ExpressionContextTests.cs            # Expression context state tests
    ├── PageContentQueryExecutorTests.cs     # Page executor tests
    ├── ProcessorSupportedQueryExecutorTests.cs  # Processor executor tests
    ├── ReusableSchemaExecutorTests.cs       # Schema executor tests
    ├── XperienceContextBuilderTests.cs      # Builder tests
    └── XperienceDataContextTests.cs         # Factory tests
```

**Organization Principles**:
- One test file per class (e.g., `ContentItemContext.cs` → `ContentItemContextTests.cs`)
- Processor tests isolated in `ProcessorTests/` subfolder (13 files)
- Test names follow: `ClassName` + `Tests.cs`

---

## Test Naming Convention

### Test Method Naming: `Method_Scenario_ExpectedOutcome`

```csharp
// ✅ Good examples
[Fact]
public void Constructor_WhenCacheIsNull_ThrowsArgumentNullException()

[Fact]
public void GetCacheKey_WithValidParameters_ReturnsExpectedFormat()

[Fact]
public void ToListAsync_WithPreviewModeEnabled_BypassesCache()

[Theory]
[InlineData("test", true)]
[InlineData("", false)]
public void Where_WithStringComparison_GeneratesCorrectQuery(string value, bool expected)

// ❌ Bad examples
[Fact]
public void Test1() // Not descriptive

[Fact]
public void TestConstructor() // Missing scenario and outcome

[Fact]
public void ItShouldWork() // Vague
```

---

## AAA Pattern (Arrange, Act, Assert)

All tests follow the AAA pattern with clear separation:

```csharp
[Fact]
public void ToListAsync_WithValidQuery_ReturnsResults()
{
    // Arrange - Set up test data and dependencies
    var websiteChannelContext = Substitute.For<IWebsiteChannelContext>();
    websiteChannelContext.WebsiteChannelID.Returns(123);
    websiteChannelContext.IsPreview.Returns(false);
    
    var cache = Substitute.For<IProgressiveCache>();
    var executor = CreateMockExecutor();
    var config = new XperienceDataContextConfig { CacheTimeOut = 30 };
    
    var context = new ContentItemContext<TestContentItem>(
        websiteChannelContext, cache, executor, config);
    
    var expectedItems = new[] { new TestContentItem(), new TestContentItem() };
    
    // Act - Execute the method being tested
    var result = await context.ToListAsync();
    
    // Assert - Verify the outcome
    Assert.NotNull(result);
    Assert.Equal(2, result.Count());
}
```

---

## Mocking Strategy

### NSubstitute Patterns

```csharp
// Mock interfaces
var cache = Substitute.For<IProgressiveCache>();
var logger = Substitute.For<ILogger<ContentQueryExecutor<T>>>();
var queryExecutor = Substitute.For<IContentQueryExecutor>();

// Set up return values
websiteChannelContext.WebsiteChannelID.Returns(123);
websiteChannelContext.IsPreview.Returns(false);

// Mock async methods
cache.LoadAsync(Arg.Any<Func<CacheSettings, Task<T>>>(), Arg.Any<CacheSettings>())
    .Returns(Task.FromResult(expectedData));

// Verify method calls
await service.ExecuteAsync();
await cache.Received(1).LoadAsync(Arg.Any<Func>(), Arg.Any<CacheSettings>());

// Verify specific arguments
queryExecutor.Received().GetResult(
    Arg.Is<ContentItemQueryBuilder>(b => b.Parameters.Contains("expectedParam")),
    Arg.Any<ContentQueryExecutionOptions>(),
    Arg.Any<CancellationToken>());
```

### Test Content Types

Each test file defines simple test content types:

```csharp
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
```

**Benefits**:
- No dependency on real Kentico content types
- Fast test execution
- Clear test isolation

---

## Test Categories & Focus Areas

### 1. Context Tests
**Files**: `ContentItemContextTests.cs`, `PageContentQueryExecutorTests.cs`, etc.

**Coverage**:
- Constructor validation (null checks, initialization)
- Query building (`Where`, `OrderBy`, `Take`, etc.)
- Cache key generation
- Cache behavior (hit/miss, preview mode bypass)
- Async operations with cancellation tokens

```csharp
[Fact]
public void Constructor_ShouldInitialize()
{
    var context = new ContentItemContext<TestContentItem>(
        _websiteChannelContext, _cache, _contentQueryExecutor, _config);
    
    Assert.NotNull(context);
}

[Fact]
public void Constructor_WhenCacheIsNull_ThrowsArgumentNullException()
{
    var exception = Assert.Throws<ArgumentNullException>(() => 
        new ContentItemContext<TestContentItem>(
            _websiteChannelContext, null!, _contentQueryExecutor, _config));
    
    Assert.Equal("cache", exception.ParamName);
}
```

### 2. Expression Processor Tests
**Location**: `ProcessorTests/` folder (13 test files)

**Coverage**:
- Binary expressions (equality, comparison, arithmetic)
- Logical expressions (AND, OR, NOT)
- Method calls (Contains, StartsWith, EndsWith, Any, All, etc.)
- Collection operations (enhanced collection processor)
- String operations (enhanced string processor)
- Range optimization (x > 5 && x < 10 → BETWEEN)
- Edge cases (null handling, empty collections, invalid expressions)

```csharp
[Fact]
public void Process_EqualityExpression_AddsWhereClause()
{
    // Arrange
    var context = new ExpressionContext();
    var processor = new EqualityExpressionProcessor(context);
    Expression<Func<TestItem, bool>> expression = x => x.Name == "Test";
    var binaryExpr = (BinaryExpression)expression.Body;
    
    // Act
    processor.Process(binaryExpr);
    
    // Assert
    Assert.Single(context.WhereActions);
    Assert.Single(context.Parameters);
    Assert.Equal("Test", context.Parameters["p0"]);
}

[Theory]
[InlineData("StartsWith", "test", "test%")]
[InlineData("EndsWith", "test", "%test")]
[InlineData("Contains", "test", "%test%")]
public void Process_StringMethod_GeneratesLikeQuery(
    string methodName, string value, string expectedPattern)
{
    // Test string method translation
}
```

### 3. Query Executor Tests
**Files**: `ContentQueryExecutorTests.cs`, `ProcessorSupportedQueryExecutorTests.cs`

**Coverage**:
- Query execution flow
- Processor chain invocation (ordered by `Order` property)
- Error handling and logging
- Performance counter tracking
- Activity/telemetry integration
- Cancellation token propagation

```csharp
[Fact]
public async Task ExecuteQueryAsync_WithProcessors_InvokesInOrder()
{
    // Arrange
    var processor1 = Substitute.For<IContentItemProcessor<TestItem>>();
    processor1.Order.Returns(1);
    
    var processor2 = Substitute.For<IContentItemProcessor<TestItem>>();
    processor2.Order.Returns(2);
    
    var processors = new[] { processor2, processor1 }; // Out of order intentionally
    var executor = new ContentQueryExecutor<TestItem>(logger, queryExecutor, processors);
    
    // Act
    await executor.ExecuteQueryAsync(queryBuilder, options, CancellationToken.None);
    
    // Assert - Processors called in order
    Received.InOrder(() =>
    {
        processor1.ProcessAsync(Arg.Any<TestItem>(), Arg.Any<CancellationToken>());
        processor2.ProcessAsync(Arg.Any<TestItem>(), Arg.Any<CancellationToken>());
    });
}
```

### 4. Configuration & Builder Tests
**Files**: `XperienceContextBuilderTests.cs`, `XperienceDataContextTests.cs`

**Coverage**:
- Fluent builder API
- Service registration
- Configuration validation
- Cache timeout settings
- Processor registration

```csharp
[Fact]
public void AddContentItemProcessor_RegistersProcessor()
{
    // Arrange
    var services = new ServiceCollection();
    
    // Act
    services.AddXperienceDataContext()
        .AddContentItemProcessor<TestItem, TestProcessor>();
    
    // Assert
    var provider = services.BuildServiceProvider();
    var processor = provider.GetService<IContentItemProcessor<TestItem>>();
    Assert.NotNull(processor);
    Assert.IsType<TestProcessor>(processor);
}
```

---

## Coverage Goals

### Current Coverage
- **Overall**: ~75% code coverage
- **Critical Paths**: ~90% (contexts, executors, processors)
- **Edge Cases**: ~60% (error handling, null scenarios)

### Target Coverage
- **Goal**: 80% overall coverage
- **Critical Components**: 95%+ (core business logic)
- **Utilities/Extensions**: 70%+ (less critical)

### Excluded from Coverage
- Generated code (Roslyn source generators, if added)
- Diagnostic/debugging code (`[Conditional("DEBUG")]`)
- Obsolete/deprecated methods

---

## Running Tests

### Command Line
```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~ContentItemContextTests.Constructor_ShouldInitialize"

# Run tests in category
dotnet test --filter "Category=Processor"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Visual Studio
- Test Explorer: View → Test Explorer
- Run All: Ctrl+R, A
- Run Selected: Ctrl+R, T
- Debug Test: Ctrl+R, Ctrl+T

### VS Code
- Install: "C# Dev Kit" extension
- Run tests via Test Explorer sidebar
- Debug tests: Breakpoint + Debug Test

---

## CI/CD Integration

### GitHub Actions Workflow
```yaml
# .github/workflows/dotnet.yml
- name: Test Solution
  run: |
    dotnet test \
      --configuration Release \
      --no-build \
      --no-restore \
      --verbosity normal \
      --logger trx \
      --results-directory TestResults

- name: Upload test results
  uses: actions/upload-artifact@v4
  if: always()
  with:
    name: test-results
    path: TestResults/*.trx
```

**Features**:
- Tests run on every push to `master`
- Tests run on all pull requests
- Test results uploaded as artifacts (viewable in GitHub)
- Build fails if any test fails

---

## Test Data Management

### Test Fixtures
```csharp
public class ContentItemContextTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    
    public ContentItemContextTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }
}

public class TestDatabaseFixture : IDisposable
{
    public TestDatabaseFixture()
    {
        // Set up shared test data
    }
    
    public void Dispose()
    {
        // Clean up
    }
}
```

**Note**: Current implementation does NOT use fixtures (tests are fully isolated with mocks).

### Test Data Builders
```csharp
public class TestContentItemBuilder
{
    private int _id = 1;
    private string _name = "Test";
    
    public TestContentItemBuilder WithId(int id)
    {
        _id = id;
        return this;
    }
    
    public TestContentItemBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public TestContentItem Build() => new TestContentItem { Id = _id, Name = _name };
}

// Usage
var item = new TestContentItemBuilder()
    .WithId(123)
    .WithName("Custom Name")
    .Build();
```

---

## Testing Best Practices

### Do's ✅
1. **Follow AAA Pattern**: Arrange, Act, Assert clearly separated
2. **One Assert Per Test**: Focus on a single behavior
3. **Descriptive Names**: `Method_Scenario_ExpectedOutcome`
4. **Mock External Dependencies**: Use NSubstitute for all interfaces
5. **Test Edge Cases**: Null, empty, boundary values
6. **Use CancellationToken**: Test async cancellation scenarios
7. **Clean Up Resources**: Dispose of contexts, connections, etc.
8. **Test Exception Handling**: Verify exceptions are thrown/handled correctly

### Don'ts ❌
1. **Don't Test Framework Code**: Trust that xUnit, NSubstitute work
2. **Don't Test Private Methods Directly**: Test via public API
3. **Don't Use Real Database**: Always mock data access
4. **Don't Share State Between Tests**: Each test should be independent
5. **Don't Hardcode Values**: Use constants or test data builders
6. **Don't Ignore Warnings**: Fix or suppress with justification
7. **Don't Skip Async/Await**: Use proper async test patterns

---

## Coverage Gaps & Recommendations

### Current Gaps
1. **Integration Tests**: No end-to-end tests with real Kentico instance
2. **Performance Tests**: No benchmarking or load testing
3. **Concurrency Tests**: Limited testing of thread safety
4. **Error Logging Tests**: Verification of diagnostic output

### Recommended Additions
1. **Integration Test Suite**: 
   - Spin up Kentico test instance
   - Execute real queries against Kentico database
   - Validate cache behavior end-to-end

2. **Benchmark Tests**:
   - Use BenchmarkDotNet
   - Measure expression translation overhead
   - Track cache hit/miss performance

3. **Concurrency Tests**:
   - Parallel query execution
   - Thread-safe cache access
   - CancellationToken handling under load

4. **Property-Based Tests**:
   - Use FsCheck or similar
   - Generate random expressions
   - Verify invariants hold

---

## Test Maintenance

### When to Update Tests
- When adding new features (TDD: write tests first)
- When fixing bugs (add regression test)
- When refactoring (ensure tests still pass)
- When deprecating features (mark tests as obsolete or remove)

### Test Review Checklist
- [ ] All public methods have corresponding tests
- [ ] Edge cases are covered (null, empty, boundary)
- [ ] Error paths are tested (exceptions thrown)
- [ ] Async methods use proper async/await patterns
- [ ] CancellationToken scenarios are tested
- [ ] Test names follow convention
- [ ] AAA pattern is followed
- [ ] No hardcoded "magic" values
- [ ] Mocks are used for external dependencies
- [ ] Tests are independent (no shared state)

---

**Last Updated**: December 28, 2025
**Test Count**: 100+ unit tests
**Coverage Target**: 80% (Current: ~75%)
**Review Frequency**: With each PR + quarterly audit
