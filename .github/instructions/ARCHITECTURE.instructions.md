# Architecture

## Overview

XperienceCommunity.DataContext is a fluent query API library for Kentico Xperience by Kentico that abstracts the built-in `ContentItemQueryBuilder` with strongly-typed expressions, caching, and extensible processor pipelines.

## Core Architectural Pattern: Three-Context Separation

The library implements **three specialized contexts**, each with distinct type constraints optimized for different content scenarios:

### 1. Content Item Context (`ContentItemContext<T>`)
- **Purpose**: Query content hub items with strongly-typed expressions
- **Type Constraint**: `T : IContentItemFieldsSource`
- **Use Cases**: Content items, reusable content blocks, structured data
- **Features**: Full LINQ support, caching, linked items, expression-based filtering

### 2. Page Content Context (`PageContentContext<T>`)
- **Purpose**: Query web pages with channel and path filtering
- **Type Constraint**: `T : IWebPageFieldsSource`
- **Use Cases**: Website pages, routing, channel-specific content
- **Features**: Channel filtering, path-based queries, page hierarchy, web-specific metadata

### 3. Reusable Schema Context (`ReusableSchemaContext<T>`)
- **Purpose**: Query reusable schemas with maximum flexibility
- **Type Constraint**: None (supports both classes and interfaces)
- **Use Cases**: Shared content schemas, interface-based content models, flexible data structures
- **Features**: Interface support, schema flexibility, reusable patterns

### Unified Access via `IXperienceDataContext`
All three contexts are accessible through a centralized factory interface:
```csharp
public interface IXperienceDataContext
{
    IContentItemContext<T> ForContentType<T>() where T : IContentItemFieldsSource;
    IPageContentContext<T> ForPageContentType<T>() where T : IWebPageFieldsSource;
    IReusableSchemaContext<T> ForReusableSchema<T>() where T : class, new();
}
```

**Benefits**:
- Single dependency injection point
- Consistent API across all content types
- Easier testing and mocking
- Reduced service registration complexity

---

## Layered Architecture

### Layer 1: Abstractions
**Location**: `src/XperienceCommunity.DataContext/Abstractions/`

**Responsibilities**:
- Define contracts for data contexts (`IDataContext<T>`, `IContentItemContext<T>`, etc.)
- Define processor interfaces (`IProcessor<T>`, `IContentItemProcessor<T>`, `IPageContentProcessor<T>`)
- Define expression processing contracts (`IExpressionContext`, `IExpressionProcessor`)

**Key Interfaces**:
- `IDataContext<T>`: Base interface for all context types with async LINQ methods
- `IXperienceDataContext`: Factory for creating specialized contexts
- `IProcessor<T>`: Base interface for content transformation pipelines
- `IExpressionContext`: Manages expression translation state

### Layer 2: Core Base Classes
**Location**: `src/XperienceCommunity.DataContext/Core/`

**Responsibilities**:
- Provide common query operations (reduced code duplication by 70%+)
- Implement caching logic with cache dependency management
- Manage query execution and parameter tracking
- Support processor pipelines with telemetry

**Key Classes**:
- **`BaseDataContext<T, TExecutor>`**: Abstract base for all context implementations
  - Provides: `Where`, `OrderBy`, `Take`, `ToListAsync`, `FirstOrDefaultAsync`, etc.
  - Manages: Caching, parameters, query building, language/channel awareness
  - **Architectural Impact**: Eliminated 500+ lines of duplicate code across contexts

- **`BaseContentQueryExecutor<T>`**: Abstract base for query executors
  - Handles content query execution
  - Integrates with Kentico's `IContentQueryExecutor`

- **`ProcessorSupportedQueryExecutor<T, TProcessor>`**: Executor with processor support
  - Executes ordered processor chains
  - Provides telemetry via `ActivitySource`
  - Tracks performance counters (execution count, average time)

