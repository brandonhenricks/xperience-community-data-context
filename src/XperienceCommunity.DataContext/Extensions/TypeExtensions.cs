using CMS.ContentEngine;
using CMS.Websites;
using System.Collections.Concurrent;

namespace XperienceCommunity.DataContext.Extensions;

/// <summary>
/// Provides extension methods for working with types.
/// </summary>
internal static class TypeExtensions
{
    private const string FieldName = "CONTENT_TYPE_NAME";

    private const string ReusableSchemaFieldName = "REUSABLE_FIELD_SCHEMA_NAME";

    private static readonly ConcurrentDictionary<string, string> s_classNames =
        new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the specified type inherits from <see cref="IWebPageFieldsSource"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type inherits from <see cref="IWebPageFieldsSource"/>; otherwise, <c>false</c>.</returns>
    internal static bool InheritsFromIWebPageFieldsSource(this Type type)
    {
        return typeof(IWebPageFieldsSource).IsAssignableFrom(type);
    }

    /// <summary>
    /// Determines whether the specified type inherits from <see cref="IContentItemFieldsSource"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><c>true</c> if the specified type inherits from <see cref="IContentItemFieldsSource"/>; otherwise, <c>false</c>.</returns>
    internal static bool InheritsFromIContentItemFieldsSource(this Type type)
    {
        return typeof(IContentItemFieldsSource).IsAssignableFrom(type);
    }

    /// <summary>
    /// Gets the content type name associated with the specified type.
    /// </summary>
    /// <param name="type">The type to get the content type name for.</param>
    /// <returns>The content type name associated with the specified type, or <c>null</c> if the type does not inherit from <see cref="IWebPageFieldsSource"/> or <see cref="IContentItemFieldsSource"/>, or if the content type name is not found.</returns>
    internal static string? GetContentTypeName(this Type? type)
    {
        if (type is null)
        {
            return null;
        }

        if (!type.InheritsFromIWebPageFieldsSource() && !type.InheritsFromIContentItemFieldsSource())
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(type.FullName))
        {
            return null;
        }

        if (s_classNames.TryGetValue(type.FullName, out var contentTypeName))
        {
            return contentTypeName;
        }

        contentTypeName = type.GetField(FieldName)?.GetRawConstantValue() as string;

        if (string.IsNullOrWhiteSpace(contentTypeName))
        {
            return null;
        }

        s_classNames.TryAdd(type!.FullName, contentTypeName);

        return contentTypeName;
    }
}
