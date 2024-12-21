using CMS.Websites;

namespace XperienceCommunity.DataContext.Configurations
{
    public sealed class PageQueryConfig : QueryConfig
    {
        /// <summary>
        /// Gets or sets the path match for the query.
        /// </summary>
        public PathMatch? PathMatch { get; set; }

        /// <summary>
        /// Gets or sets the channel name for the query.
        /// </summary>
        public string? ChannelName { get; set; }
    }
}
