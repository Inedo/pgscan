using System;

namespace Inedo.DependencyScan
{
    public sealed class PgScanException : Exception
    {
        public PgScanException(int exitCode, string message)
            : base(message)
        {
            this.ExitCode = exitCode;
        }
        public PgScanException(string message)
            : base(message)
        {
            this.ExitCode = -1;
        }
        public PgScanException(string message, bool writeUsage)
            : base(message)
        {
            this.ExitCode = -1;
            this.WriteUsage = writeUsage;
        }

        public int ExitCode { get; }
        public bool WriteUsage { get; }
    }
}
