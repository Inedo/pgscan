using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    internal sealed class NpmDependencyScanner : DependencyScanner
    {
        public override DependencyScannerType Type => DependencyScannerType.Npm;

        public override async Task<IReadOnlyCollection<ScannedProject>> ResolveDependenciesAsync(CancellationToken cancellationToken = default)
        {
            var packageLockPath = this.FileSystem.Combine(this.FileSystem.GetDirectoryName(this.SourcePath), "package-lock.json");

            using var stream = await this.FileSystem.OpenReadAsync(packageLockPath, cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var projectName = doc.RootElement.GetProperty("name").GetString();
            return new[] { new ScannedProject(projectName, ReadDependencies(doc)) };
        }

        private static IEnumerable<DependencyPackage> ReadDependencies(JsonDocument doc)
        {
            if (!doc.RootElement.TryGetProperty("packages", out var npmDependencyPackages))
                if (!doc.RootElement.TryGetProperty("dependencies", out npmDependencyPackages))
                    yield break;

            foreach (var npmDependencyPackage in npmDependencyPackages.EnumerateObject())
            {
                // skip the self reference package
                if (npmDependencyPackage.Name.Equals(string.Empty))
                    continue;

                string name;
                // drop the pre-pended paths, if they exist, to get the name of the package by itself
                var lidx = npmDependencyPackage.Name.LastIndexOf("node_modules/") + 13;
                if (lidx < 13 || lidx >= npmDependencyPackage.Name.Length)
                    name = npmDependencyPackage.Name;
                else
                    name = npmDependencyPackage.Name.Substring(lidx);


                string version = npmDependencyPackage.Value.GetProperty("version").GetString();

                // Check for npm package alias of format 'npm:package-name@package-version'
                if (version.StartsWith("npm:", System.StringComparison.OrdinalIgnoreCase) && version.Contains("@"))
                {
                    // If a npm package alias is used the information about the package is stored in the version-property
                    // The package name starts after 'npm:' and ends at the last occurence of '@'
                    // The package version comes after the last occurence of '@'
                    var separator = version.LastIndexOf('@');
                    name = version.Substring(0, separator).Remove(0, 4);
                    version = version.Substring(separator + 1);
                }

                yield return new DependencyPackage { Name = name, Version = version, Type = "npm" };
            }
        }
        }
    }
}
