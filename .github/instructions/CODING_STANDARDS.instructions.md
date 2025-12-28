# Coding Standards

Repository-wide conventions and recommendations derived from the codebase:

- Language & Targeting
  - Multi-targeting .NET 8/.NET 9 is used in CI; prefer language features compatible with both targets.
  - Implicit usings enabled — omit explicit common usings in new files unless needed.
  - Nullable Reference Types enabled — annotate nullability and prefer `ArgumentNullException.ThrowIfNull` patterns.

- Naming & Structure
  - One public class per file.
  - Type names: `PascalCase` for classes and interfaces (`IContentItemContext`).
  - Private fields: `_camelCase` prefixed with underscore (existing style in `BaseDataContext`).

- Formatting & Style
  - Follow existing indentation and braces style — minimal reformatting in patches.
  - Keep methods short and focused; prefer private helpers over large methods.

- Async & Cancellation
  - All I/O methods should be async and accept `CancellationToken` (follow `SingleOrDefaultAsync` pattern).

- Errors & Validation
  - Use `ArgumentNullException.ThrowIfNull(...)` and `ArgumentException.ThrowIfNullOrEmpty(...)` where appropriate.
  - Prefer throwing domain-specific exceptions for expression translation issues (repository already defines `UnsupportedExpressionException`).

- Tests
  - Follow AAA pattern (Arrange, Act, Assert).
  - Use NSubstitute for mocking Kentico dependencies (current tests already follow this).

- Documentation & Contribution
  - When adding expression processors, include a short doc comment describing the supported expression shape and an accompanying unit test.

Recommended linters/formatters (optional)
- `dotnet format` for whitespace/formatting consistency.
- Consider adding `editorconfig` if not already present to lock whitespace and file-level settings.
# Coding Standards

This document defines the coding conventions, style guidelines, and quality standards for the XperienceCommunity.DataContext library.

---

## General Principles

