using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    internal sealed class PypiDependencyScanner : DependencyScanner
    {
        public override DependencyScannerType Type => DependencyScannerType.PyPI;

        public override async Task<IReadOnlyCollection<ScannedProject>> ResolveDependenciesAsync(bool considerProjectReferences = false, bool scanForChildNpmDependencies = true, CancellationToken cancellationToken = default)
        {
            return new[] { new ScannedProject("PyPiPackage", await this.ReadDependenciesAsync(cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false)) };
        }

        private async IAsyncEnumerable<DependencyPackage> ReadDependenciesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var line in this.ReadLinesAsync(this.SourcePath, cancellationToken).ConfigureAwait(false))
            {
                var parts = line.Split(new[] { "==" }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                    yield return new DependencyPackage { Name = parts[0], Version = parts[1] };
            }
        }
    }
}
