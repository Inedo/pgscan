using System.Collections.Generic;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Interface to make certain methods publicly visible that are not implemented by all implementations of DependencyScanner
    /// </summary>
    public interface IConfigurableDependencyScanner
    {
        #region Public Methods
        /// <summary>
        /// Sets arguments provided command line
        /// </summary>
        /// <param name="namedArguments">The dictionary containing the named arguments</param>
        void SetArgs(IReadOnlyDictionary<string, string> namedArguments);

        #endregion Public Methods
    }
}