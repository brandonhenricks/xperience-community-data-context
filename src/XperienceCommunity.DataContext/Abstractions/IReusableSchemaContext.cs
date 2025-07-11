namespace XperienceCommunity.DataContext.Abstractions;

/// <summary>
/// Represents a reusable schema context that provides methods for working with reusable schemas.
/// </summary>
/// <typeparam name="T">The type of the data context.</typeparam>
public interface IReusableSchemaContext<T> : IDataContext<T>
{
    /// <summary>
    /// Specifies that the data context should include content type fields.
    /// </summary>
    /// <returns>The data context with content type fields included.</returns>
    IDataContext<T> WithContentTypeFields();

    /// <summary>
    /// Specifies that the data context should include web page data.
    /// </summary>
    /// <returns>The data context with web page data included.</returns>
    IDataContext<T> WithWebPageData();
}
