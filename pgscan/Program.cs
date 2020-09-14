using System;
using System.IO;

namespace Inedo.DependencyScan
{
    public static class Program
    {
        public static int Main(string[] args)
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
                        Report(argList);
                        break;

                    case "publish":
                        Publish(argList);
                        break;

                    default:
                        throw new PgScanException($"Invalid command: {argList.Command}", true);
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

        private static void Report(ArgList args)
        {
            if (!args.Named.TryGetValue("input", out var inputFileName))
                throw new PgScanException("Missing required argument --input=<input file name>");

            args.Named.TryGetValue("type", out var typeName);
            typeName = typeName ?? GetImplicitTypeName(inputFileName);
            if (string.IsNullOrWhiteSpace(typeName))
                throw new PgScanException("Missing --type argument and could not infer type based on input file name.");

            var scanner = DependencyScanner.GetScanner(typeName);
            scanner.SourcePath = inputFileName;
            var projects = scanner.ResolveDependencies();
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

        private static void Publish(ArgList args)
        {
            var inputFileName = args.GetRequiredNamed("input");

            args.Named.TryGetValue("type", out var typeName);
            typeName = typeName ?? GetImplicitTypeName(inputFileName);
            if (string.IsNullOrWhiteSpace(typeName))
                throw new PgScanException("Missing --type argument and could not infer type based on input file name.");

            var packageFeed = args.GetRequiredNamed("package-feed");
            var progetUrl = args.GetRequiredNamed("proget-url");
            var consumerSource = args.GetRequiredNamed("consumer-package-source");
            var consumerVersion = args.GetRequiredNamed("consumer-package-version");

            args.Named.TryGetValue("consumer-package-name", out var consumerName);
            args.Named.TryGetValue("consumer-package-group", out var consumerGroup);
            args.Named.TryGetValue("api-key", out var apiKey);

            string consumerFeed = null;
            string consumerUrl = null;

            if (consumerSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || consumerSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                consumerUrl = consumerSource;
            else
                consumerFeed = consumerSource;

            var client = new ProGetClient(progetUrl);

            var scanner = DependencyScanner.GetScanner(typeName);
            scanner.SourcePath = inputFileName;
            var projects = scanner.ResolveDependencies();
            foreach (var project in projects)
            {
                foreach (var package in project.Dependencies)
                {
                    Console.WriteLine($"Publishing consumer data for {package}...");

                    client.RecordPackageDependency(
                        package,
                        packageFeed,
                        new PackageConsumer
                        {
                            Name = consumerName ?? project.Name,
                            Version = consumerVersion,
                            Group = consumerGroup,
                            Feed = consumerFeed,
                            Url = consumerUrl
                        },
                        apiKey
                    );
                }
            }

            Console.WriteLine("Dependencies published!");
        }

        private static string GetImplicitTypeName(string fileName)
        {
            switch (Path.GetExtension(fileName).ToLowerInvariant())
            {
                case ".sln":
                case ".csproj":
                    return "nuget";

                case ".json":
                    return "npm";

                default:
                    return Path.GetFileName(fileName).Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ? "pypi" : null;
            }
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
            Console.WriteLine("  --api-key=<ProGet API key>");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  report\tDisplay dependency data");
            Console.WriteLine("  publish\tPublish dependency data to ProGet");
            Console.WriteLine();
        }
    }
}
