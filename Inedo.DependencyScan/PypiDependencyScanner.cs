using System;
using System.Collections.Generic;
using System.IO;

namespace Inedo.DependencyScan
{
    internal sealed class PypiDependencyScanner : DependencyScanner
    {
        public override IReadOnlyCollection<ScannedProject> ResolveDependencies() => new[] { new ScannedProject("PyPiPackage", this.ReadDependencies()) };

        private IEnumerable<DependencyPackage> ReadDependencies()
        {
            foreach (var line in File.ReadLines(this.SourcePath))
            {
                var parts = line.Split(new[] { "==" }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                    yield return new DependencyPackage { Name = parts[0], Version = parts[1] };
            }
        }
    }
}
