using CMS.Websites;

namespace XperienceCommunity.DataContext.Abstractions;

/// <summary>
/// Represents a context for querying page content of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the page content.</typeparam>
public interface IPageContentContext<T> : IDataContext<T> where T : class, IWebPageFieldsSource, new()
{
    /// <summary>
    /// Filters the page content based on the specified channel name.
    /// </summary>
    /// <param name="channelName">The channel name used to filter the page content.</param>
    /// <returns>A new instance of <see cref="IPageContentContext{T}"/> with the applied channel filter.</returns>
    IPageContentContext<T> InChannel(string channelName);

    /// <summary>
    /// Filters the page content based on the specified path match.
    /// </summary>
    /// <param name="pathMatch">The path match used to filter the page content.</param>
    /// <returns>A new instance of <see cref="IPageContentContext{T}"/> with the applied path match filter.</returns>
    IPageContentContext<T> OnPath(PathMatch pathMatch);
}
