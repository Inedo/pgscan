using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Contains the default source file system.
    /// </summary>
    public static class SourceFileSystem
    {
        /// <summary>
        /// Gets the default source file system.
        /// </summary>
        public static ISourceFileSystem Default { get; } = new DefaultFileSystem();

        private sealed class DefaultFileSystem : ISourceFileSystem
        {
            public string Combine(string path1, string path2) => Path.Combine(path1, path2);
            public ValueTask<bool> FileExistsAsync(string path, CancellationToken cancellationToken) => new(File.Exists(path));
            public string GetDirectoryName(string path) => Path.GetDirectoryName(path);
            public string GetFileName(string path) => Path.GetFileName(path);
            public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);
            public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken) => Task.FromResult<Stream>(File.OpenRead(path));
            public IAsyncEnumerable<SimpleFileInfo> FindFilesAsync(string path, string filter, bool recursive, CancellationToken cancellationToken)
            {
                return new DirectoryInfo(path)
                    .EnumerateFiles(filter, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Select(f => new SimpleFileInfo(f.FullName, f.LastWriteTimeUtc))
                    .ToAsyncEnumerable();
            }
        }
    }
}
