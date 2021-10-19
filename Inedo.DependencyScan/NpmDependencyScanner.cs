using System.Collections.Generic;
using System.IO;
#if !NET452
using System.Text.Json;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace Inedo.DependencyScan
{
    internal sealed class NpmDependencyScanner : DependencyScanner
    {
#if !NET452
        public override IReadOnlyCollection<ScannedProject> ResolveDependencies()
        {
            var packageLockPath = Path.Combine(Path.GetDirectoryName(this.SourcePath), "package-lock.json");

            JsonDocument doc;

            using (var stream = File.OpenRead(packageLockPath))
            {
                doc = JsonDocument.Parse(stream);
            }

            var projectName = doc.RootElement.GetProperty("name").GetString();
            return new[] { new ScannedProject(projectName, ReadDependencies(doc)) };
        }

        private static IEnumerable<DependencyPackage> ReadDependencies(JsonDocument doc)
        {
            if (!doc.RootElement.TryGetProperty("dependencies", out var dependencies))
                yield break;

            foreach (var d in dependencies.EnumerateObject())
            {
                var version = d.Value.GetProperty("version").GetString();
                yield return new DependencyPackage { Name = d.Name, Version = version };
            }
        }
#else
        public override IReadOnlyCollection<ScannedProject> ResolveDependencies()
        {
            var packageLockPath = Path.Combine(Path.GetDirectoryName(this.SourcePath), "package-lock.json");

            JObject doc;

            using (var reader = new JsonTextReader(File.OpenText(packageLockPath)))
            {
                doc = JObject.Load(reader);
            }

            var projectName = (string)doc.Property("name");
            return new[] { new ScannedProject(projectName, ReadDependencies(doc)) };
        }

        private static IEnumerable<DependencyPackage> ReadDependencies(JObject doc)
        {
            if (!(doc["dependencies"] is JObject dependencies))
                yield break;

            foreach (var d in dependencies.Properties())
            {
                var version = (string)((JObject)d.Value).Property("version");
                yield return new DependencyPackage { Name = d.Name, Version = version };
            }
        }
#endif
    }
}
