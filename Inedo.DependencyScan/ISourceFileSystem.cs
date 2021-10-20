using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Interface used to allow the dependency scanner to use an abstract file system.
    /// </summary>
    public interface ISourceFileSystem
    {
        /// <summary>
        /// Combines two strings into a path.
        /// </summary>
        /// <param name="path1">The first path to combine.</param>
        /// <param name="path2">The second path to combine.</param>
        /// <returns>
        /// The combined paths. If one of the specified paths is a zero-length string, this
        /// method returns the other path. If <paramref name="path2"/> contains an absolute path, this method
        /// returns <paramref name="path2"/>.
        /// </returns>
        string Combine(string path1, string path2);
        /// <summary>
        /// Returns the directory information for the specified path string.
        /// </summary>
        /// <param name="path">The path of a file or directory.</param>
        /// <returns>
        /// Directory information for path, or null if path denotes a root directory or is
        /// null. Returns <see cref="string.Empty"/> if path does not contain directory information.
        /// </returns>
        string GetDirectoryName(string path);
        /// <summary>
        /// Returns the file name and extension of the specified path string.
        /// </summary>
        /// <param name="path">The path string from which to obtain the file name and extension.</param>
        /// <returns>
        /// The characters after the last directory separator character in <paramref name="path"/>. If the last
        /// character of <paramref name="path"/> is a directory or volume separator character, this method returns
        /// <see cref="string.Empty"/>. If <paramref name="path"/> is null, this method returns null.
        /// </returns>
        string GetFileName(string path);
        /// <summary>
        /// Returns the file name of the specified path string without the extension.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>
        /// The string returned by <see cref="GetFileName(string)"/>,
        /// minus the last period (.) and all characters following it.
        /// </returns>
        string GetFileNameWithoutExtension(string path);
        /// <summary>
        /// Returns a value indicating whether a file exists at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>True if the file exists; otherwise false.</returns>
        Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken);
        /// <summary>
        /// Opens an existing file for reading.
        /// </summary>
        /// <param name="path">The file to be opened for reading.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// A read-only <see cref="Stream"/> on the specified path.
        /// </returns>
        Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken);
    }
}
