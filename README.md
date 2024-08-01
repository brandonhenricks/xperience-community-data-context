# XperienceCommunity.DataContext

Enhance your Kentico Xperience development with a fluent API for intuitive and efficient query building. This project abstracts the built-in ContentItemQueryBuilder, leveraging .NET 8 and integrated with Xperience By Kentico, to improve your local development and testing workflow.

## Features

- Fluent API for query building with expressions.
- Built in caching.
- Built on .NET6/.NET 8, ensuring modern development practices.
- Seamless integration with Xperience By Kentico.

## Quick Start

1. **Prerequisites:** Ensure you have .NET 6/.NET 8 and Kentico Xperience installed.
2. **Installation:** Install this project through Nuget.

## Prerequisites

Before you begin, ensure you have met the following requirements:

- **.NET:** Make sure you have a .NET 6, or .NET 8 installed on your development machine. You can download it from [here](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Xperience By Kentico Project:** You need an existing Xperience By Kentico project. If you're new to Xperience By Kentico, start [here](https://docs.xperience.io/).

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


## Built With

* [Xperience By Kentico](https://www.kentico.com) - Kentico Xperience
* [NuGet](https://nuget.org/) - Dependency Management

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## Authors

* **Brandon Henricks** - *Initial work* - [Brandon Henricks](https://github.com/brandonhenricks)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* [Mike Wills](https://github.com/heywills)
* David Rector
