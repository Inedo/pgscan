namespace Inedo.DependencyScan
{
    /// <summary>
    /// Contains information about the consumer of a <see cref="DependencyPackage"/>.
    /// </summary>
    public sealed class PackageConsumer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageConsumer"/> class.
        /// </summary>
        public PackageConsumer()
        {
        }

        /// <summary>
        /// Gets or sets the consuming package name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the consuming package group if applicable.
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// Gets or sets the consuming package version.
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Gets or sets the consuming package feed.
        /// </summary>
        public string Feed { get; set; }
        /// <summary>
        /// Gets or sets the consuming package URL.
        /// </summary>
        public string Url { get; set; }
    }
}
