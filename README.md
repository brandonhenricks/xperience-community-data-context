# XperienceCommunity.DataContext

Enhance your Kentico Xperience development with a fluent API for intuitive and efficient query building. This project abstracts the built-in ContentItemQueryBuilder, leveraging .NET 8 and integrated with Xperience By Kentico, to improve your local development and testing workflow.

## Features

- **Fluent API** for query building with strongly-typed expressions
- **Built-in caching** with automatic cache dependency management
- **Three specialized contexts** for different content types:
  - `IContentItemContext<T>` - For content hub items with strongly-typed querying
  - `IPageContentContext<T>` - For web pages with channel and path filtering capabilities
  - `IReusableSchemaContext<T>` - For reusable schemas supporting both classes and interfaces
- **Unified data context** via `IXperienceDataContext` for centralized access to all context types
- **Extensible processor system** for custom content processing and transformation pipelines
- **Interface support** in `ReusableSchemaContext` for maximum flexibility
- **Modern architecture** with base classes reducing code duplication by 70%+
- Built on **.NET 6/.NET 8/.NET 9**, ensuring modern development practices
- Seamless integration with **Xperience by Kentico**

## Quick Start

1. **Prerequisites:** Ensure you have .NET 8/.NET 9 and Kentico Xperience installed.
2. **Installation:** Install this project through Nuget.

## Architecture Overview

XperienceCommunity.DataContext provides three specialized contexts for different content scenarios:

### 1. Content Item Context (`IContentItemContext<T>`)

- **Purpose**: Query content hub items with strongly-typed expressions
- **Use Case**: Content items, reusable content blocks, structured data
- **Type Constraint**: `T` must implement `IContentItemFieldSource`
- **Features**: Full LINQ support, caching, linked items

### 2. Page Content Context (`IPageContentContext<T>`)

- **Purpose**: Query web pages with channel and path filtering
- **Use Case**: Website pages, routing, channel-specific content
- **Type Constraint**: `T` must implement `IWebPageFieldsSource`
- **Features**: Channel filtering, path-based queries, page hierarchy

### 3. Reusable Schema Context (`IReusableSchemaContext<T>`)

- **Purpose**: Query reusable schemas with maximum flexibility
- **Use Case**: Shared content schemas, interface-based content models
- **Type Constraint**: None - supports both classes and interfaces
- **Features**: Interface support, schema flexibility, reusable patterns

### Unified Data Context (`IXperienceDataContext`)

- **Purpose**: Centralized access to all context types
- **Benefits**: Single dependency injection, consistent API, easier testing
- **Methods**: `ForContentType<T>()`, `ForPageContentType<T>()`, `ForReusableSchema<T>()`

## Prerequisites

Before you begin, ensure you have met the following requirements:

- **.NET:** Make sure you have a .NET 8, or .NET 9 installed on your development machine. You can download it from [the official .NET download page](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Xperience By Kentico Project:** You need an existing Xperience By Kentico project. If you're new to Xperience By Kentico, start with [the official documentation](https://docs.xperience.io/).

## Installation

To integrate XperienceCommunity.DataContext into your Kentico Xperience project, follow these steps:

1. **NuGet Package**: Install the NuGet package via the Package Manager Console.

   ```shell
   Install-Package XperienceCommunity.DataContext
   ```

2. **Configure Services:**
   - In your `Startup.cs` or wherever you configure services, add the following line to register XperienceCommunity.DataContext services with dependency injection:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddXperienceDataContext();
}
```

## Injecting the `IContentItemContext` into a Class

To leverage the `IContentItemContext` in your classes, you need to inject it via dependency injection. The `IContentItemContext` requires a class that implements the `IContentItemFieldSource` interface. For instance, you might have a `GenericContent` class designed for the Content Hub.

### Querying Content Items Example:

Assuming you have a `GenericContent` class that implements `IContentItemFieldSource`, you can inject the `IContentItemContext<GenericContent>` into your classes as follows:

```csharp
public class MyService
{
    private readonly IContentItemContext<GenericContent> _contentItemContext;

    public MyService(IContentItemContext<GenericContent> contentItemContext)
    {
        _contentItemContext = contentItemContext;
    }

