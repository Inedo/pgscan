using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    public sealed class ProGetClient
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
        public async Task PublishSbomAsync(IEnumerable<ScannedProject> projects, PackageConsumer consumer, string consumerType, string packageType, string apiKey)
        {
            var request = WebRequest.CreateHttp(this.BaseUrl + "/api/sca/import");
            request.Method = "POST";
            request.ContentType = "text/xml";
            request.UseDefaultCredentials = true;
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("X-ApiKey", apiKey);

            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                using var bomWriter = new BomWriter(requestStream);
                bomWriter.Begin(consumer.Group, consumer.Name, consumer.Version, consumerType);

                foreach (var p in projects)
                {
                    foreach (var d in p.Dependencies)
                        bomWriter.AddPackage(d.Group, d.Name, d.Version, d.Type ?? packageType);
                }
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

        private async Task<Version> RecordPackageDependencyInternalAsync(DependencyPackage package, string feed, PackageConsumer consumer, string apiKey, string comments)
        {
            var request = WebRequest.CreateHttp(this.BaseUrl + "/api/dependencies/dependents");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.UseDefaultCredentials = true;
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("X-ApiKey", apiKey);

            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                JsonSerializer.Serialize(
                    requestStream,
                    new DependentPackage(package, feed, consumer, comments),
#if NET6_0_OR_GREATER
                    JsonContext.Default.DependentPackage
#else
                    new JsonSerializerOptions { IgnoreNullValues = true }
#endif
                );
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
            request.UseDefaultCredentials = true;
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("X-ApiKey", apiKey);

            using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                JsonSerializer.Serialize(
                    requestStream,
                    packages.Select(p => new DependentPackage(p, feed, consumer, comments)),
#if NET6_0_OR_GREATER
                    JsonContext.Default.IEnumerableDependentPackage
#else
                    new JsonSerializerOptions { IgnoreNullValues = true }
#endif
                );
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
    }
}
