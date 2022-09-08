using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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

                await foreach (var p in ReadProjectsFromSolutionAsync(this.SourcePath, cancellationToken).ConfigureAwait(false))
                {
                    var projectPath = this.FileSystem.Combine(solutionRoot, p);
                    projects.Add(new ScannedProject(this.FileSystem.GetFileNameWithoutExtension(p), await ReadProjectDependenciesAsync(projectPath, considerProjectReferences, cancellationToken).ToListAsync().ConfigureAwait(false)));
                }

                return projects;
            }
            else
            {
                return new[]
                {
                    new ScannedProject(this.FileSystem.GetFileNameWithoutExtension(this.SourcePath), await ReadProjectDependenciesAsync(this.SourcePath, considerProjectReferences, cancellationToken).ToListAsync().ConfigureAwait(false))
                };
            }
        }

        private async IAsyncEnumerable<string> ReadProjectsFromSolutionAsync(string solutionPath, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var l in this.ReadLinesAsync(solutionPath, cancellationToken).ConfigureAwait(false))
            {
                var m = SolutionProjectRegex.Match(l);
                if (m.Success)
                    yield return m.Groups[1].Value;
            }
        }

        private async IAsyncEnumerable<DependencyPackage> ReadProjectDependenciesAsync(string projectPath, bool considerProjectReferences, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IAsyncEnumerable<DependencyPackage> packages = null;
            var projectDir = this.FileSystem.GetDirectoryName(projectPath);
            var packagesConfigPath = this.FileSystem.Combine(projectDir, "packages.config");
            if (await this.FileSystem.FileExistsAsync(packagesConfigPath, cancellationToken).ConfigureAwait(false))
                packages = ReadPackagesConfigAsync(packagesConfigPath, cancellationToken);

            if (packages == null)
            {
                var assetsPath = this.FileSystem.Combine(this.FileSystem.Combine(projectDir, "obj"), "project.assets.json");
                if (await this.FileSystem.FileExistsAsync(assetsPath, cancellationToken).ConfigureAwait(false))
                    packages = ReadProjectAssetsAsync(assetsPath, considerProjectReferences, cancellationToken);
            }

            if (packages != null)
            {
                await foreach (var p in packages)
                    yield return p;
            }
        }

        private async IAsyncEnumerable<DependencyPackage> ReadPackagesConfigAsync(string packagesConfigPath, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var stream = await this.FileSystem.OpenReadAsync(packagesConfigPath, cancellationToken).ConfigureAwait(false);
            var xdoc = XDocument.Load(stream);

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

        private async IAsyncEnumerable<DependencyPackage> ReadProjectAssetsAsync(string projectAssetsPath, bool considerProjectReferences, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var stream = await this.FileSystem.OpenReadAsync(projectAssetsPath, cancellationToken).ConfigureAwait(false);
            using var jdoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

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
}
