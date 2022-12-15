using System.Text.Json.Serialization;

namespace Inedo.DependencyScan
{
    internal sealed class DependentPackage
    {
        private readonly DependencyPackage p;
        private readonly PackageConsumer c;

        public DependentPackage(DependencyPackage p, string feed, PackageConsumer consumer, string comments)
        {
            this.p = p;
            this.c = consumer;
            this.FeedName = feed;
            this.Comments = comments;
        }

        [JsonPropertyName("feed")]
        public string FeedName { get; }
        [JsonPropertyName("packageName")]
        public string PackageName => this.p.Name;
        [JsonPropertyName("groupName")]
        public string PackageGroup => this.p.Group ?? string.Empty;
        [JsonPropertyName("version")]
        public string Version => this.p.Version;
        [JsonPropertyName("dependentPackageName")]
        public string DependentPackageName => this.c.Name;
        [JsonPropertyName("dependentGroupName")]
        public string DependentPackageGroup => this.c.Group ?? string.Empty;
        [JsonPropertyName("dependentVersion")]
        public string DependentPackageVersion => this.c.Version;
        [JsonPropertyName("dependentFeed")]
        public string DependentPackageFeed => this.c.Feed;
        [JsonPropertyName("dependentUrl")]
        public string DependentPackageUrl => this.c.Url;
        [JsonPropertyName("comments")]
        public string Comments { get; }
    }
}
