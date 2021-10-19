using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Inedo.DependencyScan
{
    internal sealed class ArgList
    {
        public ArgList(string[] args)
        {
            var unnamed = args.Where(a => !a.StartsWith("-")).ToList();
            this.Command = unnamed.FirstOrDefault()?.ToLowerInvariant();
            this.Positional = unnamed.Skip(1).ToList().AsReadOnly();

            var regex = new Regex(@"^--?(?<1>[a-zA-Z0-9]+[a-zA-Z0-9\-]*)(=(?<2>.*))?$", RegexOptions.ExplicitCapture);
            var namedArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var arg in args.Where(a => a.StartsWith("-")))
            {
                var match = regex.Match(arg);
                if (!match.Success)
                    throw new PgScanException("Invalid argument: " + arg);

                var name = match.Groups[1].Value;
                if (namedArgs.ContainsKey(name))
                    throw new PgScanException($"Argument --{name} is specified more than once.");

                namedArgs.Add(name, match.Groups[2].Value ?? string.Empty);
            }

            this.Named = namedArgs;
        }

        public string Command { get; }
        public IReadOnlyList<string> Positional { get; }
        public IReadOnlyDictionary<string, string> Named { get; }

        public string TryGetPositional(int index) => index >= 0 && index < this.Positional.Count ? this.Positional[index] : null;

        public string GetRequiredNamed(string name)
        {
            if (this.Named.TryGetValue(name, out var value))
                return value;

            throw new PgScanException("Missing required argument --" + name);
        }
    }
}
