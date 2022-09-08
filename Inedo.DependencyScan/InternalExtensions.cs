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
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new EnumerableWrapper<T>(source);
        }

        private sealed class EnumerableWrapper<T> : IAsyncEnumerable<T>
        {
            private readonly IEnumerable<T> source;

            public EnumerableWrapper(IEnumerable<T> source) => this.source = source;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new EnumeratorWrapper(this.source.GetEnumerator());

            private sealed class EnumeratorWrapper : IAsyncEnumerator<T>
            {
                private readonly IEnumerator<T> enumerator;

                public EnumeratorWrapper(IEnumerator<T> enumerator) => this.enumerator = enumerator;

                public T Current => this.enumerator.Current;

                public ValueTask DisposeAsync()
                {
                    this.enumerator.Dispose();
                    return default;
                }
                public ValueTask<bool> MoveNextAsync() => new(this.enumerator.MoveNext());
            }
        }
    }
}
