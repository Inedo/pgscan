using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
#if !NET452
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
using JsonPropertyNameAttribute = Newtonsoft.Json.JsonPropertyAttribute;
#endif

namespace Inedo.DependencyScan
{
    internal sealed class ProGetClient
    {
        private static readonly Version MinBatchServerVersion = new(6, 0, 11);

        public ProGetClient(string baseUrl)
        {
            this.BaseUrl = baseUrl.TrimEnd('/');
        }

        public string BaseUrl { get; }

        public Task RecordPackageDependencyAsync(DependencyPackage package, string feed, PackageConsumer consumer, string apiKey, string comments = null) => this.RecordPackageDependencyInternalAsync(package, feed, consumer, apiKey, comments);

        public async Task RecordPackageDependenciesAsync(IEnumerable<DependencyPackage> packages, string feed, PackageConsumer consumer, string apiKey, string comments = null)
        {
            Version serverVersion = null;

            var remainingPackages = new List<DependencyPackage>();

            foreach (var package in packages)
            {
                if (serverVersion == null || serverVersion < MinBatchServerVersion)
                    serverVersion = await this.RecordPackageDependencyInternalAsync(package, feed, consumer, apiKey, comments).ConfigureAwait(false);
                else
                    remainingPackages.Add(package);
            }

            if (remainingPackages.Count > 0)
                await this.RecordPackageDependenciesInternalAsync(remainingPackages, feed, consumer, apiKey, comments).ConfigureAwait(false);
        }

        private async Task<Version> RecordPackageDependencyInternalAsync(DependencyPackage package, string feed, PackageConsumer consumer, string apiKey, string comments)
        {
            var request = WebRequest.CreateHttp(this.BaseUrl + "/api/dependencies/dependents");
            request.Method = "POST";
            request.ContentType = "application/json";
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("X-ApiKey", apiKey);

            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
#if !NET452
                using var writer = new Utf8JsonWriter(requestStream);
                JsonSerializer.Serialize(writer, new DependentPackage(package, feed, consumer, comments), new JsonSerializerOptions
                {
                    IgnoreNullValues = true
                });
#else
                using (var writer = new StreamWriter(requestStream, Encoding.UTF8))
                {
                    new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }.Serialize(writer, new DependentPackage(package, feed, consumer, comments));
                }
#endif
            }

            try
            {
                using var response = await request.GetResponseAsync().ConfigureAwait(false);
                if (Version.TryParse(response.Headers["X-ProGet-Version"], out var version))
                    return version;
                else
                    return null;
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse response)
            {
                using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                var message = new char[8192];
                int length = reader.ReadBlock(message, 0, message.Length);
                if (length > 0)
                    throw new InvalidOperationException($"Server responded with {(int)response.StatusCode} {response.StatusDescription}: {new string(message, 0, length)}") { Data = { ["message"] = true } };
                else
                    throw new InvalidOperationException($"Server responded with {(int)response.StatusCode} {response.StatusDescription}") { Data = { ["message"] = true } };
            }
        }

        private async Task RecordPackageDependenciesInternalAsync(List<DependencyPackage> packages, string feed, PackageConsumer consumer, string apiKey, string comments)
        {
            var request = WebRequest.CreateHttp(this.BaseUrl + "/api/dependencies/dependents");
            request.Method = "POST";
            request.ContentType = "application/json";

            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
#if !NET452
                using var writer = new Utf8JsonWriter(requestStream);
                JsonSerializer.Serialize(
                    writer,
                    packages.Select(p => new DependentPackage(p, feed, consumer, comments)),
                    new JsonSerializerOptions { IgnoreNullValues = true }
                );
#else
                using var writer = new StreamWriter(requestStream, Encoding.UTF8);
                new JsonSerializer { NullValueHandling = NullValueHandling.Ignore }.Serialize(writer, packages.Select(p => new DependentPackage(p, feed, consumer, comments)));
#endif
            }

            try
            {
                using var response = await request.GetResponseAsync().ConfigureAwait(false);
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse response)
            {
                using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                var message = new char[8192];
                int length = reader.ReadBlock(message, 0, message.Length);
                if (length > 0)
                    throw new InvalidOperationException($"Server responded with {(int)response.StatusCode} {response.StatusDescription}: {new string(message, 0, length)}") { Data = { ["message"] = true } };
                else
                    throw new InvalidOperationException($"Server responded with {(int)response.StatusCode} {response.StatusDescription}") { Data = { ["message"] = true } };
            }
        }

        private sealed class DependentPackage
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
            public object DependentPackageUrl => this.c.Url;
            [JsonPropertyName("comments")]
            public string Comments { get; }
        }
    }
}
