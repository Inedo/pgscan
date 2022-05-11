using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
#if NET452
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
#endif

            try
            {
                try
                {
                    if (args.Length < 1)
                    {
                        Usage();
                        return 1;
                    }

                    var argList = new ArgList(args);
                    if (string.IsNullOrWhiteSpace(argList.Command))
                        throw new PgScanException("Command is not specified.", true);

                    switch (argList.Command.ToLowerInvariant())
                    {
                        case "report":
                            await Report(argList);
                            break;

                        case "publish":
                            await Publish(argList);
                            break;

                        default:
                            throw new PgScanException($"Invalid command: {argList.Command}", true);
                    }
                }
                catch (Exception ex) when (ex is not PgScanException && ex.Data.Contains("message"))
                {
                    throw new PgScanException(ex.Message);
                }
            }
            catch (PgScanException ex)
            {
                Console.Error.WriteLine(ex.Message);

                if (ex.WriteUsage)
                    Usage();

                return ex.ExitCode;
            }

            return 0;
        }

        private static async Task Report(ArgList args)
        {
            if (!args.Named.TryGetValue("input", out var inputFileName))
                throw new PgScanException("Missing required argument --input=<input file name>");

            args.Named.TryGetValue("type", out var typeName);
            typeName ??= GetImplicitTypeName(inputFileName);
            if (string.IsNullOrWhiteSpace(typeName))
                throw new PgScanException("Missing --type argument and could not infer type based on input file name.");

            if (!Enum.TryParse<DependencyScannerType>(typeName, true, out var type))
                throw new PgScanException($"Invalid scanner type: {typeName} (must be nuget, npm, or pypi)");

            args.Named.TryGetValue("consider-project-references", out var considerProjectReferences);
            if (!string.IsNullOrEmpty(considerProjectReferences))
                throw new PgScanException("Supplying a value for option --consider-project-references is not allowed.");

            var scanner = DependencyScanner.GetScanner(inputFileName, type);
            var projects = await scanner.ResolveDependenciesAsync(considerProjectReferences is null ? false : true);
            if (projects.Count > 0)
            {
                foreach (var p in projects)
                {
                    Console.WriteLine(p.Name ?? "(project)");
                    foreach (var d in p.Dependencies)
                        Console.WriteLine($"  => {d.Name} {d.Version}");

                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No projects found.");
            }
        }

        private static async Task Publish(ArgList args)
        {
            var inputFileName = args.GetRequiredNamed("input");

            args.Named.TryGetValue("type", out var typeName);
            typeName ??= GetImplicitTypeName(inputFileName);
            if (string.IsNullOrWhiteSpace(typeName))
                throw new PgScanException("Missing --type argument and could not infer type based on input file name.");

            if (!Enum.TryParse<DependencyScannerType>(typeName, true, out var type))
                throw new PgScanException($"Invalid scanner type: {typeName} (must be nuget, npm, or pypi)");

            var packageFeed = args.GetRequiredNamed("package-feed");
            var progetUrl = args.GetRequiredNamed("proget-url");
            var consumerSource = args.GetRequiredNamed("consumer-package-source");

            args.Named.TryGetValue("consumer-package-group", out var consumerGroup);
            args.Named.TryGetValue("api-key", out var apiKey);

            string consumerVersion = null;
            string consumerName = null;

            // try to get consumerName and consumerVersion from file (e.g. a build result like a DLL or EXE file)
            if (args.Named.TryGetValue("consumer-package-file", out var consumerVersionFile) && File.Exists(consumerVersionFile))
            {
                try
                {
                    var vi = FileVersionInfo.GetVersionInfo(consumerVersionFile);
                    consumerVersion = vi.FileVersion ?? vi.ProductVersion;
                    consumerName = vi.ProductName;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }

            if (consumerVersion == null)
                consumerVersion = args.GetRequiredNamed("consumer-package-version");
            else if (args.Named.TryGetValue("consumer-package-version", out var consumerPackageVersion))
            {
                // a provided consumer-package-version overrides a version extracted from a file
                consumerVersion = consumerPackageVersion;
            }

            if (args.Named.TryGetValue("consumer-package-name", out var consumerPackageName))
            {
                // a provided consumer-package-name overrides a name extracted from a file
                consumerName = consumerPackageName;
            }

            string consumerFeed = null;
            string consumerUrl = null;

            if (consumerSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || consumerSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                consumerUrl = consumerSource;
            else
                consumerFeed = consumerSource;

            args.Named.TryGetValue("consider-project-references", out var considerProjectReferences);
            if (!string.IsNullOrEmpty(considerProjectReferences))
                throw new PgScanException("Supplying a value for option --consider-project-references is not allowed.");

            var scanner = DependencyScanner.GetScanner(inputFileName, type);
            var projects = await scanner.ResolveDependenciesAsync(considerProjectReferences == null ? false : true);


            if (string.IsNullOrEmpty(consumerName))
            {
                foreach (var project in projects)
                {
                    var consumer = new PackageConsumer
                    {
                        Name = project.Name,
                        Version = consumerVersion,
                        Group = consumerGroup,
                        Feed = consumerFeed,
                        Url = consumerUrl
                    };

                    foreach (var package in project.Dependencies)
                    {
                        Console.WriteLine($"Publishing consumer data for {package} consumed by {project.Name}...");
                        await package.PublishDependencyAsync(
                            progetUrl,
                            packageFeed,
                            consumer,
                            apiKey
                        );
                    }
                }
            }
            else
            {
                var consumer = new PackageConsumer
                {
                    Name = consumerName,
                    Version = consumerVersion,
                    Group = consumerGroup,
                    Feed = consumerFeed,
                    Url = consumerUrl
                };

                // aggregate packages usages so consumer infos won't be published mutiple times
                var hashset = new HashSet<DependencyPackage>();
                foreach (var project in projects)
                {
                    foreach (var package in project.Dependencies)
                    {
                        hashset.Add(package);
                    }
                }

                foreach (var package in hashset)
                {
                    Console.WriteLine($"Publishing consumer data for {package} for {consumerName}...");
                    await package.PublishDependencyAsync(
                        progetUrl,
                        packageFeed,
                        consumer,
                        apiKey
                    );
                }
            }

            Console.WriteLine("Dependencies published!");
        }

        private static string GetImplicitTypeName(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".sln" or ".csproj" => "nuget",
                ".json" => "npm",
                _ => Path.GetFileName(fileName).Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ? "pypi" : null
            };
        }

        private static void Usage()
        {
            Console.WriteLine($"pgscan v{typeof(Program).Assembly.GetName().Version}");
            Console.WriteLine("Usage: pgscan <command> [options...]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --type=<nuget|npm|pypi>");
            Console.WriteLine("  --input=<source file name>");
            Console.WriteLine("  --package-feed=<ProGet feed name>");
            Console.WriteLine("  --proget-url=<ProGet base URL>");
            Console.WriteLine("  --consumer-package-source=<feed name or URL>");
            Console.WriteLine("  --consumer-package-name=<name>");
            Console.WriteLine("  --consumer-package-version=<version>");
            Console.WriteLine("  --consumer-package-group=<group>");
            Console.WriteLine("  --consumer-package-file=<file name to read package name and version from (e.g. a dll or exe)>");
            Console.WriteLine("  --api-key=<ProGet API key>");
            Console.WriteLine("  --consider-project-references (treat project references as package references)");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  report\tDisplay dependency data");
            Console.WriteLine("  publish\tPublish dependency data to ProGet");
            Console.WriteLine();
        }
    }
}
