using CMS.ContentEngine;

namespace XperienceCommunity.DataContext.Abstractions;

/// <summary>
/// Defines the contract for a context that provides access to content items of a specified type.
/// </summary>
/// <typeparam name="T">The type of the content item.</typeparam>
/// <summary>
/// Defines the context for querying content items of a specified type.
/// </summary>
public interface IContentItemContext<T> : IDataContext<T> where T : class, IContentItemFieldsSource, new()
{        

}
