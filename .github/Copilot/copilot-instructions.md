# Copilot Instructions

## General C# Guidelines
- Target **.NET 8** and **.NET 9** with latest syntax
- Follow [Microsoft C# conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **nullable reference types**, **async/await**, **var**, and **dependency injection**
- Include XML documentation for public APIs
- Follow **SOLID** principles and clean architecture

## Testing
- Use **xUnit** with AAA pattern
- Avoid infrastructure dependencies and magic strings
- Name tests clearly, write minimally passing tests
- Mock external dependencies with **NSubstitute**

## Project Architecture

### Three-Context Library
This library extends Kentico Xperience with three specialized contexts:
- `IContentItemContext<T>` - Content hub items (`T : IContentItemFieldsSource`)
- `IPageContentContext<T>` - Web pages (`T : IWebPageFieldsSource`) 
- `IReusableSchemaContext<T>` - Reusable schemas (no constraints)

Access via unified factory: `IXperienceDataContext`

### Core Purpose: LINQ to Kentico Translation
- Translate LINQ expressions to Kentico's query format
- Use **ExpressionVisitor pattern** for expression tree processing
- Support standard operators: `==`, `!=`, `>`, `>=`, `<`, `<=`, `&&`, `||`, `!`
- Handle method calls: `Contains`, `StartsWith`, `EndsWith`
- Throw `NotSupportedException` for unsupported expressions

### Key Patterns
- **Base classes**: `BaseDataContext<T, TExecutor>` → Specialized contexts
- **Processors**: `ProcessorSupportedQueryExecutor<T, TProcessor>` for extensibility
- **Factory pattern**: `IXperienceDataContext` for creating contexts
- **Visitor pattern**: Expression tree traversal
- **Fluent API**: Method chaining for query building

### Kentico Integration
- Use `IContentQueryExecutor` for queries
- Leverage `IProgressiveCache` for caching
- Follow content type patterns: `IContentItemFieldsSource`, `IWebPageFieldsSource`, `IReusableFieldsSource`
- Register via DI: `services.AddXperienceDataContext()`

### Performance & Caching
- Cache with `IProgressiveCache` using content-aware keys
- Support pagination for large result sets
- Optimize expression tree traversal
- Use async methods with `CancellationToken` support

### Error Handling
- Use `Result<T>` patterns from CSharpFunctionalExtensions
- Log with `ILogger<T>`
- Provide clear error messages for unsupported LINQ expressions

## Code Generation Preferences
- Use **file-scoped namespaces**
- Prefer **primary constructors** for simple classes
- Use **collection expressions** `[]` over `new List<T>()`
- Always include appropriate **using statements**
- Generate complete, compilable code examples
- Follow existing project naming conventions and folder structure