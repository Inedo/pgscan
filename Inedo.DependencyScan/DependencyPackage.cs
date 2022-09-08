using System.Collections.Generic;
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
#if !NETCOREAPP
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
#if !NETCOREAPP
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
#if !NETCOREAPP
            internal set;
#else
            internal init;
#endif
        }

        /// <summary>
        /// Publishes dependency information to a ProGet server.
        /// </summary>
        /// <param name="packages">Dependencies packages.</param>
        /// <param name="progetUrl">ProGet server URL.</param>
        /// <param name="packageFeed">Name of the feed to publish to.</param>
        /// <param name="consumer">Package consumer information.</param>
        /// <param name="apiKey">ProGet API key.</param>
        /// <param name="comments">Optional comments to submit.</param>
        public static Task PublishDependenciesAsync(IEnumerable<DependencyPackage> packages, string progetUrl, string packageFeed, PackageConsumer consumer, string apiKey, string comments = null)
        {
            var client = new ProGetClient(progetUrl);
            return client.RecordPackageDependenciesAsync(packages, packageFeed, consumer, apiKey, comments);
        }

        /// <summary>Serves as a hash function (used in dictionaries and hash sets).</summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        ///   <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
        public override bool Equals(object obj)
        {
            // check if other object is DependencyPackage and not null
            var package = obj as DependencyPackage;
            if (package == null)
                return false;

            // check Group, Name & Version for equality
            return Group == package.Group
                && Name == package.Name
                && Version == package.Version;
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
