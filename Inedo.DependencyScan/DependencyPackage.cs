using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Contains information about a package recognized as a dependency.
    /// </summary>
    public sealed class DependencyPackage
    {
        internal DependencyPackage()
        {
        }

        /// <summary>
        /// Gets the package name.
        /// </summary>
        public string Name
        {
            get;
#if NET452
            internal set;
#else
            internal init;
#endif
        }
        /// <summary>
        /// Gets the package group if applicable; otherwise null.
        /// </summary>
        public string Group
        {
            get;
#if NET452
            internal set;
#else
            internal init;
#endif
        }
        /// <summary>
        /// Gets the package version.
        /// </summary>
        public string Version
        {
            get;
#if NET452
            internal set;
#else
            internal init;
#endif
        }

        /// <summary>
        /// Returns a string representation of the package.
        /// </summary>
        /// <returns>String representation of the pacakge.</returns>
        public override string ToString()
        {
            var name = string.IsNullOrWhiteSpace(this.Group) ? this.Name : (this.Group + "/" + this.Name);
            return $"{name} {this.Version}";
        }

        /// <summary>
        /// Publishes dependency information to a ProGet server.
        /// </summary>
        /// <param name="progetUrl">ProGet server URL.</param>
        /// <param name="packageFeed">Name of the feed to publish to.</param>
        /// <param name="consumer">Package consumer information.</param>
        /// <param name="apiKey">ProGet API key.</param>
        /// <param name="comments">Optional comments to submit.</param>
        public Task PublishDependencyAsync(string progetUrl, string packageFeed, PackageConsumer consumer, string apiKey, string comments = null)
        {
            var client = new ProGetClient(progetUrl);
            return client.RecordPackageDependencyAsync(this, packageFeed, consumer, apiKey, comments);
        }
    }
}
