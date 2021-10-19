using System;
using System.Collections.Generic;

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
        /// Returns the dependencies used by each project in the specified <see cref="SourcePath"/>.
        /// </summary>
        /// <returns>Dependencies used by each project.</returns>
        public abstract IReadOnlyCollection<ScannedProject> ResolveDependencies();

        /// <summary>
        /// Returns a <see cref="DependencyScanner"/> for the specified path.
        /// </summary>
        /// <param name="sourcePath">Path of projects to scan.</param>
        /// <param name="type">Type of project to scan for.</param>
        /// <returns><see cref="DependencyScanner"/> for the specified path.</returns>
        public static DependencyScanner GetScanner(string sourcePath, DependencyScannerType type)
        {
            return type switch
            {
                DependencyScannerType.NuGet => Create<NuGetDependencyScanner>(sourcePath),
                DependencyScannerType.Npm => Create<NpmDependencyScanner>(sourcePath),
                DependencyScannerType.PyPI => Create<PypiDependencyScanner>(sourcePath),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private static TScanner Create<TScanner>(string sourcePath) where TScanner : DependencyScanner, new() => new() { SourcePath = sourcePath };
    }
}
