using CMS.ContentEngine;
using CMS.Websites;

namespace XperienceCommunity.DataContext.Abstractions;

/// <summary>
/// Represents the data context for Xperience by Kentico CMS.
/// </summary>
public interface IXperienceDataContext
{
    /// <summary>
    /// Gets the content item context for the specified content type.
    /// </summary>
    /// <typeparam name="T">The type of the content item.</typeparam>
    /// <returns>The content item context.</returns>
    IContentItemContext<T> ForContentType<T>() where T : class, IContentItemFieldsSource, new();

    /// <summary>
    /// Gets the page content context for the specified page content type.
    /// </summary>
    /// <typeparam name="T">The type of the page content.</typeparam>
    /// <returns>The page content context.</returns>
    IPageContentContext<T> ForPageContentType<T>() where T : class, IWebPageFieldsSource, new();

    /// <summary>
    /// Gets the reusable schema context for the specified schema.
    /// </summary>
    /// <typeparam name="T">The type of the schema.</typeparam>
    /// <returns>The reusable schema context.</returns>
    IReusableSchemaContext<T> ForReusableSchema<T>();
}