### Layer 3: Context Implementations
**Location**: `src/XperienceCommunity.DataContext/Contexts/`

**Responsibilities**:
- Implement specialized query building for each content type
- Configure content-specific query options
- Generate appropriate cache keys and dependencies

**Key Classes**:
- **`ContentItemContext<T>`**: Content hub item queries
- **`PageContentContext<T>`**: Web page queries with path/channel filtering
- **`ReusableSchemaContext<T>`**: Flexible schema queries
- **`XperienceDataContext`**: Factory implementation for creating contexts
- **`ExpressionContext`**: Manages expression translation state (parameters, member paths, WHERE clauses)

### Layer 4: Expression Processing Pipeline
**Location**: `src/XperienceCommunity.DataContext/Expressions/`

**Responsibilities**:
- Translate LINQ expressions to Kentico query builder operations
- Handle complex expression patterns (binary, logical, method calls, collections)
- Optimize query generation (range optimization, collection operations)

**Architecture Pattern**: Visitor Pattern + Processor Chain

```
Expression Tree → ContentItemQueryExpressionVisitor → Processors → ContentItemQueryBuilder
```

**Key Components**:

1. **Visitor** (`ContentItemQueryExpressionVisitor`):
   - Entry point for expression processing
   - Routes expression nodes to appropriate processors
   - Maintains processor dictionaries by expression type

2. **Processors** (`Expressions/Processors/`):
   - **`BinaryExpressionProcessor`**: Equality, comparison operators
   - **`LogicalExpressionProcessor`**: AND/OR logic with grouping
   - **`MethodCallExpressionProcessor`**: String methods (Contains, StartsWith, etc.)
   - **`EnhancedCollectionProcessor`**: Collection operations (Any, Contains, All)
   - **`RangeOptimizationProcessor`**: Date/numeric range optimization
   - **`NegatedExpressionProcessor`**: NOT operations
   - **Enhanced processors**: Specialized optimization for common patterns

3. **Expression Context** (`ExpressionContext`):
   - Tracks query parameters
   - Manages member path stack (e.g., "User.Name.FirstName")
   - Accumulates WHERE clause fragments
   - Handles logical grouping (AND/OR nesting)

**Extensibility Point**: Add new expression support by:
1. Creating a processor in `Expressions/Processors/`
2. Implementing `IExpressionProcessor<TExpression>`
3. Registering in visitor's processor dictionary

### Layer 5: Custom Content Processors
**Location**: User-defined, registered via DI

**Responsibilities**:
- Transform query results post-retrieval
- Enrich content with additional data
- Apply business logic to content items

**Pattern**: Ordered processor chain
```csharp
public class BlogPostProcessor : IContentItemProcessor<BlogPost>
{
    public int Order => 1; // Lower = earlier execution
    
    public async Task ProcessAsync(BlogPost content, CancellationToken ct)
    {
        // Transform content
    }
}
```

**Registration**:
```csharp
builder.Services.AddXperienceDataContext()
    .AddContentItemProcessor<BlogPost, BlogPostProcessor>()
    .AddPageContentProcessor<WebPage, WebPageProcessor>()
    .SetCacheTimeout(30);
```

### Layer 6: Diagnostics & Telemetry
**Location**: `src/XperienceCommunity.DataContext/Diagnostics/`

**Responsibilities**:
- Provide debugging information via `DebuggerDisplay` attributes
- Track performance metrics (execution count, average time)
- Generate diagnostic reports
- Integrate with OpenTelemetry/Activity API

**Key Classes**:
- **`DataContextDiagnostics`**: Static diagnostics manager
  - Global enable/disable
  - Performance statistics
  - Diagnostic report generation
- **`DiagnosticEntry`**: Timestamped diagnostic event
- **Extension Methods**: `EnableDiagnostics`, `ExecuteWithDiagnostics`, `ToDebugString`

---

## Data Flow

