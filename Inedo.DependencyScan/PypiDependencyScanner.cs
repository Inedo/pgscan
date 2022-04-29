using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    internal sealed class PypiDependencyScanner : DependencyScanner
    {
        public override DependencyScannerType Type => DependencyScannerType.PyPI;

        public override async Task<IReadOnlyCollection<ScannedProject>> ResolveDependenciesAsync(bool considerProjectReferences = false, CancellationToken cancellationToken = default)
        {
            return new[] { new ScannedProject("PyPiPackage", await this.ReadDependenciesAsync(cancellationToken).ConfigureAwait(false)) };
        }

        private async Task<IEnumerable<DependencyPackage>> ReadDependenciesAsync(CancellationToken cancellationToken)
        {
            return enumeratePackages(await this.ReadLinesAsync(this.SourcePath, cancellationToken).ConfigureAwait(false));

            static IEnumerable<DependencyPackage> enumeratePackages(IEnumerable<string> lines)
            {
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { "==" }, 2, StringSplitOptions.None);
                    if (parts.Length == 2)
                        yield return new DependencyPackage { Name = parts[0], Version = parts[1] };
                }
            }
        }
    }
}