    // Example method using the _contentItemContext
    public async Task<GenericContent> GetContentItemAsync(Guid contentItemGUID)
    {
        return await _contentItemContext
            .FirstOrDefaultAsync(x => x.SystemFields.ContentItemGUID == contentItemGUID);
    }
}
```

This setup allows you to utilize the fluent API provided by `IContentItemContext` to interact with content items in a type-safe manner, enhancing the development experience with Kentico Xperience.

## Example Usage

Here's a quick example to show how you can use XperienceCommunity.DataContext in your project:

```csharp
var result = await _context
    .WithLinkedItems(1)
    .FirstOrDefaultAsync(x => x.SystemFields.ContentItemGUID == selected.Identifier, HttpContext.RequestAborted);
```

This example demonstrates how to asynchronously retrieve the first content item that matches a given GUID, with a single level of linked items included, using the fluent API provided by XperienceCommunity.DataContext.

### Querying Page Content Example:

Assuming you have a `GenericPage` class that implements `IWebPageFieldsSource`, you can inject the `IPageContentContext<GenericPage>` into your classes as follows:

```csharp
public class GenericPageController: Controller
{
    private readonly IPageContentContext<GenericPage> _pageContext;
    private readonly IWebPageDataContextRetriever _webPageDataContextRetriever;

    public GenericPageController(IPageContentContext<GenericPage> pageContext
            IWebPageDataContextRetriever webPageDataContextRetriever)
    {
        _pageContext = pageContext;
        _webPageDataContextRetriever = webPageDataContextRetriever;
    }

    // Example method using the _pageContext
    public async Task<IActionResult> IndexAsync()
    {           
         var page = _webPageDataContextRetriever.Retrieve().WebPage;

        if (page == null)
        {
            return NotFound();
        }

        var content = await _pageContext
            .FirstOrDefaultAsync(x => x.SystemFields.WebPageItemID == page.WebPageItemID, HttpContext.RequestAborted);

        if (content == null)
        {
            return NotFound();
        }

        return View(conent);
    }
}
```

This example demonstrates how to asynchronously retrieve the first page content item that matches a given ID, using the fluent API provided by XperienceCommunity.DataContext.

### Using IXperienceDataContext Example:

To demonstrate how to use the IXperienceDataContext interface, consider the following example:

```csharp

public class ContentService
{
    private readonly IXperienceDataContext _dataContext;

    public ContentService(IXperienceDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<GenericContent> GetContentItemAsync(Guid contentItemGUID)
    {
        var contentItemContext = _dataContext.ForContentType<GenericContent>();
        return await contentItemContext.FirstOrDefaultAsync(x => x.SystemFields.ContentItemGUID == contentItemGUID);
    }

    public async Task<GenericPage> GetPageContentAsync(Guid pageGUID)
    {
        var pageContentContext = _dataContext.ForPageContentType<GenericPage>();
        return await pageContentContext.FirstOrDefaultAsync(x => x.SystemFields.PageGUID == pageGUID);
    }
}

```

In this example, the ContentService class uses the IXperienceDataContext interface to get contexts for content items and page content. This setup allows you to leverage the fluent API provided by IContentItemContext and IPageContentContext to interact with content items and page content in a type-safe manner.

## Advanced Usage Examples

### Working with Reusable Schema Context

The `IReusableSchemaContext<T>` is the most flexible context, supporting both classes and interfaces. This is particularly useful for reusable content schemas:

```csharp
// Define an interface for shared content
public interface ISharedContent
{
    string Title { get; set; }
    string Description { get; set; }
    DateTime PublishDate { get; set; }
}

// Use the interface with ReusableSchemaContext
public class SharedContentService
{
    private readonly IReusableSchemaContext<ISharedContent> _schemaContext;

    public SharedContentService(IReusableSchemaContext<ISharedContent> schemaContext)
    {
        _schemaContext = schemaContext;
    }

    public async Task<IEnumerable<ISharedContent>> GetRecentContentAsync()
    {
        return await _schemaContext
            .Where(x => x.PublishDate >= DateTime.Now.AddDays(-30))
            .OrderByDescending(x => x.PublishDate)
            .ToListAsync();
    }
}
```

### Custom Processors and Extensibility

The library includes an extensible processor system for custom content transformations:

```csharp
// Custom processor for specialized queries
public class CustomContentProcessor : IExpressionProcessor
{
    public bool CanProcess(Expression expression)
    {
        // Define when this processor should handle expressions
        return expression is MethodCallExpression method && 
               method.Method.Name == "HasTag";
    }