1. **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
2. **DRY (Don't Repeat Yourself)**: Eliminate code duplication through abstraction
3. **KISS (Keep It Simple, Stupid)**: Favor simplicity over complexity
4. **YAGNI (You Aren't Gonna Need It)**: Don't add functionality until needed
5. **Clean Code**: Code should be self-documenting and readable

---

## Project Settings

### Target Frameworks
- **Multi-targeting**: .NET 8.0 and .NET 9.0
- **Language Version**: `latest`
- **Nullable Reference Types**: **Enabled** (all new code must handle nullability)
- **Implicit Usings**: **Enabled**

### Global Settings (Directory.Build.props)
```xml
<PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>1591</NoWarn> <!-- Suppress XML doc warnings for internal/private members -->
</PropertyGroup>
```

---

## Naming Conventions

### General Rules
- Use **PascalCase** for: Classes, methods, properties, public fields, constants
- Use **camelCase** for: Local variables, private fields, parameters
- Use **_camelCase** (underscore prefix) for: Private instance fields
- Use **IPascalCase** for: Interfaces (prefix with `I`)
- Avoid abbreviations except well-known ones (e.g., `Id`, `Url`, `Html`)

### Examples
```csharp
// ✅ Good
public class ContentItemContext<T> 
{
    private readonly IProgressiveCache _cache;
    private readonly XperienceDataContextConfig _config;
    private bool? _includeTotalCount;
    
    public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var queryBuilder = BuildQuery(expression);
        var cacheKey = GetCacheKey(queryBuilder);
        return await GetOrCacheAsync(executeFunc, cacheKey);
    }
}

// ❌ Bad
public class contentItemContext<T> // Wrong casing
{
    private IProgressiveCache cache; // Missing underscore
    private bool? include_total_count; // Snake case instead of camelCase
    
    public async Task<IEnumerable<T>> to_list_async(CancellationToken ct) // Snake case
    {
        var qb = build_query(expr); // Unclear abbreviation
        return await get_or_cache_async(func, key);
    }
}
```

### File Organization
- **One class per file** (exception: small related interfaces or nested types)
- **File name matches type name**: `ContentItemContext.cs` for `ContentItemContext<T>`
- **Folder structure mirrors namespaces**

---

## Code Organization

### Namespace Structure
```
XperienceCommunity.DataContext           // Root namespace
├── Abstractions                          // Interfaces
│   └── Processors                        // Processor interfaces
├── Configurations                        // Configuration classes
├── Contexts                              // Context implementations
├── Core                                  // Base classes
├── Diagnostics                           // Diagnostics and telemetry
├── Exceptions                            // Custom exceptions
├── Executors                             // Query executors
├── Expressions                           // Expression processing
│   ├── Processors                        // Expression processors
│   └── Visitors                          // Expression visitors
└── Extensions                            // Extension methods
```

### Class Member Order
1. Constants
2. Static fields
3. Private fields (readonly first, then mutable)
4. Constructors
5. Properties (public, then protected, then private)
6. Public methods
7. Protected methods
8. Private methods
9. Nested types

### Example
```csharp
public class MyClass
{
    // 1. Constants
    private const string DefaultLanguage = "en-US";
    
    // 2. Static fields
    private static readonly ActivitySource ActivitySource = new("MyClass");
    
    // 3. Private fields (readonly first)
    private readonly ILogger _logger;
    private readonly IProgressiveCache _cache;
    private bool _isInitialized;
    
    // 4. Constructors
    public MyClass(ILogger logger, IProgressiveCache cache)
    {
        _logger = logger;
        _cache = cache;
    }
    
    // 5. Properties
    public string Language { get; set; } = DefaultLanguage;
    protected bool IsInitialized => _isInitialized;
    
    // 6. Public methods
    public async Task ExecuteAsync() { }
    
    // 7. Protected methods
    protected void ValidateState() { }
    
    // 8. Private methods
    private string GetCacheKey() => "";
    
    // 9. Nested types
    private class InternalState { }
}
```

---

## Formatting & Style

### Indentation & Spacing
- **Indentation**: 4 spaces (no tabs)
- **Line length**: Soft limit of 120 characters
- **Blank lines**:
  - One blank line between methods
  - One blank line between property groups
  - Two blank lines between major sections (if needed)

### Braces
- **Opening brace on new line** (Allman style) for: Classes, methods, properties
- **Opening brace on same line** for: Conditional statements, loops (optional, but consistent)

```csharp
// Classes, methods - new line
public class MyClass
{
    public void MyMethod()
    {
        // Implementation
    }
}

// Conditionals - same line (consistent with team preference)
if (condition)
{
    // Do something
}
else
{
    // Do something else
}

// Expression-bodied members - no braces
public string Name => _name;
public int Count() => _items.Count;
```

### Whitespace
```csharp
// ✅ Good spacing
public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
{
    var result = await _cache.LoadAsync(
        async cs =>
        {
            var data = await ExecuteAsync();
            return data;
        },
        cacheSettings);
    
    return result ?? Enumerable.Empty<T>();
}

// ❌ Bad spacing
public async Task<IEnumerable<T>>ToListAsync(CancellationToken cancellationToken=default){
var result=await _cache.LoadAsync(async cs=>{var data=await ExecuteAsync();return data;},cacheSettings);
return result??Enumerable.Empty<T>();}
```

---

## Language Features

### Modern C# Features (Preferred)
```csharp
// ✅ Use pattern matching
if (value is string str && str.Length > 0)
{
    // Use str
}

// ✅ Use null-coalescing assignment
_cache ??= CreateCache();

// ✅ Use target-typed new
var config = new XperienceDataContextConfig();

// ✅ Use collection expressions (.NET 8+)
var list = [item1, item2, item3];

// ✅ Use file-scoped namespaces
namespace XperienceCommunity.DataContext;

// ✅ Use primary constructors (when appropriate)
public class MyService(ILogger logger) : IMyService
{
    public void DoWork() => logger.LogInformation("Working");
}

// ✅ Use expression-bodied members
public string GetName() => _name;
public void SetName(string name) => _name = name;

// ✅ Use string interpolation
var message = $"User {userId} logged in at {DateTime.Now}";
```

### Avoid
```csharp
// ❌ Avoid explicit type when obvious
Dictionary<string, object?> parameters = new Dictionary<string, object?>();
// ✅ Prefer var
var parameters = new Dictionary<string, object?>();

// ❌ Avoid magic numbers
if (count > 100) { }
// ✅ Use named constants
private const int MaxItems = 100;
if (count > MaxItems) { }

// ❌ Avoid nested ternaries
var result = condition1 ? value1 : condition2 ? value2 : value3;
// ✅ Use if-else for clarity
var result = condition1 ? value1 : value2;
if (condition2) result = value3;
```

---

## Asynchronous Programming

### Async/Await Rules
```csharp
// ✅ Always accept CancellationToken
public async Task<T> ExecuteAsync(CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    // Implementation
}

// ✅ Use ConfigureAwait(false) in libraries (when not using context)
var result = await _httpClient.GetAsync(url).ConfigureAwait(false);

// ✅ Avoid async void (except event handlers)
public async Task HandleAsync() { } // ✅
public async void HandleAsync() { } // ❌

// ✅ Don't block on async code
var result = await GetDataAsync(); // ✅
var result = GetDataAsync().Result; // ❌ Can cause deadlocks

// ✅ Return Task directly when no additional work
public Task<int> GetCountAsync() => _repository.GetCountAsync();
// ❌ Unnecessary async/await wrapper
public async Task<int> GetCountAsync() => await _repository.GetCountAsync();
```

---

## Error Handling

### Exception Handling
```csharp
// ✅ Catch specific exceptions
try
{
    await ProcessAsync();
}
catch (ArgumentNullException ex)
{
    _logger.LogError(ex, "Argument was null");
    throw; // Preserve stack trace
}
catch (InvalidOperationException ex)
{
    _logger.LogError(ex, "Invalid operation");
    throw new CustomException("Failed to process", ex);
}

// ❌ Avoid catching Exception
try
{
    await ProcessAsync();
}
catch (Exception ex) // Too broad
{
    // What exactly are you catching?
}

// ✅ Use guard clauses
public void Process(string? input)
{
    ArgumentException.ThrowIfNullOrEmpty(input);
    // Process input
}

// ✅ Create domain-specific exceptions
public class ExpressionProcessingException : Exception
{
    public Expression? Expression { get; }
    
    public ExpressionProcessingException(string message, Expression? expression = null)
        : base(message)
    {
        Expression = expression;
    }
}
```

---

## Nullability

### Nullable Reference Types
```csharp
// ✅ Enable nullable reference types in all files
#nullable enable

// ✅ Use nullable annotations
public string? GetName() // May return null
{
    return _name;
}

public string GetRequiredName() // Never returns null
{
    return _name ?? throw new InvalidOperationException("Name not set");
}

// ✅ Validate parameters
public void SetName(string name)
{
    ArgumentNullException.ThrowIfNull(name);
    _name = name;
}

// ✅ Use null-forgiving operator sparingly (when you know better than compiler)
var value = GetValue()!; // I know this won't be null

// ❌ Disable nullable warnings without reason
#nullable disable // Bad practice
```

---

## Comments & Documentation

### XML Documentation
```csharp
/// <summary>
/// Executes the query asynchronously and returns the results.
/// </summary>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>
/// A task that represents the asynchronous operation. 
/// The task result contains the query results, or an empty collection if no results found.
/// </returns>
/// <exception cref="InvalidOperationException">
/// Thrown when the query is not properly initialized.
/// </exception>
public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Inline Comments
```csharp
// ✅ Explain why, not what
// Bypass cache in preview mode to show latest content
if (_websiteChannelContext.IsPreview)
{
    return await executeFunc();
}

// ❌ State the obvious
// Check if preview
if (_websiteChannelContext.IsPreview)
{
    return await executeFunc();
}

// ✅ Use TODO comments for future work
// TODO: Implement query plan caching for improved performance

// ✅ Use comments for complex algorithms
// Range optimization: Combine multiple comparison expressions (e.g., x > 5 && x < 10)
// into a single BETWEEN operation for better query performance
if (IsRangeExpression(node))
{
    OptimizeRange(node);
}
```

---

## Dependency Injection

### Registration
```csharp
// ✅ Register with appropriate lifetimes
services.AddScoped<IContentItemContext<T>, ContentItemContext<T>>(); // Per-request
services.AddSingleton<XperienceDataContextConfig>(); // Shared
services.AddTransient<IProcessor<T>, Processor<T>>(); // New instance each time

// ✅ Use extension methods for cohesive registration
public static IServiceCollection AddXperienceDataContext(this IServiceCollection services)
{
    // Register all related services
    return services;
}

// ✅ Register open generics
services.AddScoped(typeof(IContentItemContext<>), typeof(ContentItemContext<>));
```

### Constructor Injection
```csharp
// ✅ Inject interfaces, not concrete types
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly IProgressiveCache _cache;
    
    public MyService(ILogger<MyService> logger, IProgressiveCache cache)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cache);
        
        _logger = logger;
        _cache = cache;
    }
}

