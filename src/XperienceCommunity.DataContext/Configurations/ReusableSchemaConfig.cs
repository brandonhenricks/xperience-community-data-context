namespace XperienceCommunity.DataContext.Configurations
{
    /// <summary>
    /// Represents the configuration settings for a reusable schema.
    /// </summary>
    public sealed class ReusableSchemaConfig : QueryConfig
    {
        /// <summary>
        /// Gets or sets the schema names for the reusable schema configuration.
        /// </summary>
        public HashSet<string>? SchemaNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include content fields in the reusable schema configuration.
        /// </summary>
        public bool? WithContentFields { get; set; }
    }
}
