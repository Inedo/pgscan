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
            switch (typeName.ToLowerInvariant())
            {
                case "nuget":
                    return new NuGetDependencyScanner();
                case "npm":
                    return new NpmDependencyScanner();
                case "pypi":
                    return new PypiDependencyScanner();
                default:
                    throw new PgScanException($"Invalid scanner type: {typeName} (must be nuget, npm, or pypi)");
            }
        }
    }
}