// ❌ Avoid property injection (use constructor injection)
public class MyService
{
    public ILogger Logger { get; set; } // ❌
}
```

---

## Performance Considerations

### Best Practices
```csharp
// ✅ Use StringBuilder for concatenation in loops
var sb = new StringBuilder();
for (int i = 0; i < 1000; i++)
{
    sb.Append(i);
}

// ✅ Use ConcurrentDictionary for thread-safe collections
private readonly ConcurrentDictionary<string, object?> _cache = new();

// ✅ Avoid unnecessary allocations
return Array.Empty<T>(); // ✅ Cached singleton
return new T[0]; // ❌ New allocation

// ✅ Use ValueTask for hot paths (when appropriate)
public ValueTask<int> GetCountAsync() => new(_count);

// ✅ Use Span<T> for performance-critical code
public void Process(ReadOnlySpan<char> input)
{
    // Process without allocating string
}
```

### Conditional Compilation
```csharp
// ✅ Use for debug-only code
[Conditional("DEBUG")]
private void ValidateState()
{
    // Validation logic - removed in Release builds
}

// ✅ Use for performance-critical validation
[DebuggerStepThrough]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void EnsureNotNull(object? value)
{
    if (value is null) throw new ArgumentNullException();
}
```

---

## Testing Standards

### Test Naming
```csharp
// ✅ Use descriptive names: Method_Scenario_ExpectedOutcome
[Fact]
public void Constructor_WhenCacheIsNull_ThrowsArgumentNullException()
{
    // Arrange, Act, Assert
}

