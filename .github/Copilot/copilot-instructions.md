# Copilot Instructions

- Use latest syntax compatible with **.NET 8** and **.NET 9** (project targets both frameworks).
- Adhere to [Microsoft's coding conventions for C#](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- Include XML documentation comments for all public classes, methods, and properties.
- Use **PascalCase** for type and method names, **camelCase** for local variables and parameters.
- Optimize code for readability, maintainability, and performance.
- Follow **SOLID** principles and clean architecture patterns.
- Use **nullable reference types** consistently (project has nullable enabled).
- Prefer full method bodies over inline lambdas in C#.
- Prefer async and await over synchronous code. [Async Guidance](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md)
- Prefer var over explicit types.
- Employ dependency injection using the built-in DI container.
- Handle exceptions gracefully and use `Microsoft.Extensions.Logging` for logging.
- Follow HttpClient Best Guidance [HttpClient Guidance](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/HttpClientGuidance.md)

## Testing Guidelines
- Use the AAA pattern (Arrange, Act, Assert)
- Avoid infrastructure dependencies.
- Name tests clearly.
- Write minimally passing tests.
- Avoid magic strings.
- Avoid logic in tests.
- Prefer helper methods for setup and teardown.
- Avoid multiple acts in a single test.
- Write unit tests using **xUnit** and aim for high code coverage.

## Project-specific Instructions

### Kentico Xperience Integration
- This is a **library project** that extends Kentico Xperience functionality, not a web application.
- Utilize **Kentico Xperience by Kentico** APIs for all CMS interactions.
- Follow Kentico's content type patterns using `IContentItemFieldsSource`, `IWebPageFieldsSource`, and `IReusableSchemaContext`.
- Use `IContentQueryExecutor` for database queries rather than direct database access.
- Leverage `IProgressiveCache` for caching strategies.
- Implement the **fluent API pattern** consistently across all query builders.

### Expression Tree Processing & LINQ Translation
- The **core purpose** of this library is to translate LINQ expressions into Kentico's query format.
- Use the **ExpressionVisitor pattern** to traverse and process expression trees systematically.
- Implement **specialized expression processors** for different expression types:
  - `BinaryExpression` for equality, comparison, and logical operations
  - `MethodCallExpression` for method calls like Contains, StartsWith, EndsWith
  - `UnaryExpression` for negation and type conversion
  - `MemberExpression` for property access
  - `ConstantExpression` for literal values
- Follow the **visitor pattern** by extending `System.Linq.Expressions.ExpressionVisitor`.
- Use **processor dictionaries** to map expression types to their corresponding processors.
- Maintain **expression context** during traversal (current member names, values, etc.).

### Expression Processor Design
- Create **typed processors** implementing `IExpressionProcessor<T>` for different expression types.
- Use **QueryParameterManager** to coordinate parameter generation and query building.
- Support **all standard LINQ operators**: `==`, `!=`, `>`, `>=`, `<`, `<=`, `&&`, `||`, `!`.
- Handle **method call expressions** for string operations and collection queries.
- Implement **proper parameter binding** to prevent SQL injection.
- Throw **NotSupportedException** for unsupported expression types with clear error messages.

### LINQ Expression Guidelines
- Support **strongly-typed lambda expressions** like `x => x.SystemFields.ContentItemGUID == guid`.
- Handle **complex expression trees** with nested logical operations.
- Process **member access chains** correctly (e.g., `x.SystemFields.ContentItemName`).
- Support **method chaining** in LINQ expressions.
- Validate **expression compatibility** with Kentico's query capabilities.
- Provide **clear error messages** when expressions cannot be translated.

### Architecture Patterns
- Use **generic constraints** to ensure type safety (e.g., `where T : class, IContentItemFieldsSource, new()`).
- Implement **processor pattern** for extending functionality (`IProcessor`, `IContentItemProcessor<T>`, `IPageContentProcessor<T>`).
- Use **factory pattern** through `IXperienceDataContext` for creating context instances.
- Maintain **separation of concerns** between content items, page content, and reusable schemas.
- Apply **visitor pattern** for expression tree traversal and processing.

### Library Design Principles
- All public APIs should be **async-first** with cancellation token support.
- Use **strongly-typed** interfaces over generic object types.
- Implement **fluent method chaining** for query building.
- Support **dependency injection** through extension methods (`services.AddXperienceDataContext()`).
- Use **CSharpFunctionalExtensions** for functional programming patterns when appropriate.
- Design **extensible processor architecture** for adding new expression types.

### Code Quality Standards
- Methods should return **Task<T>** for async operations.
- Use **sealed classes** where inheritance is not intended.
- Implement **proper disposal patterns** for resources.
- Use **readonly fields** for injected dependencies.
- Prefer **composition over inheritance** for extending functionality.
- **Validate expression trees** before processing to ensure compatibility.

### Error Handling
- Use `Result<T>` patterns from CSharpFunctionalExtensions for operations that may fail.
- Log errors using `ILogger<T>` with appropriate log levels.
- Provide meaningful error messages for developers using the library.
- Handle `CancellationToken` appropriately in async methods.
- Throw **NotSupportedException** for unsupported LINQ expressions with helpful guidance.

### Performance Considerations
- Cache query results using `IProgressiveCache` with appropriate cache keys.
- Use `IEnumerable<T>` for lazy evaluation where possible.
- Implement **pagination support** for large result sets.
- Consider **memory efficiency** when working with large content collections.
- **Optimize expression tree traversal** to minimize allocations.
- Use **expression compilation caching** where appropriate.

### Documentation Requirements
- Include **code examples** in XML documentation for complex APIs.
- Document **generic type constraints** and their purposes.
- Provide **usage examples** similar to those in README.md.
- Include **performance considerations** in documentation where relevant.
- Document **supported LINQ expressions** and their Kentico query equivalents.
- Provide **troubleshooting guidance** for common expression translation issues.