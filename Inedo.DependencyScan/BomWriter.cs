using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Inedo.DependencyScan
{
    /// <summary>
    /// Generates a minimal SBOM file.
    /// </summary>
    public class BomWriter : IDisposable
    {
        private const string ns = "http://cyclonedx.org/schema/bom/1.2";
        private readonly XmlWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BomWriter"/> class.
        /// </summary>
        /// <param name="stream">Stream into which SBOM is written.</param>
        /// <param name="closeOutput">Value indicating whether to close <paramref name="stream"/>.</param>
        public BomWriter(Stream stream, bool closeOutput = true)
        {
            this.writer = XmlWriter.Create(new StreamWriter(stream, new UTF8Encoding(false)), new XmlWriterSettings { CloseOutput = closeOutput, WriteEndDocumentOnClose = true });
            this.writer.WriteStartElement("bom", ns);
            this.writer.WriteAttributeString("serialNumber", "urn:uuid:" + Guid.NewGuid().ToString("n"));
            this.writer.WriteAttributeString("version", "1");
        }

        /// <summary>
        /// Records project information in the SBOM.
        /// </summary>
        /// <param name="group">The project group (may be null).</param>
        /// <param name="name">The project name.</param>
        /// <param name="version">The project version.</param>
        /// <param name="type">The project type (typically application or library).</param>
        public void Begin(string group, string name, string version, string type)
        {
            this.writer.WriteStartElement("metadata", ns);

            this.writer.WriteElementString("timestamp", ns, DateTime.UtcNow.ToString("o"));

            this.writer.WriteStartElement("component", ns);
            this.writer.WriteAttributeString("type", type);

            if (!string.IsNullOrEmpty(group))
                this.writer.WriteElementString("group", ns, group);

            this.writer.WriteElementString("name", ns, name);
            this.writer.WriteElementString("version", ns, version);

            this.writer.WriteEndElement(); // component

            this.writer.WriteEndElement(); // metadata

            this.writer.WriteStartElement("components", ns);
        }
        /// <summary>
        /// Records package information in the SBOM.
        /// </summary>
        /// <param name="group">The package group (may be null).</param>
        /// <param name="name">The package name.</param>
        /// <param name="version">The package version.</param>
        /// <param name="type">The package type (nuget,npm,pypi).</param>
        /// <param name="qualifier">Additional qualifier text.  Is appended to the end of the purl</param>
        public void AddPackage(string group, string name, string version, string type, string qualifier)
        {
            this.writer.WriteStartElement("component", ns);
            this.writer.WriteAttributeString("type", "library");

            if (!string.IsNullOrEmpty(group))
                this.writer.WriteElementString("group", ns, group);

            this.writer.WriteElementString("name", ns, name);
            this.writer.WriteElementString("version", ns, version);

            var fullName = string.IsNullOrEmpty(group) ? name : $"{group}/{name}";

            this.writer.WriteElementString("purl", ns, $"pkg:{type}/{Uri.EscapeUriString(fullName)}@{version}{(string.IsNullOrWhiteSpace(qualifier) ? string.Empty : ("?"+qualifier))}");

            this.writer.WriteEndElement(); // component
        }
        /// <summary>
        /// Closes the SBOM file.
        /// </summary>
        public void Dispose()
        {
            this.writer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
