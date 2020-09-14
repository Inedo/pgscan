namespace Inedo.DependencyScan
{
    public sealed class Package
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Version { get; set; }

        public override string ToString()
        {
            var name = string.IsNullOrWhiteSpace(this.Group) ? this.Name : (this.Group + "/" + this.Name);
            return $"{name} {this.Version}";
        }
    }
}
