using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    internal static class InternalExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var list = new List<T>();

            await foreach (var item in source.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                list.Add(item);
            }

            return list;
        }
    }
}
