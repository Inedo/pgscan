using System;
using System.Collections.Generic;
using System.IO;

namespace Inedo.DependencyScan
{
    public sealed class PypiDependencyScanner : DependencyScanner
    {
        public override IReadOnlyCollection<Project> ResolveDependencies()
        {
            return new[] { new Project("PyPiPackage", this.ReadDependencies()) };
        }

        private IEnumerable<Package> ReadDependencies()
        {
            foreach (var line in File.ReadLines(this.SourcePath))
            {
                var parts = line.Split(new[] { "==" }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                    yield return new Package { Name = parts[0], Version = parts[1] };
            }
        }
    }
}
