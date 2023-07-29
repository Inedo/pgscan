using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.DependencyScan
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
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

                        case "identify":
                            await CreateBom(argList);
                            break;

                        case "help":
                            Usage(argList.TryGetPositional(0));
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
            var projects = await scanner.ResolveDependenciesAsync(considerProjectReferences is not null);
            if (projects.Count > 0)
            {
                foreach (var p in projects)
                {
                    Console.WriteLine(p.Name ?? "(project)");
                    foreach (var d in p.Dependencies.OrderBy(dep => dep.Name).ThenBy(dep => dep.Version))
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

            string[] packageFeeds;
            if (args.Named.TryGetValue("package-feeds", out var packageFeedsCommaSeparated))
            {
                packageFeeds = packageFeedsCommaSeparated.Split(',');
            }
            else
            {
                var packageFeed = args.GetRequiredNamed("package-feed");
                packageFeeds = new[] { packageFeed };
            }

            var progetUrl = args.GetRequiredNamed("proget-url");
            var consumerSource = args.GetRequiredNamed("consumer-package-source");

            args.Named.TryGetValue("consumer-package-group", out var consumerGroup);
            args.Named.TryGetValue("api-key", out var apiKey);

            // take product name and version either from a given file of from explicitly given parameters
            args.Named.TryGetValue("consumer-package-name", out var consumerName);
            args.Named.TryGetValue("consumer-package-version", out var consumerVersion);
            TryGetFileInfo(args, "consumer-package-file", ref consumerName, ref consumerVersion);
            if (consumerName == null)
                throw new PgScanException("Missing required argument --consumer-package-name=<name>");

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
            var projects = await scanner.ResolveDependenciesAsync(considerProjectReferences != null);

            if (string.IsNullOrEmpty(consumerName))
            {
                foreach (var project in projects)
                {
                    var dependents = new HashSet<DependencyPackage>();

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
                        if (dependents.Add(package))
                            Console.WriteLine($"Publishing consumer data for {package}...");
                    }

                    foreach (var packageFeed in packageFeeds)
                    {
                        await DependencyPackage.PublishDependenciesAsync(
                            dependents,
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

                var dependents = new HashSet<DependencyPackage>();

                foreach (var project in projects)
                {
                    foreach (var package in project.Dependencies)
                    {
                        if (dependents.Add(package))
                            Console.WriteLine($"Publishing consumer data for {package}...");
                    }
                }

                foreach (var packageFeed in packageFeeds)
                {
                    await DependencyPackage.PublishDependenciesAsync(
                        dependents,
                        progetUrl,
                        packageFeed,
                        consumer,
                        apiKey
                    );
                }
            }

            Console.WriteLine("Dependencies published!");
        }

        /// <summary>
        /// try to get product/consumer name and version from file (e.g. a build result like a DLL or EXE file)
        /// </summary>
        /// <param name="args"></param>
        /// <param name="fileInfoParamter"></param>
        /// <param name="productName">provided name takes precedence over name read from file</param>
        /// <param name="productVersion">provided version takes precedence over version read from file</param>
        private static void TryGetFileInfo(ArgList args, string fileInfoParamter, ref string productName, ref string productVersion)
        {
            if (args.Named.TryGetValue(fileInfoParamter, out var consumerVersionFile) && File.Exists(consumerVersionFile))
            {
                try
                {
                    var fvi = FileVersionInfo.GetVersionInfo(consumerVersionFile);

                    // use product name from file if name has been not provided explicitly
                    productName ??= fvi.ProductName;

                    // use file version or product version from file if version has not been provided explicitly
                    productVersion ??= fvi.FileVersion ?? fvi.ProductVersion;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static async Task CreateBom(ArgList args)
        {
            if (!args.Named.TryGetValue("input", out var inputFileName))
                throw new PgScanException("Missing required argument --input=<input file name>");

            // take product name and version either from a given file of from explicitly given parameters
            args.Named.TryGetValue("project-name", out var consumerName);
            args.Named.TryGetValue("version", out var consumerVersion);
            TryGetFileInfo(args, "file-info", ref consumerName, ref consumerVersion);
            if (consumerName == null)
                throw new PgScanException("Missing required argument --project-name=<name>");

            var isAutoType = !args.Named.TryGetValue("type", out var typeName) || typeName == null;
            typeName ??= GetImplicitTypeName(inputFileName);
            if (string.IsNullOrWhiteSpace(typeName))
                throw new PgScanException("Missing --type argument and could not infer type based on input file name.");

            if (!Enum.TryParse<DependencyScannerType>(typeName, true, out var type))
                throw new PgScanException($"Invalid scanner type: {typeName} (must be nuget, npm, or pypi)");

            args.Named.TryGetValue("consider-project-references", out var considerProjectReferences);
            if (!string.IsNullOrEmpty(considerProjectReferences))
                throw new PgScanException("Supplying a value for option --consider-project-references is not allowed.");

            var scanner = DependencyScanner.GetScanner(inputFileName, type);
            var projects = await scanner.ResolveDependenciesAsync(args.Named.ContainsKey("consider-project-references"), isAutoType);

            args.Named.TryGetValue("project-type", out var consumerType);
            consumerType ??= "library";

            var progetUrl = args.GetRequiredNamed("proget-url");
            args.Named.TryGetValue("api-key", out var apiKey);
            args.Named.TryGetValue("output-file", out var outputFile);

            if (projects.Count > 0)
            {
                var client = new ProGetClient(progetUrl);

                if (string.IsNullOrEmpty(outputFile))
                    await client.PublishSbomAsync(
                        projects,
                        new PackageConsumer { Name = consumerName, Version = consumerVersion },
                        consumerType,
                        scanner.Type.ToString().ToLowerInvariant(),
                        apiKey
                    );
                else
                    await client.PublishSbomToFileAsync(
                        projects,
                        new PackageConsumer { Name = consumerName, Version = consumerVersion },
                        consumerType,
                        scanner.Type.ToString().ToLowerInvariant(),
                        outputFile
                    );
            }
            else
            {
                Console.WriteLine("No projects found.");
            }
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

        private static void Usage(string command = null)
        {
            Console.WriteLine($"pgscan v{typeof(Program).Assembly.GetName().Version}");

            switch (command?.ToLowerInvariant())
            {
                case "help":
                    Console.WriteLine("Usage: pgscan help <command>");
                    Console.WriteLine();
                    Console.WriteLine("Displays usage information for the specified command.");
                    Console.WriteLine();
                    break;

                case "report":
                    Console.WriteLine("Usage: pgscan report [options...]");
                    Console.WriteLine();
                    Console.WriteLine("Display project dependency data.");
                    Console.WriteLine();
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --type=<nuget|npm|pypi>");
                    Console.WriteLine("  --input=<source file name>");
                    Console.WriteLine();
                    break;

                case "identify":
                    Console.WriteLine("Usage: pgscan identify [options...]");
                    Console.WriteLine();
                    Console.WriteLine("Publishes a minimal sbom file with project dependency data to ProGet. Requires ProGet 2022 or later.");
                    Console.WriteLine();
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --type=<nuget|npm|pypi>");
                    Console.WriteLine("  --input=<source file name>");
                    Console.WriteLine("  --project-name=<name>");
                    Console.WriteLine("  --version=<version>");
                    Console.WriteLine("  --file-info=<name of file to read product name and version from (e.g. a dll or exe)>");
                    Console.WriteLine("  --project-type=<library/application>");
                    Console.WriteLine("  --proget-url=<ProGet base URL>");
                    Console.WriteLine("  --api-key=<ProGet API key>");
                    Console.WriteLine("  --consider-project-references (treat project references as package references)");
                    Console.WriteLine("  --output-file=<file name> (For debugging: Writes sbom to local file instead of sending it to ProGet)");
                    Console.WriteLine();
                    break;

                case "publish":
                    Console.WriteLine("Usage: pgscan publish [options...]");
                    Console.WriteLine();
                    Console.WriteLine("Publish project dependency data to ProGet.");
                    Console.WriteLine("Note: This option is deprecated. Use 'identify' instead.");
                    Console.WriteLine();
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --type=<nuget|npm|pypi>");
                    Console.WriteLine("  --input=<source file name>");
                    Console.WriteLine("  --package-feed=<ProGet feed name>");
                    Console.WriteLine("  --proget-url=<ProGet base URL>");
                    Console.WriteLine("  --consumer-package-source=<feed name or URL>");
                    Console.WriteLine("  --consumer-package-name=<name>");
                    Console.WriteLine("  --consumer-package-version=<version>");
                    Console.WriteLine("  --consumer-package-group=<group>");
                    Console.WriteLine("  --consumer-package-file=<name of file to read package name and version from (e.g. a dll or exe)>");
                    Console.WriteLine("  --api-key=<ProGet API key>");
                    Console.WriteLine("  --consider-project-references (treat project references as package references)");
                    Console.WriteLine();
                    break;

                default:
                    if (!string.IsNullOrEmpty(command))
                        Console.Error.WriteLine("Invalid command: " + command);
                    Console.WriteLine("Usage: pgscan <command> [options...]");
                    Console.WriteLine();
                    Console.WriteLine("Commands:");
                    Console.WriteLine("  help\tDisplay command help");
                    Console.WriteLine("  report\tDisplay dependency data");
                    Console.WriteLine("  identify\tPublish minimal sbom file to ProGet");
                    Console.WriteLine("  publish\tPublish dependency data to ProGet");
                    Console.WriteLine();
                    break;
            }
        }
    }
}
