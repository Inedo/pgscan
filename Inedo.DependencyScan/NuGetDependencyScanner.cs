using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public override async Task<IReadOnlyCollection<ScannedProject>> ResolveDependenciesAsync(bool considerProjectReferences = false, bool scanForChildNpmDependencies = true, CancellationToken cancellationToken = default)
        {
            if (this.SourcePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                var projects = new List<ScannedProject>();

                var solutionRoot = this.FileSystem.GetDirectoryName(this.SourcePath);

                var assets = await this.FindAllAssetsAsync(solutionRoot, cancellationToken).ConfigureAwait(false);

                await foreach (var p in ReadProjectsFromSolutionAsync(this.SourcePath, cancellationToken).ConfigureAwait(false))
                {
                    var projectPath = this.FileSystem.Combine(solutionRoot, p);
                    IEnumerable<DependencyPackage> packages;

                    if (assets.TryGetValue(projectPath, out var a))
                    {
                        packages = a.Packages;
                        if (considerProjectReferences)
                            packages = packages.Concat(a.Projects);
                    }
                    else
                    {
                        packages = await ReadProjectDependenciesAsync(projectPath, considerProjectReferences, cancellationToken).ToListAsync().ConfigureAwait(false);
                    }
                    if (scanForChildNpmDependencies)
                        packages = packages.Concat(await this.FindNpmPackagesAsync(this.FileSystem.GetDirectoryName(projectPath), cancellationToken).ToListAsync().ConfigureAwait(false));
                    projects.Add(
                        new ScannedProject(
                            this.FileSystem.GetFileNameWithoutExtension(p),
                            packages
                        )
                    );
                }

                return projects;
            }
            else
            {
                var packages = await ReadProjectDependenciesAsync(this.SourcePath, considerProjectReferences, cancellationToken).ToListAsync().ConfigureAwait(false);

                if (scanForChildNpmDependencies)
                    packages = packages.Concat(await this.FindNpmPackagesAsync(this.FileSystem.GetDirectoryName(this.SourcePath), cancellationToken).ToListAsync().ConfigureAwait(false)).ToList();

                return new[]
                {
                    new ScannedProject(
                        this.FileSystem.GetFileNameWithoutExtension(this.SourcePath), 
                        packages
                    )
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
        private async Task<Dictionary<string, ProjectAssets>> FindAllAssetsAsync(string solutionPath, CancellationToken cancellationToken)
        {
            var projects = new Dictionary<string, ProjectAssets>(StringComparer.OrdinalIgnoreCase);

            await foreach (var f in this.FileSystem.FindFilesAsync(solutionPath, "project.assets.json", true, cancellationToken).ConfigureAwait(false))
            {
                if (Path.GetFileName(f.FullName) == "project.assets.json")
                {
                    var assets = await ProjectAssets.ReadAsync(this.FileSystem, f, cancellationToken).ConfigureAwait(false);
                    if (assets != null)
                    {
                        if (!projects.TryGetValue(assets.ProjectPath, out var oldAssets) || oldAssets.SourceFile.LastModified < f.LastModified)
                            projects[assets.ProjectPath] = assets;
                    }
                }
            }

            return projects;
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
                SimpleFileInfo newest = null;

                await foreach (var f in this.FileSystem.FindFilesAsync(projectDir, "projects.assets.json", true, cancellationToken).ConfigureAwait(false))
                {
                    if (Path.GetFileName(f.FullName) == "projects.assets.json" && (newest == null || newest.LastModified < f.LastModified))
                        newest = f;
                }

                var assetsPath = this.FileSystem.Combine(this.FileSystem.Combine(projectDir, "obj"), "project.assets.json");
                if (await this.FileSystem.FileExistsAsync(assetsPath, cancellationToken).ConfigureAwait(false))
                    packages = ReadProjectAssetsAsync(assetsPath, considerProjectReferences, cancellationToken);
            }

            if (packages != null)
            {
                await foreach (var p in packages.ConfigureAwait(false))
                    yield return p;
            }

            
        }

        private async IAsyncEnumerable<DependencyPackage> FindNpmPackagesAsync(string projectPath, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach(var npmProjectFile in this.FileSystem.FindFilesAsync(projectPath, "package-lock.json", true, cancellationToken))
            {
                var npmScanner = DependencyScanner.GetScanner(npmProjectFile.FullName, DependencyScannerType.Npm, this.FileSystem);
                foreach(var npmProject in await npmScanner.ResolveDependenciesAsync(cancellationToken: cancellationToken))
                {
                    foreach(var npmDependency in npmProject.Dependencies)
                        yield return npmDependency;
                }
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
                    Version = (string)p.Attribute("version"),
                    Type = "nuget"
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
                            Version = parts[1],
                            Type = "nuget"
                        };
                    }
                }
            }
        }

        private sealed class ProjectAssets
        {
            private ProjectAssets(SimpleFileInfo sourceFile, string projectName, string projectPath, IReadOnlyCollection<DependencyPackage> packages, IReadOnlyCollection<DependencyPackage> projects)
            {
                this.SourceFile = sourceFile;
                this.ProjectName = projectName;
                this.ProjectPath = projectPath;
                this.Packages = packages;
                this.Projects = projects;
            }

            public SimpleFileInfo SourceFile { get; }
            public string ProjectName { get; }
            public string ProjectPath { get; }
            public IReadOnlyCollection<DependencyPackage> Packages { get; }
            public IReadOnlyCollection<DependencyPackage> Projects { get; }

            public static async Task<ProjectAssets> ReadAsync(ISourceFileSystem fileSystem, SimpleFileInfo file, CancellationToken cancellationToken)
            {
                using var stream = await fileSystem.OpenReadAsync(file.FullName, cancellationToken).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                if (!doc.RootElement.TryGetProperty("project", out var project) || project.ValueKind != JsonValueKind.Object)
                    return null;

                if (!project.TryGetProperty("restore", out var restore) || restore.ValueKind != JsonValueKind.Object)
                    return null;

                if (!restore.TryGetProperty("projectName", out var name) || name.ValueKind != JsonValueKind.String)
                    return null;

                if (!restore.TryGetProperty("projectPath", out var path) || path.ValueKind != JsonValueKind.String)
                    return null;

                var packages = new List<DependencyPackage>();
                var projectPackages = new List<DependencyPackage>();

                if (doc.RootElement.TryGetProperty("libraries", out var libraries) && libraries.ValueKind == JsonValueKind.Object)
                {
                    foreach (var library in libraries.EnumerateObject())
                    {
                        var type = library.Value.GetProperty("type");
                        bool isPackage = type.ValueEquals("package");
                        if (isPackage || type.ValueEquals("project"))
                        {
                            var parts = library.Name.Split(new[] { '/' }, 2);
                            (isPackage ? packages : projectPackages).Add(
                                new DependencyPackage
                                {
                                    Name = parts[0],
                                    Version = parts[1],
                                    Type = "nuget"
                                }
                            );
                        }
                    }
                }

                return new ProjectAssets(file, name.GetString(), path.GetString(), packages, projectPackages);
            }
        }
    }
}
