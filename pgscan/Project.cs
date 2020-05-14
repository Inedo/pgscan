using System.Collections.Generic;
using System.Linq;

namespace Inedo.DependencyScan
{
    public sealed class Project
    {
        public Project(string name, IEnumerable<Package> dependencies)
        {
            this.Name = name;
            this.Dependencies = dependencies.ToList();
        }

        public string Name { get; }
        public IReadOnlyCollection<Package> Dependencies { get; }
    }
}
