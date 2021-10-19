namespace Inedo.DependencyScan
{
    /// <summary>
    /// Specifies the type of project to scan.
    /// </summary>
    public enum DependencyScannerType
    {
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
