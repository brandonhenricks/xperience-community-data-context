# XperienceCommunity.DataContext - AI Coding Agent Instructions

## Project Overview

This is a .NET library providing a fluent query API abstraction for Kentico Xperience by Kentico. It wraps the built-in `ContentItemQueryBuilder` with strongly-typed expressions, caching, and extensible processor pipelines.

## Architecture: Three-Context Pattern

The library implements three specialized contexts, each with distinct type constraints:

1. **`ContentItemContext<T>`** - Content hub items where `T : IContentItemFieldsSource`
2. **`PageContentContext<T>`** - Web pages where `T : IWebPageFieldsSource` 
3. **`ReusableSchemaContext<T>`** - Flexible schemas (no type constraint, supports interfaces)

Access all contexts via **`IXperienceDataContext`** factory methods:
- `ForContentType<T>()` → `IContentItemContext<T>`
- `ForPageContentType<T>()` → `IPageContentContext<T>`
- `ForReusableSchema<T>()` → `IReusableSchemaContext<T>`

**Key architectural pattern**: All three contexts inherit from `BaseDataContext<T, TExecutor>` which provides common query operations (async LINQ methods), caching, and parameter management. This base class reduced code duplication by 70%+.

## Expression Processing Pipeline

LINQ expressions are translated to Kentico's query builder through a visitor pattern:

1. **`ContentItemQueryExpressionVisitor`** walks the expression tree
2. Specialized processors handle expression types (see `src/XperienceCommunity.DataContext/Expressions/Processors/`):
   - `BinaryExpressionProcessor` - Equality, comparison operators
   - `LogicalExpressionProcessor` - AND/OR logic
   - `MethodCallExpressionProcessor` - String methods (Contains, StartsWith, etc.)
   - `EnhancedCollectionProcessor` - Collection operations (Any, Contains, etc.)
   - `RangeOptimizationProcessor` - Optimizes date ranges and comparisons
3. Each processor updates an `ExpressionContext` which tracks parameters, member paths, and WHERE clause fragments

**Critical**: When adding new expression support, create a processor in `Expressions/Processors/` and register it in the visitor's processor dictionaries.

## Custom Processors (Content Transformation)

Extend query execution with ordered processor chains:

```csharp
// Content items
public class BlogPostProcessor : IContentItemProcessor<BlogPost>
{
    public int Order => 1; // Lower = earlier execution
    public async Task ProcessAsync(BlogPost content, CancellationToken ct) { /* transform */ }
}

// Register in DI:
builder.Services.AddXperienceDataContext()
    .AddContentItemProcessor<BlogPost, BlogPostProcessor>()
    .SetCacheTimeout(30);
```

Processors registered via `XperienceContextBuilder` are automatically injected into `ProcessorSupportedQueryExecutor<T>` and executed after query results are retrieved.

## Testing Patterns

- Use **NSubstitute** for mocking Kentico dependencies (`IContentQueryExecutor`, `IProgressiveCache`, `IWebsiteChannelContext`)
- Test content types must implement required interfaces (`IContentItemFieldsSource`, `IWebPageFieldsSource`)
- See [ContentItemContextTests.cs](../tests/XperienceCommunity.DataContext.Tests/ContentItemContextTests.cs) for mock setup patterns
- Tests target both .NET 8 and .NET 9 via multi-targeting

## Dependency Injection Rules

**Prerequisites**: Kentico services MUST be registered before this library:
```csharp
builder.Services.AddKentico();
builder.Services.AddKenticoFeatures();
builder.Services.AddXperienceDataContext(cacheInMinutes: 30);
```

**Required Kentico dependencies** (auto-provided by `AddKentico()`):
- `IProgressiveCache` - Query result caching
- `IWebsiteChannelContext` - Channel/preview awareness
- `IContentQueryExecutor` - Query execution

All contexts are registered as **scoped** services. Configuration is **singleton**.

## Debugging & Diagnostics

Enable diagnostics globally or per-context:
```csharp
// Global
DataContextDiagnostics.DiagnosticsEnabled = true;

// Per-context fluent method
var results = await context
    .EnableDiagnostics(LogLevel.Debug)
    .ExecuteWithDiagnostics("QueryName", ctx => ctx.Where(...).ToListAsync());
```

All contexts have `[DebuggerDisplay]` attributes showing: ContentType, Language, Parameters count, HasQuery, CacheTimeout. See [Debugging-Guide.md](../docs/Debugging-Guide.md) for performance stats and diagnostic reports.

## Build & Test Commands

```bash
# Build (multi-targets net8.0 and net9.0)
dotnet build

# Run tests
dotnet test

# Generate test results
dotnet test --logger trx

# Create NuGet package
dotnet pack --configuration Release
```

Uses **Central Package Management** via `Directory.Packages.props`. Lock files are enabled for reproducible builds.

## Code Conventions

- **Implicit usings enabled** - Common namespaces auto-imported (System, Linq, Collections, etc.)
- **Nullable reference types** - Required for all new code
- **Async/await** - All I/O operations must be async with `CancellationToken` support
- **One class per file** except small related interfaces
- **Folder structure**: Abstractions/ (interfaces), Contexts/ (implementations), Core/ (base classes), Extensions/ (extension methods)
- **Extension method naming**: Suffix static classes with `Extensions` (e.g., `LoggingExtensions`, `DebuggingExtensions`)

## Key Files to Reference

- [DependencyInjection.cs](../src/XperienceCommunity.DataContext/DependencyInjection.cs) - Service registration patterns
- [BaseDataContext.cs](../src/XperienceCommunity.DataContext/Core/BaseDataContext.cs) - Common query operations, caching logic
- [ContentItemQueryExpressionVisitor.cs](../src/XperienceCommunity.DataContext/Expressions/Visitors/ContentItemQueryExpressionVisitor.cs) - Expression translation entry point
- [XperienceContextBuilder.cs](../src/XperienceCommunity.DataContext/Configurations/XperienceContextBuilder.cs) - Fluent configuration API
