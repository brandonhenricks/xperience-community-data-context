using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;

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
    /// Thread safe dictionary for faster schema name lookup.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> s_schemaNames = new();

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

    /// <summary>
    /// Gets the value of a static string field from a given type.
    /// </summary>
    /// <param name="type">The type to get the static string field from.</param>
    /// <param name="fieldName">The name of the static string field.</param>
    /// <returns>The value of the static string field if found; otherwise, an empty string.</returns>
    internal static string? GetStaticString(this Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (field == null || field.FieldType != typeof(string))
        {
            return null;
        }

        return field.GetValue(null) as string ?? null;
    }


    /// <summary>
    /// Gets the reusable field schema name for a given value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="type">The value to get the reusable field schema name from.</param>
    /// <returns>The reusable field schema name if found; otherwise, null.</returns>
    internal static string? GetReusableFieldSchemaName<T>(this T type) where T: notnull, Type
    {
        if (type is null)
        {
            return null;
        }

        Debug.Assert(type is Type, "Type parameter T must be of type Type.");
        Debug.Assert(type is not null, "Type parameter T must not be null.");

        if (type.IsInterface)
        {
            var reusableSchemaName =  type.GetStaticString(ReusableSchemaFieldName);

            if (!string.IsNullOrEmpty(reusableSchemaName))
            {
                s_schemaNames.TryAdd(type.Name, reusableSchemaName);
                return reusableSchemaName;
            }

            return null;
        }

        var interfaces = type.GetInterfaces() ?? [];

        if (!type.IsInterface && (interfaces is null || interfaces.Length == 0))
        {
            return null;
        }

        string? interfaceName = interfaces.FirstOrDefault()?.Name;

        if (string.IsNullOrEmpty(interfaceName))
        {
            return null;
        }

        if (s_schemaNames.TryGetValue(interfaceName, out string? schemaName))
        {
            return schemaName;
        }

        schemaName = type?.GetStaticString(ReusableSchemaFieldName);

        if (string.IsNullOrEmpty(schemaName))
        {
            return null;
        }

        s_schemaNames.TryAdd(interfaceName, schemaName);

        return schemaName;
    }
}
