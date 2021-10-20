namespace Inedo.DependencyScan
{
    /// <summary>
    /// Specifies the type of project to scan.
    /// </summary>
    public enum DependencyScannerType
    {
        /// <summary>
        /// Automatically determine the type based on the file name.
        /// </summary>
        Auto,
        /// <summary>
        /// NuGet.
        /// </summary>
        NuGet,
        /// <summary>
        /// npm.
        /// </summary>
        Npm,
        /// <summary>
        /// PyPI.
        /// </summary>
        PyPI
    }
}
