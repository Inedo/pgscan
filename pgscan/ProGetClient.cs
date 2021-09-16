using System;
using System.IO;
using System.Net;
using System.Text;
#if NETCOREAPP3_1
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
using JsonPropertyNameAttribute = Newtonsoft.Json.JsonPropertyAttribute;
#endif

namespace Inedo.DependencyScan
{
    public sealed class ProGetClient
    {
        public ProGetClient(string baseUrl)
        {
            this.BaseUrl = baseUrl.TrimEnd('/');
        }

        public string BaseUrl { get; }

        public void RecordPackageDependency(Package package, string feed, PackageConsumer consumer, string apiKey)
        {
            var request = WebRequest.CreateHttp(this.BaseUrl + "/api/dependencies/dependents");
            request.Method = "POST";
            request.ContentType = "application/json";
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("X-ApiKey", apiKey);

            using (var requestStream = request.GetRequestStream())
            {
#if NETCOREAPP3_1
                using (var writer = new Utf8JsonWriter(requestStream))
                {
                    JsonSerializer.Serialize(writer, new DependentPackage(package, feed, consumer), new JsonSerializerOptions
                    {
                        IgnoreNullValues = true
                    });
                }
#else
                using (var writer = new StreamWriter(requestStream, Encoding.UTF8))
                {
                    new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }.Serialize(writer, new DependentPackage(package, feed, consumer));
                }
#endif
            }

            try
            {
                using var response = request.GetResponse();
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse response)
            {
                using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                var message = new char[8192];
                int length = reader.ReadBlock(message, 0, message.Length);
                if (length > 0)
                    throw new PgScanException($"Server responded with {(int)response.StatusCode} {response.StatusDescription}: {new string(message, 0, length)}");
                else
                    throw new PgScanException($"Server responded with {(int)response.StatusCode} {response.StatusDescription}");
            }
        }

        private sealed class DependentPackage
        {
            private readonly Package p;
            private readonly PackageConsumer c;

            public DependentPackage(Package p, string feed, PackageConsumer consumer)
            {
                this.p = p;
                this.c = consumer;
                this.FeedName = feed;
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
        }
    }
}