### Query Execution Flow
```
User Code (LINQ Expression)
    ↓
IDataContext<T>.Where/OrderBy/Take
    ↓
BaseDataContext<T>.BuildQuery (abstract)
    ↓
ContentItemQueryExpressionVisitor.Visit
    ↓
Expression Processors (Binary, Logical, MethodCall, etc.)
    ↓
ExpressionContext (accumulate WHERE, parameters)
    ↓
ContentItemQueryBuilder (Kentico API)
    ↓
GetOrCacheAsync (IProgressiveCache)
    ↓
ProcessorSupportedQueryExecutor.ExecuteQueryAsync
    ↓
IContentQueryExecutor (Kentico)
    ↓
Custom Processors (ordered chain)
    ↓
Results to User
```

### Caching Flow
```
Query Request
    ↓
Check Preview Mode? → Yes → Bypass Cache
    ↓ No
Generate Cache Key (content type, language, channel, parameters, query hash)
    ↓
IProgressiveCache.LoadAsync
    ↓
Cache Hit? → Yes → Return Cached
    ↓ No
Execute Query
    ↓
Generate Cache Dependencies (contentitem|byid|{id})
    ↓
Store in Cache
    ↓
Return Results
```

---

## Architectural Patterns & Principles

### 1. **Visitor Pattern**
- **Usage**: Expression tree traversal (`ContentItemQueryExpressionVisitor`)
- **Benefit**: Separates expression structure from processing logic
- **Extensibility**: Add new expression types without modifying existing code

### 2. **Strategy Pattern**
- **Usage**: Expression processors (one per expression type)
- **Benefit**: Encapsulates algorithm variations
- **Extensibility**: Plug in new processors for new expression types

### 3. **Template Method Pattern**
- **Usage**: `BaseDataContext<T, TExecutor>` defines query workflow
- **Benefit**: Code reuse across all context types
- **Customization**: Subclasses override `BuildQuery`, `GetCacheKey`

### 4. **Factory Pattern**
- **Usage**: `IXperienceDataContext` creates specialized contexts
- **Benefit**: Centralized creation logic, easier testing
- **Consistency**: Single API for all content types

### 5. **Decorator Pattern**
- **Usage**: Custom processor chains wrap query execution
- **Benefit**: Add behavior without modifying executors
- **Extensibility**: Unlimited processors per content type

### 6. **Dependency Injection**
- **Scope**: All contexts are scoped services
- **Required Dependencies**: `IProgressiveCache`, `IWebsiteChannelContext`, `IContentQueryExecutor`
- **Configuration**: `XperienceDataContextConfig` registered as singleton

---

## Architectural Risks & Considerations

### Identified Risks

1. **Expression Complexity Growth**
   - **Risk**: As more LINQ methods are supported, processor complexity increases
   - **Mitigation**: Enhanced processors for common patterns, clear separation of concerns
   - **Status**: Manageable with current processor architecture

2. **Cache Key Collisions**
   - **Risk**: Complex queries may generate identical cache keys
   - **Mitigation**: Include query hash, parameters, language, channel in key
   - **Status**: Low risk with current implementation

3. **Performance with Deep Linked Items**
   - **Risk**: `WithLinkedItems(depth)` can cause exponential data retrieval
   - **Mitigation**: Document best practices, default to depth 0
   - **Status**: User responsibility, documented

4. **Processor Order Dependencies**
   - **Risk**: Processors with incorrect `Order` values may break transformations
   - **Mitigation**: Document ordering conventions, use ordered execution
   - **Status**: Requires developer discipline

5. **Memory Usage in Diagnostics**
   - **Risk**: Enabled diagnostics accumulate unbounded entries
   - **Mitigation**: Maximum 1000 entries, auto-removal of oldest
   - **Status**: Handled, but should not be enabled in production

### Anti-Patterns to Avoid

