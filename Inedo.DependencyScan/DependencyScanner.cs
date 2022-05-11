using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Used to generate a list of dependencies consumed by projects.
    /// </summary>
    public abstract class DependencyScanner
    {
        private protected DependencyScanner()
        {
        }

        /// <summary>
        /// Gets the source path to scan.
        /// </summary>
        public string SourcePath
        {
            get;
#if NET452
            private set;
#else
            private init;
#endif
        }
        /// <summary>
        /// Gets the source file system abstraction used by the scanner.
        /// </summary>
        public ISourceFileSystem FileSystem
        {
            get;
#if NET452
            private set;
#else
            private init;
#endif
        }
        /// <summary>
        /// Gets the type of scanner.
        /// </summary>
        public abstract DependencyScannerType Type { get; }

        /// <summary>
        /// Returns the dependencies used by each project in the specified <see cref="SourcePath"/>.
        /// </summary>
        /// <param name="considerProjectReferences">Determines if project references are considered as package references or not (only relevant for NuGetDependencyScanner).</param>
        /// <param name="cancellationToken">Cancellation token for asynchronous operation.</param>
        /// <returns>Dependencies used by each project.</returns>
        public abstract Task<IReadOnlyCollection<ScannedProject>> ResolveDependenciesAsync(bool considerProjectReferences = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a <see cref="DependencyScanner"/> for the specified path.
        /// </summary>
        /// <param name="sourcePath">Path of projects to scan.</param>
        /// <param name="type">Type of project to scan for.</param>
        /// <param name="fileSystem">Abstract file system used to scan for dependencies. If not specified, the default system file I/O is used.</param>
        /// <returns><see cref="DependencyScanner"/> for the specified path.</returns>
        public static DependencyScanner GetScanner(string sourcePath, DependencyScannerType type = DependencyScannerType.Auto, ISourceFileSystem fileSystem = null)
        {
            var fs = fileSystem ?? SourceFileSystem.Default;
            if (type == DependencyScannerType.Auto)
            {
                type = GetImplicitType(sourcePath);
                if (type == DependencyScannerType.Auto)
                    throw new ArgumentException("Could not automatically determine project type.") { Data = { ["message"] = true } };
            }

            return type switch
            {
                DependencyScannerType.NuGet => Create<NuGetDependencyScanner>(sourcePath, fs),
                DependencyScannerType.Npm => Create<NpmDependencyScanner>(sourcePath, fs),
                DependencyScannerType.PyPI => Create<PypiDependencyScanner>(sourcePath, fs),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private protected async Task<IEnumerable<string>> ReadLinesAsync(string path, CancellationToken cancellationToken)
        {
            return streamLines(new StreamReader(await this.FileSystem.OpenReadAsync(path, cancellationToken).ConfigureAwait(false), Encoding.UTF8));

            static IEnumerable<string> streamLines(StreamReader reader)
            {
                using (reader)
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }

        private static TScanner Create<TScanner>(string sourcePath, ISourceFileSystem fileSystem) where TScanner : DependencyScanner, new() => new() { SourcePath = sourcePath, FileSystem = fileSystem };
        private static DependencyScannerType GetImplicitType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".sln" or ".csproj" => DependencyScannerType.NuGet,
                ".json" => DependencyScannerType.Npm,
                _ => Path.GetFileName(fileName).Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ? DependencyScannerType.PyPI : DependencyScannerType.Auto
            };
        }
    }
}
