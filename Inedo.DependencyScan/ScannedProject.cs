using System.Collections.Generic;
using System.Linq;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Contains information about a project file.
    /// </summary>
    public sealed class ScannedProject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScannedProject"/> class.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        /// <param name="dependencies">Dependencies used by the project.</param>
        public ScannedProject(string name, IEnumerable<DependencyPackage> dependencies)
        {
            this.Name = name;
            this.Dependencies = dependencies.ToList();
        }

        /// <summary>
        /// Gets the name of the project.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the dependencies used by the project.
        /// </summary>
        public IReadOnlyCollection<DependencyPackage> Dependencies { get; }
    }
}