[Fact]
public void GetCacheKey_WithValidParameters_ReturnsExpectedFormat()
{
    // Arrange, Act, Assert
}
```

### Test Structure (AAA Pattern)
```csharp
[Fact]
public void ToListAsync_WithValidQuery_ReturnsResults()
{
    // Arrange
    var context = CreateContext();
    var expectedItems = new[] { new TestItem(), new TestItem() };
    
    // Act
    var result = await context.ToListAsync();
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count());
}
```

### Mocking
```csharp
// ✅ Use NSubstitute for clean, readable mocks
var cache = Substitute.For<IProgressiveCache>();
cache.LoadAsync(Arg.Any<Func<CacheSettings, Task<T>>>(), Arg.Any<CacheSettings>())
    .Returns(Task.FromResult(expectedData));

// ✅ Verify interactions
await service.ExecuteAsync();
await cache.Received(1).LoadAsync(Arg.Any<Func>(), Arg.Any<CacheSettings>());
```

---

## Code Quality Tools

### Analysis
- **Roslyn Analyzers**: Enabled (latest)
- **Nullable Reference Types**: Enforced
- **Code Analysis**: Treat warnings as errors in CI/CD

### EditorConfig (Recommended)
```ini
[*.cs]
# Indentation
indent_style = space
indent_size = 4

# New line preferences
end_of_line = crlf
insert_final_newline = true

# Naming conventions
dotnet_naming_rule.private_fields.symbols = private_fields
dotnet_naming_rule.private_fields.style = underscore_camelcase
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

# Code style
csharp_prefer_braces = true
csharp_prefer_simple_using_statement = true
csharp_prefer_static_local_function = true
```

---

## Identified Inconsistencies & Action Items

### Current Issues
1. **Inconsistent brace style**: Some files use Allman, others K&R
   - **Action**: Standardize on Allman style for all files
   
2. **Missing XML documentation**: Some public methods lack documentation
   - **Action**: Add documentation to all public APIs
   
3. **Diagnostic logging level inconsistency**: Some use Debug, others use Information
   - **Action**: Standardize on Debug for detailed logs, Information for key events

### Compliance Checklist
- [ ] All public APIs have XML documentation
- [ ] All async methods accept CancellationToken
- [ ] All fields follow naming conventions (_camelCase for private)
- [ ] Nullable reference types enabled in all files
- [ ] DebuggerDisplay attributes on all public classes
- [ ] No compiler warnings in Release build
- [ ] Unit tests follow AAA pattern
- [ ] Test coverage > 70% (target: 80%)

---

**Last Updated**: December 28, 2025
**Review Frequency**: Quarterly or when adding new contributors
