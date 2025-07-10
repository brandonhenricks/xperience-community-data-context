using CMS.ContentEngine;
using CMS.Websites;

namespace XperienceCommunity.DataContext.Abstractions.Processors;

/// <summary>
/// Marker Interface.
/// </summary>
public interface IProcessor
{

}

/// <summary>
/// Represents a processor for a specific type of content.
/// </summary>
/// <typeparam name="T">The type of content to process.</typeparam>
public interface IProcessor<in T>: IProcessor where T : class, new()
{
    /// <summary>
    /// Processes the specified content asynchronously.
    /// </summary>
    /// <param name="content">The content to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(T content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the order of the processor.
    /// </summary>
    int Order { get; }
}

/// <summary>
/// Represents a processor for a specific type of content item.
/// </summary>
/// <typeparam name="T">The type of content item to process.</typeparam>
public interface IContentItemProcessor<in T> : IProcessor<T> where T : class, IContentItemFieldsSource, new()
{

}

/// <summary>
/// Represents a processor for a specific type of page content.
/// </summary>
/// <typeparam name="T">The type of page content to process.</typeparam>
public interface IPageContentProcessor<in T> : IProcessor<T> where T : class, IWebPageFieldsSource, new()
{

}