1. **❌ Calling `.ToList()` before filtering**
   ```csharp
   // Bad - loads all items before filtering
   var allItems = await context.ToListAsync();
   var filtered = allItems.Where(x => x.IsPublished);
   ```

2. **❌ Not using `CancellationToken`**
   ```csharp
   // Bad - cannot cancel long-running queries
   var results = await context.ToListAsync();
   
   // Good
   var results = await context.ToListAsync(cancellationToken);
   ```

3. **❌ Creating contexts directly without factory**
   ```csharp
   // Bad - bypasses DI, requires manual dependency management
   var context = new ContentItemContext<T>(...);
   
   // Good - use factory
   var context = dataContext.ForContentType<T>();
   ```

4. **❌ Ignoring cache timeout configuration**
   ```csharp
   // Bad - uses default cache timeout for all scenarios
   builder.Services.AddXperienceDataContext(cacheInMinutes: 60);
   
   // Good - configure appropriately for content volatility
   builder.Services.AddXperienceDataContext(cacheInMinutes: 5); // Frequently changing
   ```

---

## Improvement Opportunities

### High Priority

1. **Query Plan Caching**
   - **Opportunity**: Cache translated query expressions, not just results
   - **Benefit**: Reduce expression processing overhead for repeated queries
   - **Complexity**: Medium (requires expression caching strategy)

2. **Async Processor Parallelization**
   - **Opportunity**: Execute independent processors in parallel
   - **Benefit**: Faster content enrichment for multi-processor scenarios
   - **Complexity**: Low (use `Task.WhenAll`)

3. **Query Hints/Options API**
   - **Opportunity**: Add fluent methods for query optimization hints
   - **Benefit**: Allow users to optimize specific queries
   - **Complexity**: Low
   ```csharp
   context.Where(x => x.Title == "Test")
       .WithHint(QueryHint.NoCache)
       .WithHint(QueryHint.PreferLatestVersion)
       .ToListAsync();
   ```

### Medium Priority

4. **GraphQL-Style Projection**
   - **Opportunity**: Add field selection to reduce data transfer
   - **Benefit**: Optimize bandwidth for large content items
   - **Complexity**: High (requires projection mapping)

5. **Batch Query API**
   - **Opportunity**: Execute multiple queries in a single database round-trip
   - **Benefit**: Reduce latency for dashboard/summary pages
   - **Complexity**: Medium

6. **Query Result Streaming**
   - **Opportunity**: Return `IAsyncEnumerable<T>` for large result sets
   - **Benefit**: Memory efficiency for batch processing
   - **Complexity**: Low

### Low Priority

7. **Expression Tree Optimization**
   - **Opportunity**: Simplify/rewrite expression trees before translation
   - **Benefit**: More efficient queries
   - **Complexity**: High (requires expression tree rewriting)

---

## Technology Constraints

- **Kentico Xperience Dependency**: Tightly coupled to Kentico''s content query API (by design)
- **.NET Version**: Requires .NET 8 or .NET 9 (multi-targeted)
- **Expression Support**: Limited to expressions translatable to `ContentItemQueryBuilder`
- **Caching**: Depends on Kentico''s `IProgressiveCache` implementation

---

## Testing Architecture

- **Unit Tests**: Mock Kentico dependencies (`IContentQueryExecutor`, `IProgressiveCache`, `IWebsiteChannelContext`)
- **Test Framework**: xUnit with NSubstitute for mocking
- **Multi-Targeting**: Tests run against both .NET 8 and .NET 9
- **Coverage**: Focus on expression processors, context operations, cache behavior

---

## Future Architectural Considerations

1. **Plugin System**: Allow third-party expression processors
2. **Multi-Database Support**: Abstract Kentico dependency for other CMSs (breaking change)
3. **Code Generation**: Generate context types from Kentico content models
4. **Query Analyzer**: Static analysis tool for query optimization suggestions

---

**Last Updated**: December 28, 2025
**Architectural Review**: Recommended annually
