using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    internal sealed class NpmDependencyScanner : DependencyScanner
    {
        public override DependencyScannerType Type => DependencyScannerType.Npm;

        public override async Task<IReadOnlyCollection<ScannedProject>> ResolveDependenciesAsync(bool considerProjectReferences = false, CancellationToken cancellationToken = default)
        {
            var packageLockPath = this.FileSystem.Combine(this.FileSystem.GetDirectoryName(this.SourcePath), "package-lock.json");

            using var stream = await this.FileSystem.OpenReadAsync(packageLockPath, cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var projectName = doc.RootElement.GetProperty("name").GetString();
            return new[] { new ScannedProject(projectName, ReadDependencies(doc)) };
        }

        private static IEnumerable<DependencyPackage> ReadDependencies(JsonDocument doc)
        {
            if (!doc.RootElement.TryGetProperty("dependencies", out var dependencies))
                yield break;

            foreach (var d in dependencies.EnumerateObject())
            {
                var version = d.Value.GetProperty("version").GetString();
                yield return new DependencyPackage { Name = d.Name, Version = version };
            }
        }
    }
}
