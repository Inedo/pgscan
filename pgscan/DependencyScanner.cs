using System.Collections.Generic;

namespace Inedo.DependencyScan
{
    public abstract class DependencyScanner
    {
        protected DependencyScanner()
        {
        }

        public string SourcePath { get; set; }

        public abstract IReadOnlyCollection<Project> ResolveDependencies();

        public static DependencyScanner GetScanner(string typeName)
        {
            return typeName.ToLowerInvariant() switch
            {
                "nuget" => new NuGetDependencyScanner(),
                "npm" => new NpmDependencyScanner(),
                "pypi" => new PypiDependencyScanner(),
                _ => throw new PgScanException($"Invalid scanner type: {typeName} (must be nuget, npm, or pypi)")
            };
        }
    }
}
