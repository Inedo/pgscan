using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
#if !NET452
using System.Text.Json;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace Inedo.DependencyScan
{
    internal sealed class NuGetDependencyScanner : DependencyScanner
    {
        private static readonly Regex SolutionProjectRegex = new(@"^Project[^=]*=\s*""[^""]+""\s*,\s*""(?<1>[^""]+)""", RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        public override IReadOnlyCollection<ScannedProject> ResolveDependencies()
        {
            if (this.SourcePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                var projects = new List<ScannedProject>();

                var solutionRoot = Path.GetDirectoryName(this.SourcePath);

                foreach (var p in ReadProjectsFromSolution(this.SourcePath))
                {
                    var projectPath = Path.Combine(solutionRoot, p);
                    projects.Add(new ScannedProject(Path.GetFileNameWithoutExtension(p), ReadProjectDependencies(projectPath)));
                }

                return projects;
            }
            else
            {
                return new[]
                {
                    new ScannedProject(Path.GetFileNameWithoutExtension(this.SourcePath), ReadProjectDependencies(this.SourcePath))
                };
            }
        }

        private static IEnumerable<string> ReadProjectsFromSolution(string solutionPath)
        {
            foreach (var line in File.ReadLines(solutionPath))
            {
                var m = SolutionProjectRegex.Match(line);
                if (m.Success)
                    yield return m.Groups[1].Value;
            }
        }

        private static IEnumerable<DependencyPackage> ReadProjectDependencies(string projectPath)
        {
            var projectDir = Path.GetDirectoryName(projectPath);
            var packagesConfigPath = Path.Combine(projectDir, "packages.config");
            if (File.Exists(packagesConfigPath))
                return ReadPackagesConfig(packagesConfigPath);

            var assetsPath = Path.Combine(projectDir, "obj", "project.assets.json");
            if (File.Exists(assetsPath))
                return ReadProjectAssets(assetsPath);

            return Enumerable.Empty<DependencyPackage>();
        }

        private static IEnumerable<DependencyPackage> ReadPackagesConfig(string packagesConfigPath)
        {
            var xdoc = XDocument.Load(packagesConfigPath);
            var packages = xdoc.Element("packages")?.Elements("package");
            if (packages == null)
                yield break;

            foreach (var p in packages)
            {
                yield return new DependencyPackage
                {
                    Name = (string)p.Attribute("id"),
                    Version = (string)p.Attribute("version")
                };
            }
        }

#if !NET452
        private static IEnumerable<DependencyPackage> ReadProjectAssets(string projectAssetsPath)
        {
            JsonDocument jdoc;
            using (var stream = File.OpenRead(projectAssetsPath))
            {
                jdoc = JsonDocument.Parse(stream);
            }

            var libraries = jdoc.RootElement.GetProperty("libraries");
            if (libraries.ValueKind == JsonValueKind.Object)
            {
                foreach (var library in libraries.EnumerateObject())
                {
                    if (library.Value.GetProperty("type").ValueEquals("package"))
                    {
                        var parts = library.Name.Split(new[] { '/' }, 2);

                        yield return new DependencyPackage
                        {
                            Name = parts[0],
                            Version = parts[1]
                        };
                    }
                }
            }
        }
#else
        private static IEnumerable<DependencyPackage> ReadProjectAssets(string projectAssetsPath)
        {
            JObject jdoc;
            using (var reader = new JsonTextReader(File.OpenText(projectAssetsPath)))
            {
                jdoc = JObject.Load(reader);
            }

            if (jdoc["libraries"] is JObject libraries)
            {
                foreach (var library in libraries.Properties())
                {
                    if ((string)((JObject)library.Value).Property("type") == "package")
                    {
                        var parts = library.Name.Split(new[] { '/' }, 2);

                        yield return new DependencyPackage
                        {
                            Name = parts[0],
                            Version = parts[1]
                        };
                    }
                }
            }
        }
#endif
    }
}