    public Expression Process(Expression expression)
    {
        // Transform the expression for your specific needs
        // Implementation details depend on your requirements
        return expression;
    }
}

// Register your custom processor
services.AddScoped<IExpressionProcessor, CustomContentProcessor>();
```

### Unified Data Context Patterns

Using `IXperienceDataContext` for centralized content management:

```csharp
public class ContentManagementService
{
    private readonly IXperienceDataContext _dataContext;

    public ContentManagementService(IXperienceDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<T> GetContentByIdAsync<T>(int id) where T : class, IContentItemFieldSource
    {
        return await _dataContext
            .ForContentType<T>()
            .FirstOrDefaultAsync(x => x.SystemFields.ContentItemID == id);
    }

    public async Task<T> GetPageByPathAsync<T>(string path) where T : class, IWebPageFieldsSource
    {
        return await _dataContext
            .ForPageContentType<T>()
            .FirstOrDefaultAsync(x => x.SystemFields.WebPageUrlPath == path);
    }

    public async Task<IEnumerable<T>> GetSchemaContentAsync<T>() where T : class
    {
        return await _dataContext
            .ForReusableSchema<T>()
            .ToListAsync();
    }
}
```

### Performance Optimization with Caching

The library includes built-in caching with automatic cache dependency management:

```csharp
public class OptimizedContentService
{
    private readonly IContentItemContext<Article> _contentContext;

    public OptimizedContentService(IContentItemContext<Article> contentContext)
    {
        _contentContext = contentContext;
    }

    public async Task<IEnumerable<Article>> GetFeaturedArticlesAsync()
    {
        // Caching is automatically handled with proper cache dependencies
        return await _contentContext
            .WithLinkedItems(2) // Include linked items up to 2 levels
            .Where(x => x.IsFeatured == true)
            .OrderByDescending(x => x.PublishDate)
            .Take(5)
            .ToListAsync();
    }
}
```

## Troubleshooting & Best Practices

### Common Issues and Solutions

#### 1. Type Constraint Errors

**Problem**: `The type 'T' cannot be used as type parameter 'T' in the generic type or method`

**Solution**: Ensure your types implement the required interfaces:

- `IContentItemContext<T>` requires `T : IContentItemFieldSource`
- `IPageContentContext<T>` requires `T : IWebPageFieldsSource`
- `IReusableSchemaContext<T>` has no constraints - supports any type

#### 2. Interface Support in ReusableSchemaContext

**Problem**: Need to use interfaces instead of concrete classes

**Solution**: Use `IReusableSchemaContext<T>` which supports both classes and interfaces:

```csharp
// This works with interfaces
IReusableSchemaContext<IMyInterface> context;

// This also works with classes
IReusableSchemaContext<MyClass> context;
```

#### 3. Processor Registration

**Problem**: Custom processors not being recognized

**Solution**: Register your processors in the DI container:

```csharp
services.AddScoped<IExpressionProcessor, CustomProcessor>();
services.AddXperienceDataContext(); // Call after registering processors
```

#### 4. Performance Optimization

**Best Practices**:

- Use `WithLinkedItems()` only when needed
- Prefer `FirstOrDefaultAsync()` over `ToListAsync().FirstOrDefault()`
- Leverage built-in caching - don't implement your own caching layer
- Use cancellation tokens for long-running operations

#### 5. Testing with Contexts
**Recommendation**: Use `IXperienceDataContext` for easier mocking in unit tests:

```csharp
// Easy to mock
public class MyService
{
    private readonly IXperienceDataContext _dataContext;
    
    public MyService(IXperienceDataContext dataContext)
    {
        _dataContext = dataContext;
    }
}
```

### Architecture Notes

The library uses a three-tier architecture:

1. **Base Classes**: `BaseDataContext<T, TExecutor>` and `ProcessorSupportedQueryExecutor<T, TProcessor>`
2. **Specialized Contexts**: Content, Page, and Reusable Schema contexts
3. **Unified Interface**: `IXperienceDataContext` for centralized access

This design reduces code duplication by 70%+ while maintaining type safety and flexibility.

## Built With

- [Xperience By Kentico](https://www.kentico.com) - Kentico Xperience
- [NuGet](https://nuget.org/) - Dependency Management

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags).

## Authors

- **Brandon Henricks** - *Initial work* - [Brandon Henricks](https://github.com/brandonhenricks)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

- [Mike Wills](https://github.com/heywills)
- David Rector
