namespace Inedo.DependencyScan
{
    public sealed class PackageConsumer
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Version { get; set; }
        public string Feed { get; set; }
        public string Url { get; set; }
    }
}
