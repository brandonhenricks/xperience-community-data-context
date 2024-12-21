namespace XperienceCommunity.DataContext.Configurations
{
    /// <summary>
    /// Represents the configuration settings for a query.
    /// </summary>
    public class QueryConfig
    {
        /// <summary>
        /// Gets or sets the content type for the query.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the column names to be included in the query.
        /// </summary>
        public HashSet<string>? ColumnNames { get; set; }

        /// <summary>
        /// Gets or sets the depth of linked items to be included in the query.
        /// </summary>
        public int? LinkedItemsDepth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include the total count in the query result.
        /// </summary>
        public bool? IncludeTotalCount { get; set; }

        /// <summary>
        /// Gets or sets the offset for the query in the form of a tuple (start, end).
        /// </summary>
        public (int?, int?) Offset { get; set; }

        /// <summary>
        /// Gets or sets the language for the query.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use fallback language if the specified language is not available.
        /// </summary>
        public bool? UseFallBack { get; set; }
    }
}
