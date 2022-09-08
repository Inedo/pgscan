using System;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Contains basic information about a file.
    /// </summary>
    public sealed class SimpleFileInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleFileInfo"/> class.
        /// </summary>
        /// <param name="fullName">Full path of the file.</param>
        /// <param name="lastModified">Last modified date of the file.</param>
        public SimpleFileInfo(string fullName, DateTimeOffset lastModified)
        {
            this.FullName = fullName;
            this.LastModified = lastModified;
        }

        /// <summary>
        /// Gets the full path of the file.
        /// </summary>
        public string FullName { get; }
        /// <summary>
        /// Gets the last modified date of the file.
        /// </summary>
        public DateTimeOffset LastModified { get; }
    }
}
