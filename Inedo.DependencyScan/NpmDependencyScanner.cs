using System.Collections.Generic;
using System.Linq;
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
            return new[] { new ScannedProject(projectName, ReadPackageLockFile(doc).Distinct()) };
        }

        private static IEnumerable<DependencyPackage> ReadPackageLockFile(JsonDocument doc)
        {
            // works for file format version 2 & 3 (and probably later versions as well)
            if (doc.RootElement.TryGetProperty("packages", out var npmDependencyPackages))
                return ReadPackages(npmDependencyPackages);

            // legacy implementation for file format versions 1 & 2
            return ReadDependencies(doc.RootElement);
        }

        /// <summary>
        /// Read package-lock.json file format 2 and 3
        /// </summary>
        /// <param name="npmDependencyPackages"></param>
        /// <returns></returns>
        private static IEnumerable<DependencyPackage> ReadPackages(JsonElement npmDependencyPackages)
        {
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

                // check for package alias
                if (npmDependencyPackage.Value.TryGetProperty("name", out var alias) && alias.ValueKind == JsonValueKind.String)
                    name = alias.GetString();

                string version = npmDependencyPackage.Value.GetProperty("version").GetString();

                // return dependency
                yield return new DependencyPackage { Name = name, Version = version, Type = "npm" };
            }

        }

        /// <summary>
        /// Read package-lock.json file format 1 (and 2, which is backwards-compatible to 1)
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private static IEnumerable<DependencyPackage> ReadDependencies(JsonElement doc)
        {
            // for recursive calls: if for any reason npmDependencyPackage.Value is a primitive type instead of an object, yield nothing
            if (doc.ValueKind != JsonValueKind.Object)
                yield break;

            // get "dependencies" property
            if (!doc.TryGetProperty("dependencies", out var npmDependencyPackages))
                    yield break;

            foreach (var npmDependencyPackage in npmDependencyPackages.EnumerateObject())
            {
                // skip the self reference package
                if (npmDependencyPackage.Name.Equals(string.Empty))
                    continue;

                string name = npmDependencyPackage.Name;

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

                // return dependency
                yield return new DependencyPackage { Name = name, Version = version, Type = "npm" };

                // check for sub-dependencies recursively
                foreach (var subDependency in ReadDependencies(npmDependencyPackage.Value))
                    yield return subDependency;
            }
        }
    }
}
