using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Threading;
#if !NET452
using System.Text.Json;
#else
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace Inedo.DependencyScan
{
    internal sealed class NuGetDependencyScanner : DependencyScanner
    {
        private static readonly Regex SolutionProjectRegex = new(@"^Project[^=]*=\s*""[^""]+""\s*,\s*""(?<1>[^""]+)""", RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        public override DependencyScannerType Type => DependencyScannerType.NuGet;

        public override async Task<IReadOnlyCollection<ScannedProject>> ResolveDependenciesAsync(bool considerProjectReferences = false, CancellationToken cancellationToken = default)
        {
            if (this.SourcePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                var projects = new List<ScannedProject>();

                var solutionRoot = this.FileSystem.GetDirectoryName(this.SourcePath);

                foreach (var p in await ReadProjectsFromSolutionAsync(this.SourcePath, cancellationToken).ConfigureAwait(false))
                {
                    var projectPath = this.FileSystem.Combine(solutionRoot, p);
                    projects.Add(new ScannedProject(this.FileSystem.GetFileNameWithoutExtension(p), await ReadProjectDependenciesAsync(projectPath, considerProjectReferences, cancellationToken).ConfigureAwait(false)));
                }

                return projects;
            }
            else
            {
                return new[]
                {
                    new ScannedProject(this.FileSystem.GetFileNameWithoutExtension(this.SourcePath), await ReadProjectDependenciesAsync(this.SourcePath, considerProjectReferences, cancellationToken).ConfigureAwait(false))
                };
            }
        }

        private async Task<IEnumerable<string>> ReadProjectsFromSolutionAsync(string solutionPath, CancellationToken cancellationToken)
        {
            return (await this.ReadLinesAsync(solutionPath, cancellationToken).ConfigureAwait(false))
                .Select(l => SolutionProjectRegex.Match(l))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value);
        }

        private async Task<IEnumerable<DependencyPackage>> ReadProjectDependenciesAsync(string projectPath, bool considerProjectReferences, CancellationToken cancellationToken)
        {
            var projectDir = this.FileSystem.GetDirectoryName(projectPath);
            var packagesConfigPath = this.FileSystem.Combine(projectDir, "packages.config");
            if (await this.FileSystem.FileExistsAsync(packagesConfigPath, cancellationToken).ConfigureAwait(false))
                return await ReadPackagesConfigAsync(packagesConfigPath, cancellationToken).ConfigureAwait(false);

            var assetsPath = this.FileSystem.Combine(this.FileSystem.Combine(projectDir, "obj"), "project.assets.json");
            if (await this.FileSystem.FileExistsAsync(assetsPath, cancellationToken).ConfigureAwait(false))
                return await ReadProjectAssetsAsync(assetsPath, considerProjectReferences, cancellationToken).ConfigureAwait(false);

            return Enumerable.Empty<DependencyPackage>();
        }

        private async Task<IEnumerable<DependencyPackage>> ReadPackagesConfigAsync(string packagesConfigPath, CancellationToken cancellationToken)
        {
            var xdoc = XDocument.Load(await this.FileSystem.OpenReadAsync(packagesConfigPath, cancellationToken).ConfigureAwait(false));
            return enumeratePackages(xdoc);

            static IEnumerable<DependencyPackage> enumeratePackages(XDocument xdoc)
            {
                var packages = xdoc.Element("packages")?.Elements("package");
                if (packages == null)
                    yield break;

                foreach (var p in packages)
                {
                    yield return new DependencyPackage
                    {
                        Name = (string)p.Attribute("id"),
                        Version = (string)p.Attribute("version")
                    };
                }
            }
        }

#if !NET452
        private async Task<IEnumerable<DependencyPackage>> ReadProjectAssetsAsync(string projectAssetsPath, bool considerProjectReferences, CancellationToken cancellationToken)
        {
            JsonDocument jdoc;
            using (var stream = await this.FileSystem.OpenReadAsync(projectAssetsPath, cancellationToken).ConfigureAwait(false))
            {
                jdoc = JsonDocument.Parse(stream);
            }

            return enumeratePackages(jdoc, considerProjectReferences);

            static IEnumerable<DependencyPackage> enumeratePackages(JsonDocument jdoc, bool considerProjectReferences)
            {
                var libraries = jdoc.RootElement.GetProperty("libraries");
                if (libraries.ValueKind == JsonValueKind.Object)
                {
                    foreach (var library in libraries.EnumerateObject())
                    {
                        if (library.Value.GetProperty("type").ValueEquals("package") || (library.Value.GetProperty("type").ValueEquals("project") && considerProjectReferences))
                        {
                            var parts = library.Name.Split(new[] { '/' }, 2);

                            yield return new DependencyPackage
                            {
                                Name = parts[0],
                                Version = parts[1]
                            };
                        }
                    }
                }
            }
        }
#else
        private async Task<IEnumerable<DependencyPackage>> ReadProjectAssetsAsync(string projectAssetsPath, bool considerProjectReferences, CancellationToken cancellationToken)
        {
            JObject jdoc;
            using (var reader = new JsonTextReader(new StreamReader(await this.FileSystem.OpenReadAsync(projectAssetsPath, cancellationToken).ConfigureAwait(false), Encoding.UTF8)))
            {
                jdoc = JObject.Load(reader);
            }

            return enumeratePackages(jdoc, considerProjectReferences);

            static IEnumerable<DependencyPackage> enumeratePackages(JObject jdoc, bool considerProjectReferences)
            {
                if (jdoc["libraries"] is JObject libraries)
                {
                    foreach (var library in libraries.Properties())
                    {
                        if ((string)((JObject)library.Value).Property("type") == "package" || ((string)((JObject)library.Value).Property("type") == "project" && considerProjectReferences))
                        {
                            var parts = library.Name.Split(new[] { '/' }, 2);
    
                            yield return new DependencyPackage
                            {
                                Name = parts[0],
                                Version = parts[1]
                            };
                        }
                    }
                }
            }
        }
#endif
    }
}
